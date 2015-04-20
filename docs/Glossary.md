# Glossary

<dl>

<dt>Event</dt>
<dd>A message or data sent from the device. Generally assumed to have a body and
a header metadata.</dd>

<dt>Event Schema</dt>
<dd>The definition for the data transmitted in an event.</dd>

<dt>Device</dt>
<dd>A remote item that has the ability to interact with its physical
environment. The most common originator of events.</dd>

<dt>Field Gateway</dt>
<dd>A device that acts as a proxy for other devices that sits on-premise or in
the cloud. Primarily responsible for managing communication with the cloud
gateway.</dd>

<dt>Cloud Gateway</dt>
<dd>A cloud-hosted gateway that sits in between the device and the event
broker.</dd>

<dt>Event Broker</dt>
<dd>A partitioned store of events, such as Event Hub or Kafka. An event broker
acts as a buffer for load leveling and as a composition point in the
system.</dd>

<dt>Consumer Group</dt>
<dd>A logical service maintaining its own unique view over the stream of events
in the event broker.</dd>

</dl>
