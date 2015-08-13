// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Practices.IoTJourney.WarmStorage.EventProcessor.Logging;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.IoTJourney.TransientFaultHandling;

namespace Microsoft.Practices.IoTJourney.WarmStorage.EventProcessor.ElasticSearchWriter
{
    public class ElasticSearchWriter : IElasticSearchWriter
    {
        private const string BulkServiceOperationPath = "/_bulk";

        private readonly string _index;
        private readonly string _type;
        private readonly Uri _elasticsearchUrl;
        private readonly Microsoft.Practices.TransientFaultHandling.RetryPolicy _retryPolicy;

        public ElasticSearchWriter(string connectionString, string index, string type, int retryCount)
        {
            Guard.ArgumentNotNullOrEmpty(connectionString, "connectionString");
            Guard.ArgumentNotNullOrEmpty(index, "index");
            Guard.ArgumentNotNullOrEmpty(type, "type");
            Guard.ArgumentGreaterOrEqualThan(1, retryCount, "retryCount");

            if (Regex.IsMatch(index, "[\\\\/*?\",<>|\\sA-Z]"))
            {
                throw new ArgumentException(Resource.InvalidElasticsearchIndexNameError, "index");
            }

            _index = index;
            _type = type;
            _elasticsearchUrl = new Uri(new Uri(connectionString), BulkServiceOperationPath);
            _retryPolicy = RetryPolicyFactory.MakeHttpRetryPolicy(retryCount);
        }

        public async Task<bool> WriteAsync(IReadOnlyCollection<EventData> events, CancellationToken cancellationToken)
        {
            Guard.ArgumentNotNull(events, "events");

            if(!events.Any())
            {
                return false;
            }

            try
            {
                string logMessages;
                using (var serializer = new ElasticSearchEventDataSerializer(_index, _type))
                {
                    logMessages = serializer.Serialize(events);
                }

                using (var client = new HttpClient())
                {
                    var resp = await _retryPolicy.ExecuteAsync(async () =>
                    {
                        var content = new StringContent(logMessages);
                        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                        var response = await client.PostAsync(_elasticsearchUrl, content, cancellationToken).ConfigureAwait(false);

                        if(response.StatusCode != HttpStatusCode.OK)
                        {
                            WarmStorageEventSource.Log.WriteToElasticSearchFailedPerf(events.Count);

                            await HandleErrorResponse(response);
                        }

                        //This is for retry strategy. If response is not successful it will raise an exception catched by the retry strategy.
                        response.EnsureSuccessStatusCodeEx();

                        WarmStorageEventSource.Log.WriteToElasticSearchSuccessPerf(events.Count);

                        return response;
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                //If a non-transient error has ocurred or if we ran out of attempts.

                WarmStorageEventSource.Log.WriteToElasticSearchError(ex);

                return false;
            }
        }

        private static async Task HandleErrorResponse(HttpResponseMessage response)
        {
            var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            string serverErrorMessage;

            // Try to parse the exception message
            try
            {
                var errorObject = JObject.Parse(errorContent);
                serverErrorMessage = errorObject["error"].Value<string>();
            }
            catch (Exception)
            {
                // If for some reason we cannot extract the server error message log the entire response
                serverErrorMessage = errorContent;
            }

            WarmStorageEventSource.Log.WriteToElasticSearchResponseFailed(serverErrorMessage);
        }
    }
}