// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Practices.IoTJourney.ScenarioSimulator.Instrumentation
{
    public interface ISenderInstrumentationPublisher
    {
        void EventSendRequested();
        void EventSendCompleted(long length, TimeSpan elapsed);
    }
}