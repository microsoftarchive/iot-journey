using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Practices.IoTJourney.WarmStorage.ElasticSearchWriter
{
    /// <summary>
    /// Converts Microsoft.ServiceBus.Messaging.EventData to JSON formatted Elasticsearch _bulk service index operation.
    /// IMPORTANT: we are assumming that EventData payload is valid JSON UTF8 Encoded.
    /// </summary>
    internal class ElasticSearchEventDataSerializer : IDisposable
    {
        private readonly string _indexName;
        private readonly string _entryType;

        private JsonWriter _writer;

        internal ElasticSearchEventDataSerializer(string indexName, string entryType)
        {
            _indexName = indexName;
            _entryType = entryType;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "StringWriter does not hold resources.")]
        internal string Serialize(IEnumerable<EventData> events)
        {
            if (events == null)
            {
                return null;
            }

            var sb = new StringBuilder();
            _writer = new JsonTextWriter(new StringWriter(sb, CultureInfo.InvariantCulture)) { CloseOutput = true };

            foreach (var e in events)
            {
                this.WriteJsonEntry(e);
            }

            // Close the writer
            _writer.Close();
            _writer = null;

            return sb.ToString();
        }

        private void WriteJsonEntry(EventData e)
        {
            _writer.WriteStartObject();

            _writer.WritePropertyName("index");

            // Write the batch "index" operation header
            _writer.WriteStartObject();
            // ES index names must be lower case and cannot contain whitespace or any of the following characters \/*?"<>|,
            WriteValue("_index", this.GetIndexName(e.EnqueuedTimeUtc));
            WriteValue("_type", _entryType);

            _writer.WriteEndObject();
            _writer.WriteEndObject();
            _writer.WriteRaw("\n");  //ES requires this \n separator

            var payload = JObject.Parse(Encoding.UTF8.GetString(e.GetBytes()));

            foreach (var property in e.Properties)
            {
                payload.Add("Properties-" + property.Key, new JValue(property.Value));
            }

            _writer.WriteRaw(payload.ToString(Formatting.None));
            
            _writer.WriteRaw("\n");
        }

        private void WriteValue(string key, object valueObj)
        {
            _writer.WritePropertyName(key);
            _writer.WriteValue(valueObj);
        }

        private string GetIndexName(DateTime entryDateTime)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}-{1:yyyy.MM.dd}", _indexName, entryDateTime);
        }

        public void Dispose()
        {
            if (_writer != null)
            {
                _writer.Close();
                _writer = null;
            }
        }
    }
}
