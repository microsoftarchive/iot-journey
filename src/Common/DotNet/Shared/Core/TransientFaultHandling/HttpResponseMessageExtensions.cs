using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Practices.IoTJourney.TransientFaultHandling
{
    public static class HttpResponseMessageExtensions
    {
        public static HttpResponseMessage EnsureSuccessStatusCodeEx(this HttpResponseMessage message)
        {
            if (message.IsSuccessStatusCode)
            {
                return message;
            }
            if (message.Content != null)
            {
                message.Content.Dispose();
            }

            object[] args = new object[] { (int)message.StatusCode, message.ReasonPhrase };

            throw new HttpRequestExceptionExt(
                string.Format(CultureInfo.InvariantCulture, "Response status code does not indicate success: {0} ({1})", args),
                message.StatusCode,
                message.ReasonPhrase
            );
        }
    }

    public class HttpRequestExceptionExt : HttpRequestException
    {
        public HttpStatusCode Status { get; set; }

        public string ReasonPhrase { get; set; }

        public HttpRequestExceptionExt(string message, HttpStatusCode statusCode, string reasonPhrase)
            : base(message)
        {
            this.Status = statusCode;

            this.ReasonPhrase = reasonPhrase;
        }
    }
}
