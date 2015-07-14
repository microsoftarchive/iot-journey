no # Time Considerations

The vast majority of IoT projects involve collecting and forwarding events.
One usually needs to know *when* these events occurred in order to do anything
meaningful with the data.  In other words, a *temporal context* implicitly
applies to the data.

This document covers some of the important considerations when working with time in an IoT project, from data collection (choosing a clock source) and representation (timestamp formats) to event consumption.  

- [Clock sources](#clock-sources)
- [Event timestamps](#event-timestamps)
- [Time formats](#time-formats)
- [Time zones](#time-zones)
- [Time-based load leveling](#time-based-load-leveling)
- [Time-based aggregation and windowing](#time-based-aggregation-and-windowing)

## Clock sources

Events must be associated with a timestamp, but the reliability of that
timestamp can vary significantly depending on where it originates. 

Your main options are using timestamps from the device itself, from a field gateway (if present), or from the cloud gateway. The following table summarizes the relative strengths of each, along three axes: 

- **Latency**: How much delay occurs between the actual event and the measuring
  of its timestamp?
- **Accuracy**: How correct is the clock's time with relation to the world's
  timekeeping systems?
- **Precision**: What is the smallest degree of measure that we can use with
  certainty? See also [Accuracy vs Precision][accuracy-precision].

[accuracy-precision]: https://en.wikipedia.org/wiki/Accuracy_and_precision

Clock Source                          | Latency            | Accuracy           | Precision
--------------------------------------|--------------------|--------------------|--------------------
Cloud gateway                         | :-1:               | :star::star:       | :star:
Field gateway                         | :star::star:       | :star::star:       | :star::star:
Field gateway with dedicated hardware | :star::star:       | :star::star::star: | :star::star::star:
Device without real-time clock (RTC)  | :star::star::star: | :star:             | :star:
Device with RTC                       | :star::star::star: | :star::star:       | :star:
Device with traditional GPS           | :star::star::star: | :star::star::star: | :star::star:
Device with dedicated hardware        | :star::star::star: | :star::star::star: | :star::star::star:

In the next sections, we'll explore these options in more detail. 

### Using the cloud gateway's clock

When you use the cloud gateway's clock, you are recording the time when the server received the event message. In some cases this is acceptable, but it's usually discouraged. 

**Latency.**  While simple to implement, it doesn't account for the time it takes to deliver the message from the device to the cloud. This latency can be especially bad when you consider batch processing, offline devices, or ocassionally-connected devices. In these scenarios, event messages are typically stored for later transmittal. If the event time isn't captured until the message is received, it could very well be incorrect. 

In some cases, this might not matter.  For example, if you only care about *reasonably current* events, you might simply discard
data when a device is offline. In that case, you would still have network latency,
but this is usually less than a few seconds.

**Accuracy.** A cloud gateway is typically synchronized over the Internet, via servers that
implement the Network Time Protocol (NTP).  The accuracy of a clock synchronized
by NTP is called the [stratum][clock-strata].  Stratum 0 is the most accurate,
and each hop away is incremented by one.  The larger the stratum, the further
away the original timing source, and the less accurate the time.

- Stratum 0 can only be measured directly from an independent timing source.
- Stratum 1 refers to a machine that is *directly connected* to an
  independent timing source.  This is usually not achievable in the cloud,
  due to the physical hardware requirement.
- Stratum 2 is usually found on NTP host servers, such as `time.windows.com`.
- Stratum 3 or 4 is often found on domain controllers in a network.

Depending on how it is synchronized, your cloud gateway may have a clock
stratum of 3, 4, or higher.  Each level of stratum can add anywhere from a few
nanoseconds to a few hundred milliseconds of error. That means
your timestamps could be a couple of seconds off, which may or may not be acceptable, depending on
the details of your IoT scenario.

For our IoT Journey project, we are targeting one event per minute, per device. For our scenarios, it really doesn't if we are off by a few seconds in either direction. 

[clock-strata]: https://en.wikipedia.org/wiki/Network_Time_Protocol#Clock_strata

**Precision.** A cloud gateway consists of software and services that run on virtual
machines.  The clock of the underlying physical servers is precise to about 10
milliseconds.  However, the virtual machine infrastructure can lead to
additional precision errors.  In general, if you are timing events that occur
10 or more times per second, the cloud gateway is not precise enough to generate
a sufficient timestamp.

### Using a field gateway's clock

In IoT scenarios where many devices are in a single location, it may be desired
to incorporate a field gateway.  This is a computer that receives event messages
from the devices and forwards them to the cloud.

**Latency.** If messages are timestamped by the field gateway, and the gateway is designed
for high availability (fault-tolerant, always-on), then the latency issues
discussed in the previous section are greatly reduced.  There is still *some*
latency, but it is now limited by the local area network connection between the
devices and the field gateway, rather than the Internet.

**Accuracy.** When configured correctly, a field gateway will synchronize its clock via NTP,
either to a public NTP server (stratum 2), or to a local domain server (stratum 3).
This means that a field gateway will typically have a stratum 3 or 4 clock,
similar to that of a cloud gateway.

Again, for most scenarios, this is perfectly acceptable, leading to only a few
seconds or a few milliseconds of discrepancy.  However, if you are coordinating
event times from multiple sites, using multiple field gateways, you may find
this to be a limiting factor.

Because a field gateway is located on premises, it is possible to achieve higher
accuracy by attaching dedicated timing hardware to the server.  See the section
[Using dedicated timing hardware](#using-dedicated-timing-hardware), below.

**Precision.** Similar to a cloud gateway, the onboard clock of most field gateway servers is
precise to about 10 milliseconds.  However, a field gateway can be installed directly on a physical server, so it is not necessarily impacted by timing errors that can occur on a virtual machine.

In a perfect world, that means you should be able to generate distinct
timestamps for up to 100 events per second.  However, it practice it may be
slightly less than that.

If dedicated timing hardware is attached to the field gateway, it can achieve higher much higher precision levels.

### Using the device's clock

Most modern IoT devices have some sort of onboard clock. The clock may have similar
precision and accuracy as computer hardware, or it may be
significantly better or significantly worse. It's important to understand the
timing characteristics of your specific device.

However, keep in mind that some devices have no clock at all.
Obviously, if a device doesn't have a reliable clock, you can't
use it as a clock source. In that case, consider using a field gateway.

**Latency.** The main advantage of using the device's clock is that it can record the timestamp 
with very little latency.  The latency is only a factor of how long it takes the
device to read from its sensor and request a timestamp from its clock. 

In addition, a device can record a timestamp even when disconnected. In this situation, it simply stores the message until it
can forward the message to the field gateway or cloud gateway.  This capability
is also useful when sending events in a batch.

**Accuracy.** Many devices can synchronize using NTP during their startup sequence, but may
need to be explicitly configured to do so. It's important to configure the device to repeat
this synchronization periodically, to avoid [clock drift][clock-drift] &mdash; perhaps daily or more frequently, depending on the device.

On some devices, you can improve accuracy by using external timing sources, such as GPS or other dedicated hardware.  Refer to the following sections for more detail.

[clock-drift]: https://en.wikipedia.org/wiki/Clock_drift

**Precision.** Clock precision varies significantly across different devices.  Some devices
use an onboard real-time clock (RTC).  Others simply load the
time into a CPU register or memory location, and use interrupts to update the
value with each tick.

Precision is also highly dependent on the specific device
hardware.  While the RTC on most computer hardware is precise to
about 10 milliseconds, a particular device may offer more or less precision.  Again,
understand the performance characteristics of the device you are using.  If you
change devices or use a mix of different devices, don't assume that
they will have the same accuracy or precision.

### Using the device's GPS as a clock

Many IoT scenarios use GPS signals to gather latitude and longitude
coordinates, which are included in the event data.  If you're already using GPS for
location, consider using it for timing also.  GPS signals are highly
accurate, and this will relieve your device from having to synchronize its
clock over a network connection.   However, it's important to realize that
*precision* is usually still a function of the device's onboard hardware, and
that not all GPS devices are capable of acting as clock sources.

The raw GPS signal gives a
timestamp that is not fully aligned to Coordinated Universal Time (UTC).
In 1980, near the introduction of the GPS system, the "GPS time" was set to
align with UTC, but it has not been adjusted for leap seconds that have occurred
since then.  As of July 2015, GPS time is 17 seconds ahead of UTC, and that
will change as more leap seconds are added in future years.

There are two approaches to converting GPS time to UTC time.  Traditionally, a
table of leap seconds was maintained on the device.  However, many
modern GPS receivers can read an additional field sent in the GPS data stream that
gives the GPS-UTC offset, and apply it directly.   It's important to know which
approach your device's GPS receiver uses.  If it automatically adjusts for
leap seconds, then you have nothing more to do.  Otherwise, you may
need to adjust the timestamp manually before using it in your application.

### Using dedicated timing hardware

If you need both high accuracy and high precision, consider a
dedicated hardware clock.  There are [several manufacturers][clock-makers] of
high-precision timing hardware that use signals from other sources, such as
radio, CDMA, or GPS.  There are even atomic clocks for extreme precision.  These devices can be attached to a field gateway, to your network,
or in some cases, directly to your IoT devices.  These timing sources provide
some of the highest precision and accuracy levels available, but also come at a
cost.

See also [Choosing Reference Clocks][ntp-choosing].

[clock-makers]: http://www.nist.gov/pml/div688/grp40/receiverlist.cfm
[ntp-choosing]: http://support.ntp.org/bin/view/Support/ChoosingReferenceClocks


### Clock sources: summary

The following table summarizes the pros and cons of each approach.

Clock Source | Pros | Cons
-------------|------|------
Cloud Gateway | <ul><li>Simple</li><li>Already synchronized</li><li>Acceptable for some always-online scenarios</li></ul> | <ul><li>High latency impacts data</li><li>Risk of bad data or dropped events when devices are offline</li><li>VMs may impact precision</li><li>No ability to attach dedicated clock hardware</li></ul>
Field Gateway | <ul><li>Single source of time across devices</li><li>Can attach dedicated clock hardware for improved accuracy and precision</li></ul> | <ul><li>IoT solution may not include a field gateway</li><li>Not practical for ocassionally-connected devices</li></ul>
Device | <ul><li>Works when disconnected</li><li>May have a GPS clock already</li><li>May be possible to attach dedicated clock hardware</li></ul> | <ul><li>Some devices don't have a clock</li><li>Synchronization is more difficult</li><li>Device may not have an RTC </li><li>Device precision may be lower than a typical computer</li></ul>

## Event timestamps

When you record a timestamp and send it over the wire, it's important that the value has an unambiguous meaning. There are two viable options, UTC and local time + offset. 

### Coordinated Universal Time (UTC)

Most programming languages make it simple to get the current UTC time.  For
example, in .NET you can call `DateTime.UtcNow`.  The returned value represents a single moment in time that is the same anywhere
on the planet, regardless of time zone.  Additionally, UTC does not use daylight
saving time, so it is never ambiguous.

Note that UTC is sometimes referred to as GMT.  Both are equivalent in modern
times, but the term UTC is preferred because it has a precise scientific
definition.

### Local time + offset

UTC doesn't provide any information
about the local time.  Perhaps it matters to your scenario that an event
happened in the morning or in the evening.  Or perhaps your data needs to be
grouped by the local date, where "local" is different depending on where in the
world the device is located.

In these scenarios, it may be tempting to timestamp your events with just the
local date and time &mdash; for example, by using `DateTime.Now` in .NET.  However, that
is usually not a good idea, because it creates ambiguity about the exact time when 
the event occurred.  Even if all your devices are located in the same time zone,
you could still get ambiguous data, due to [the mechanics of daylight
saving time][dst-wiki].

To overcome this problem, you can pass the local time *and* the current offset
from UTC.  For example, `DateTimeOffset.Now` in .NET returns a data
type that has the current local time along with the current UTC offset.  Note
that in many time zones, the offset changes for daylight saving time, so it is
not just a fixed number.

In some cases, a particular technology does not directly support a time + offset type. For example, Azure Stream Analytics only has a `datetime` type, and does not support
`datetimeoffset`.  In that case, you should timestamp your events by the UTC
time, and then include either the offset or the local time in a separate field.
Alternatively, you could send *just* the UTC timestamp, and compute the
local offset later, based on metadata for the device. (See [Time Zones](time-zones).)

See also:  [`DateTime` vs `DateTimeOffset`][dt-vs-dto]

[dst-wiki]: http://stackoverflow.com/tags/dst/info
[dt-vs-dto]: http://stackoverflow.com/questions/4331189/datetime-vs-datetimeoffset

## Time formats

There are many different date and time formats used in programming. The format
you choose may be specific to your transport and serialization
protocols. However, the two most commonly used options are ISO 8601 and Unix timestamps. Dates in ISO 8601 are more readable, and Unix timestamps are more compact.

### ISO 8601

The [ISO 8601][iso-8601] specification provides multiple formats for representing of date and time values.  Because events need to be unambiguous, it's important that you use the subset defined in [RFC 3339][rfc-3339].

- An example of a UTC-based value is:  `2015-07-30T01:23:59.999Z`  
  The `Z` at the end of the string indicates "Zulu Time", which is another name
  for UTC.

- An example of a date-time-offset value is: `2015-07-29T18:23:59.999-07:00`  
  This represents the same instant in time as the previous example, but
  falls into a different date and time in its local time zone.

These particular timestamps include three decimal places of
precision, which isn't necessarily required.  You can use fewer or more, and you may want to align the precision with the actual precision of
your clock source.   In other words, there's no point in sending seven digits
if you're only certain of the time to the millisecond.

[iso-8601]: https://en.wikipedia.org/wiki/ISO_8601
[rfc-3339]: https://tools.ietf.org/html/rfc3339

### Unix timestamps

Another way to transmit a timestamp is as an integer number of some unit of time
(typically seconds or milliseconds) that have elapsed since a particular point in time (called
an *epoch*).

While there have been several different epochs used in computing, the most
common is `1970-01-01T00:00:00Z`, known as the "Unix Epoch". If you are serializing timestamps as integers, you should base them on this epoch for maximum portability, regardless of operating system or language.  Note that the epoch is in UTC, not in any particular local time
zone.

Strictly speaking, "Unix Time" uses *seconds* as its unit of measure.  However,
in practice, many implementations require higher precision and
thus use *milliseconds* instead.  Use whichever makes sense for your precision
requirements, but make sure that both sides understand which measurement is
being used.

Consider Unitx timestamps if your event messages use a compact binary
representation, because integers require fewer bytes than strings.  For JSON, XML,
and other text formats that are intended to be human readable, use ISO 8601
instead.

For example, the timestamp `2015-07-30T01:23:59.999Z` can be represented by the number `1438219439999`, which is a Unix
timestamp based on milliseconds.  This only occupies 4 bytes, instead of the
24 bytes used by the ISO 8601 string.

[unix-time]: https://en.wikipedia.org/wiki/Unix_time


## Time zones

We've already discussed using the local offset in the event timestamp. It's important to recognize that an *offset* is not the same thing as a *time zone*. An offset just tells how
a single point in time relates to UTC.  A time zone *might* use a single offset,
or it might use different offsets at different points in time.  For example,
the US Pacific time zone has a standard offset of `-08:00`, but switches to
`-07:00` when [daylight saving time][dst-wiki] is in effect.  

You might want to assign a time zone to a device or group of devices.  This can be useful when performing certain types of aggregations in ad-hoc exploration, and when querying long-term
storage.

For example, consider the thermostat devices used by Fabrikam in our IoT
Journey project.  Each thermostat belongs to a particular building.  We can
assign a time zone to the building, and then use that time zone to group
events by the local day of that particular building.

So in Fabrikam's database, each building would have a time zone *identifier*,
rather than a numerical offset.  This could be either a Windows time zone ID
such as `"Pacific Standard Time"`, or an IANA time zone ID such as
`"America/Los_Angeles"`.  In .NET, you can use the
[`TimeZoneInfo`][timezoneinfo] class to work with Windows time zones, or use the open-source [Noda Time][nodatime] library to work with either type
of time zone identifier.

See also: [The timezone tag wiki on StackOverflow][tz-wiki]

[timezoneinfo]: https://msdn.microsoft.com/en-us/library/system.timezoneinfo.aspx
[nodatime]: http://nodatime.org
[tz-wiki]: http://stackoverflow.com/tags/timezone/info


## Time-based load leveling

It's a common scenario to send an event from a device at some predetermined
interval. For example, Fabrikam's thermostats send the temperature once per
minute.  Other devices might send an event once per hour, or once
per day.

In this situation, it's *critical* that all your devices do not send
events at the exact same time.  Doing so can create peaks of high activity
spaced by long stretches of idle time.  Depending on load, this may make it
difficult to process your event stream efficiently.

Instead, consider leveling out the load.  One easy approach is simply to
pause for some amount of random delay on device startup, and then start a timer on
the device to fire every interval. 

Consider:

Device | Bad Example            | Good Example
-------|------------------------|-----------------------
 A     | `0:00`, `1:00`, `2:00` | `0:00`, `1:00`, `2:00`
 B     | `0:00`, `1:00`, `2:00` | `0:01`, `1:01`, `2:01`
 C     | `0:00`, `1:00`, `2:00` | `0:02`, `1:02`, `2:02`
 D     | `0:00`, `1:00`, `2:00` | `0:03`, `1:03`, `2:03`

If your scenario requires synchronized event times, you
can *record* the events at the exact same time, and then add a
small random delay before *sending* the event.  Remember, it's not the *value* of the
timestamp that creates a problem, but rather the *simultaneous transmittal* of
many messages from many devices.

In other words, don't [DDOS][ddos] your own services.

[ddos]: https://en.wikipedia.org/wiki/Denial-of-service_attack#Distributed_attack

## Time-based aggregation and windowing

Windowing functions provide a way to group events that occur within a
period of time.

If you are using an event stream processor such as Azure Stream Analytics, you should
be familiar with the different types of windowing functions that are available.

For example, you could use a 1-hour *tumbling* window to see which hours have the most
activity.  Or, you might use a 5-minute *sliding* window to watch for trends in
the incoming data across groups of devices.  You can read more about
windowing functions in the [ASA documentation][asa-windowing].

Also, keep in mind that the timestamp used by the windowing function in ASA is
determined by the `TIMESTAMP BY` keyword.  If you do not use that, ASA will
revert to the message arrival time.  Read more in the [ASA query language reference][asa-langref].

[asa-windowing]: https://msdn.microsoft.com/en-us/library/azure/dn835019.aspx
[asa-langref]: https://msdn.microsoft.com/en-us/library/azure/dn834998.aspx
