using Microsoft.Practices.TransientFaultHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Practices.IoTJourney.TransientFaultHandling
{
    /// <summary> 
    /// Provides the transient error detection logic that can recognize transient faults when dealing with HTTP services. 
    /// </summary> 
    public class HttpTransientErrorDetectionStrategy : ITransientErrorDetectionStrategy
    {
        /// <summary>
        /// Probable transient error codes. See: https://github.com/mspnp/azure-guidance/blob/master/Retry-Service-Specific.md#general-rest-and-retry-guidelines
        /// </summary>
        private static readonly HttpStatusCode[] statusCodes = new[]
        {
            HttpStatusCode.RequestTimeout, //408
            HttpStatusCode.InternalServerError, //500
            HttpStatusCode.BadGateway, //502
            HttpStatusCode.ServiceUnavailable, //503
            HttpStatusCode.GatewayTimeout //504
        };

        private static readonly WebExceptionStatus[] webExceptionStatus = new[] { WebExceptionStatus.ConnectionClosed, WebExceptionStatus.Timeout, WebExceptionStatus.RequestCanceled };

        /// <summary> 
        /// Determines whether the specified exception represents a transient failure that can be compensated by a retry. 
        /// </summary> 
        /// <param name="ex">The exception object to be verified.</param> 
        /// <returns>true if the specified exception is considered transient; otherwise, false.</returns> 
        public bool IsTransient(Exception ex)
        {
            return ex != null && (CheckIsTransient(ex) || (ex.InnerException != null && CheckIsTransient(ex.InnerException)));
        }

        private bool CheckIsTransient(Exception ex)
        {
            if (ex is TimeoutException) return true;

            var httpRequestExceptionExt = ex as HttpRequestExceptionExt;
            if (httpRequestExceptionExt != null)
            {
                if (statusCodes.Contains(httpRequestExceptionExt.Status)) return true;
            }

            var webException = ex as WebException;
            if (webException != null)
            {
                if (webExceptionStatus.Contains(webException.Status)) return true;

                if (webException.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = webException.Response as HttpWebResponse;
                    if (response != null && statusCodes.Contains(response.StatusCode)) return true;
                }
            }

            return false;
        }
    }
}
