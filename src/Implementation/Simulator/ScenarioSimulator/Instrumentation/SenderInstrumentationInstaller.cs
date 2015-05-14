// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Configuration.Install;

namespace Microsoft.Practices.IoTJourney.ScenarioSimulator.Instrumentation
{
    [RunInstaller(true)]
    public partial class SenderInstrumentationInstaller : Installer
    {
        public SenderInstrumentationInstaller()
        {
            this.Installers.Add(new SenderInstrumentationManager(false, false).GetInstaller());
        }
    }
}