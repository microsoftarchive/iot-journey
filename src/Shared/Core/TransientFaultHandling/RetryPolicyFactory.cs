using Microsoft.Practices.TransientFaultHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Practices.IoTJourney.TransientFaultHandling
{
    public static class RetryPolicyFactory
    {
        public static RetryPolicy MakeHttpRetryPolicy(int retryCount)
        {
            ITransientErrorDetectionStrategy strategy = new HttpTransientErrorDetectionStrategy();

            return Exponential(strategy, retryCount);
        }

        private static RetryPolicy Exponential(ITransientErrorDetectionStrategy strategy, int count)
        {
            return Exponential(strategy, count, 1024d, 2d);
        }

        private static RetryPolicy Exponential(ITransientErrorDetectionStrategy strategy,
                                                int retryCount,
                                                double maxBackoffDelayInSeconds,
                                                double delta)
        {
            var maxBackoff = TimeSpan.FromSeconds(maxBackoffDelayInSeconds);
            var deltaBackoff = TimeSpan.FromSeconds(delta);
            var minBackoff = TimeSpan.FromSeconds(0);

            var exponentialBackoff = new ExponentialBackoff(retryCount, minBackoff, maxBackoff, deltaBackoff);

            return new RetryPolicy(strategy, exponentialBackoff);
        }
    }
}
