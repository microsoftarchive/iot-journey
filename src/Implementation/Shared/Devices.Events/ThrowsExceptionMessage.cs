// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.Practices.IoTJourney.Devices.Events
{
    // This event represents a "malformed" event. The
    // implication is that the event producer created
    // a event that dispatcher routes to a handler, but
    // that handler is not able to successfully process the
    // event and it throws.
    // This behavior is explicitly modelled in the handler in
    // order to demonstrate how the dispatcher compensates for 
    // handlers that throw.
    public class ThrowsExceptionEvent { }
}