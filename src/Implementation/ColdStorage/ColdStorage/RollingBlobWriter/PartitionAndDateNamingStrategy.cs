// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

namespace Microsoft.Practices.IoTJourney.ColdStorage.RollingBlobWriter
{
    public class PartitionAndDateNamingStrategy : IBlobNamingStrategy
    {
        private readonly string _instanceName;

        private readonly string _partitionName;

        public PartitionAndDateNamingStrategy(string partitionName, String instanceName = "pnp-iotjourney")
        {
            Guard.ArgumentNotNullOrEmpty(partitionName, "partitionName");
            Guard.ArgumentNotNullOrEmpty(instanceName, "instanceName");

            _partitionName = partitionName;
            _instanceName = instanceName;
        }

        public string GetNamePrefix()
        {
            var dateTime = DateTime.UtcNow;
            var year = dateTime.Year;
            var month = dateTime.Month;
            var day = dateTime.Day;
            return String.Format(
                CultureInfo.InvariantCulture,
                "{0}/{1}/{2:D4}/{3:D2}/{4:D2}/",
                _instanceName,
                _partitionName,
                year,
                month,
                day);
        }
    }
}