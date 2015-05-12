// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Practices.IoTJourney.Logging
{
    public class NoOpLoggerFactory : ILogFactory
    {
        public ILogger Create()
        {
            return new NoOpLogger();
        }

        public ILogger Create(string name)
        {
            return Create();
        }

        public void Initialize()
        {
        }
    }
}
