// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Practices.IoTJourney
{
    public static class Extensions
    {
        public static TimeSpan Multiply(this TimeSpan timeSpan, double factor)
        {
            return TimeSpan.FromTicks((long)(timeSpan.Ticks * factor));
        }
    }
}
