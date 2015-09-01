using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using STimer = System.Timers.Timer;

namespace Microsoft.Practices.IoTJourney.Monitoring.EventProcessor
{
    public class PartitionMonitor
    {
        private int MinSamplingRateSeconds = 30;
        private TimeSpan _samplingRate;
        private STimer _samplingTimer;
        private STimer _sessionTimer;
        private ISubject<EventEntry> _stream = new Subject<EventEntry>();

        private string _consumerGroupName;
        private string _checkpointContainerName;

        private NamespaceManager _nsm;
        private EventHubClient _eventHubClient;
        private Configuration _configuration;
        private CloudStorageAccount _storageAccount;

        public ISubject<EventEntry> Stream
        {
            get { return _stream; }
        }

        public PartitionMonitor(TimeSpan samplingRate, TimeSpan sessionTimeout, string consumerGroupName, string checkpointContainerName)
        {
            Guard.ArgumentNotNullOrEmpty(consumerGroupName, "consumerGroupName");
            Guard.ArgumentNotNullOrEmpty(checkpointContainerName, "checkpointContainerName");

            if(samplingRate.TotalSeconds < MinSamplingRateSeconds)
            {
                throw new ArgumentException(string.Format("samplingRate must be higher or equal to {0} seconds.", MinSamplingRateSeconds));
            }

            _samplingRate = samplingRate;
            _samplingTimer = new STimer(samplingRate.TotalMilliseconds);
            _sessionTimer = new STimer(sessionTimeout.TotalMilliseconds);
            _configuration = Configuration.GetCurrentConfiguration();
            _consumerGroupName = consumerGroupName;
            _checkpointContainerName = checkpointContainerName;

            var endpoint = ServiceBusEnvironment.CreateServiceUri("sb", _configuration.EventHubNamespace, string.Empty);
            var connectionString = ServiceBusConnectionStringBuilder.CreateUsingSharedAccessKey(endpoint,
                                                                                                _configuration.EventHubSasKeyName,
                                                                                                _configuration.EventHubSasKey);

            _eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, _configuration.EventHubName);

            _nsm = NamespaceManager.CreateFromConnectionString(connectionString);
            _nsm.Settings.OperationTimeout = samplingRate;

            _storageAccount = CloudStorageAccount.Parse(_configuration.CheckpointStorageAccount);
        }

        public async Task StartAsync()
        {
            var runtime = await _eventHubClient.GetRuntimeInformationAsync().ConfigureAwait(false);
            var blobClient = _storageAccount.CreateCloudBlobClient();
            var checkpointContainer = blobClient.GetContainerReference(_checkpointContainerName);

            var partitionBlobRefereces = new List<CloudBlockBlob>();
            var previousSnapshots = new List<EventEntry>();

            for (int i = 0; i < runtime.PartitionCount; i++)
            {
                //Initialize block blobs references
                var partitionBlob = checkpointContainer.GetBlockBlobReference(_consumerGroupName + "/" + i);
                partitionBlobRefereces.Add(partitionBlob);

                previousSnapshots.Add(new EventEntry());
            }

            _samplingTimer.Elapsed += async (sender, e) =>
            {
                var timestamp = DateTime.UtcNow;

                for (int i = 0; i < runtime.PartitionCount; i++)
                {
                    string partitionId = i.ToString();
                    
                    try
                    {
                        var partitionInfo = await _nsm.GetEventHubPartitionAsync(_configuration.EventHubName, _consumerGroupName, partitionId).ConfigureAwait(false);

                        var checkpointBlobText = await partitionBlobRefereces[i].DownloadTextAsync().ConfigureAwait(false);
                        var partitionSnapshot = JsonConvert.DeserializeObject<EventEntry>(checkpointBlobText);

                        var unprocessedEvents = partitionInfo.EndSequenceNumber - partitionSnapshot.SequenceNumber;

                        var previousSnapshot = previousSnapshots[i];

                        partitionSnapshot.TimeStamp = timestamp;
                        partitionSnapshot.PreciseTimeStamp = DateTime.UtcNow;
                        partitionSnapshot.UnprocessedEvents = unprocessedEvents;
                        partitionSnapshot.IncomingEventsPerSecond = (int)Math.Round((partitionInfo.EndSequenceNumber - previousSnapshot.EndSequenceNumber) / _samplingRate.TotalSeconds, 0);
                        partitionSnapshot.OutgoingEventsPerSecond = (int)Math.Round((partitionSnapshot.SequenceNumber - previousSnapshot.SequenceNumber) / _samplingRate.TotalSeconds, 0);
                        partitionSnapshot.EndSequenceNumber = partitionInfo.EndSequenceNumber;
                        partitionSnapshot.IncomingBytesPerSecond = partitionInfo.IncomingBytesPerSecond;
                        partitionSnapshot.OutgoingBytesPerSecond = partitionInfo.OutgoingBytesPerSecond;

                        _stream.OnNext(partitionSnapshot);

                        previousSnapshots[i] = partitionSnapshot;
                    }
                    catch (TimeoutException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            };

            _sessionTimer.Elapsed += (sender, e) =>
            {
                Stop();
                _sessionTimer.Stop();
            };

            _samplingTimer.Start();
            _sessionTimer.Start();
        }

        public void Stop()
        {
            _samplingTimer.Stop();
        }
    }
}
