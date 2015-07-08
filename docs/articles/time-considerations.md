# Time Considerations

The vast majority of IoT projects involve collecting and forwarding events.
One usually needs to know *when* these events occurred in order to do anything
meaningful with the data.  In other words, a *temporal context* implicitly
applies to the data.

This document will cover many of the things you need to take into consideration
when working with time in an IoT project.

## Clock Sources

Events need to associated with a timestamp, but the reliability of that
timestamp can vary significantly depending on where it originates.  Should
you use the server's time, or the device's time, or perhaps some other source of
time?  We'll explore each option, taking into account the following aspects:

- **Latency** - How much delay occurs between the actual event and the measuring
  of its timestamp?
- **Accuracy** - How correct is the clock's time with relation to the world's
  timekeeping systems?
- **Precision** - What is the smallest degree of measure that we can use with
  certainty?

See also [Accuracy vs Precision][accuracy-precision].

[accuracy-precision]: https://en.wikipedia.org/wiki/Accuracy_and_precision

### Using the Cloud Gateway's Clock

#### Latency

When you use the cloud gateway's clock, you are recording the time that the
message containing the event information was received by the server.  In some
cases this is acceptable, but it's usually discouraged.  While simple to use,
it doesn't account for the time it takes to deliver the message from the device
to the cloud.

The latency can be detrimental, especially when you consider batch processing,
offline, and ocassionally-connected scenarios.  In these cases, event messages
are typically stored for later transmittal.  If the event time isn't captured
until the message is received, it could very well be incorrect.

There are some scenarios where this is not as important.  For example, if you
only care about *reasonably current* events, then you may just decide to discard
data when a device is offline.  You would still have the latency of transmittal,
but this is usually less than a few seconds.

#### Accuracy

A cloud gateway is typically synchronized over the Internet, via servers that
implement the Network Time Protocol (NTP).  The accuracy of a clock synchronized
by NTP is called the [stratum][clock-strata].  Stratum 0 is the most accurate,
and each hop away is incremented by one.  The larger the stratum, the further
away the original timing source, and the less accurate the time.

- Stratum 0 can only be measured directly from an independent timing source.
- Stratum 1 would refer to a machine that is *directly connected* to an
  independent timing source.  This is usually not achievable in the cloud,
  due to the physical hardware requirement.
- Stratum 2 is usually found on NTP host servers, such as `time.windows.com`.
- Stratum 3 or 4 is often found on domain controllers in a network.

Thus depending on how it is synchronized, your cloud gateway may have a clock
stratum of 3, 4, or higher.  Each level of stratum could add anywhere from a few
nanoseconds up to a few hundred milliseconds of error to your clock.  That means
your timestamps could be a couple of seconds off from what you expect them to
be.  This may or may not be acceptable for your IoT scenario, depending on
exactly what the scenario entails.

For our IoT Journey project, we only expect to receive an event from a single
device once per minute, and it really doesn't matter if we are off by a few
seconds in either direction.  However, other IoT scenarios may have higher
accuracy requirements.

[clock-strata]: https://en.wikipedia.org/wiki/Network_Time_Protocol#Clock_strata

#### Precision

A cloud gateway consists of software and services that run on top of virtual
machines.  The clock of the underlying physical servers is precise to about 10
milliseconds.  However, the virtual machine infrastructure can lead to
additional precision errors.  In general, if you are timing events that occur
10 or more times per second, the cloud gateway is not precise enough to generate
a sufficient timestamp.

### Using a Field Gateway's Clock

In IoT scenarios where many devices are in a single location, it may be desired
to incorporate a field gateway.  This is a computer that receives event messages
from the devices, then forwards them on to the cloud.

#### Latency

If messages are timestamped by the field gateway, and the gateway is designed
for high availability (fault-tolerant, always-on), then the latency issues
discussed in the previous section are greatly reduced.  There is still *some*
latency, but it is now limited by the local area network connection between the
devices and the field gateway, rather than the Internet.

#### Accuracy

When configured correctly, a field gateway will synchronize its clock via NTP,
either to a public stratum 2 NTP server, or to a local stratum 3 domain server.
This means that a field gateway will typically have stratum 3 or 4 clock,
similar to that of a cloud gateway.

Again, for most scenarios, this is perfectly acceptable, leading to only a few
seconds or a few milliseconds of discrepancy.  However, if you are coordinating
event times from multiple sites (using multiple field gateways), you may find
this to be a limiting factor.

Since a field gateway is located on premises, it is possible to achieve higher
accuracy by attaching dedicated timing hardware to the server.  See the section
"Using Dedicated Timing Hardware" below.

#### Precision

Similar to a cloud gateway, the onboard clock of most field gateway servers is
precise to about 10 milliseconds.  However, a field gateway has the ability to
be installed directly on a physical server, so it is not necessarily impacted
by timing errors that can occur on a virtual machine.

In a perfect world, that means you should be able to generate distinct
timestamps for up to 100 events per second.  However, it practice it may be
slightly less than that.

Additionally, if dedicated timing hardware is attached to the field gateway,
it can achieve higher much higher precision levels.

### Using the Device's Clock

Most modern IoT devices have some sort of onboard clock, which may have similar
precision and accuracy characteristics as computer hardware, or they may be
significantly better or significantly worse.

However, keep in mind that there are devices that do not have clocks at all.
If the device doesn't have a reliable onboard clock, then obviously you *cannot*
use it as clock source, and should instead consider using a field gateway.

When using the device as the clock source, it's important to understand the
timing characteristics of the specific device.

#### Latency

The primary advantage to using the device for timing, is that it is the
originator of the event.  That means it can record the timestamp of the event
with very little latency.  The latency is only a factor of how long it takes the
device to read from its sensor and request a timestamp from its clock.

Additionally, the device can record an events timestamp even when the device is
disconnected.  In this situation, the device simply stores the message until it
can later be forwarded to the upstream field gateway or cloud gateway.  This is
also useful when sending events in a batch.

#### Accuracy

Many devices can synchronize using NTP during their startup sequence, but may
need to be explicitly configured to do so.  Additionally, due to
[clock drift][clock-drift], it's important to configure the device to repeat
this synchronization periodically - perhaps daily or more frequently,
depending on the device.

Accuracy can be improved on some devices, by use external sources of timing
information, such as GPS or other dedicated hardware.  Refer to the sections on
these topics below for more detail.

[clock-drift]: https://en.wikipedia.org/wiki/Clock_drift

#### Precision

Clock precision will vary significantly across different devices.  Some devices
will use an onboard real-time clock (RTC).  Other devices will simply load the
time into a CPU register or memory location and use interrupts to update the
value with each tick.

Like accuracy, precision is also highly dependent on the specific device
hardware being used.  While the RTC on most computer hardware is precise to
about 10 milliseconds, a device's RTC may offer more or less precision.  Again,
understand the performance characteristics of the device you are using.  If you
change devices or use a mix of different types of devices, don't assume that
they will have the same accuracy or precision.

### Using the Device's GPS as a Clock

Many IoT scenarios use GPS signals for gathering latitude and longitude
coordinates to include in the event data.   If you're already using GPS for
location, you should consider using it for timing also.  GPS signals are highly
accurate, and this will relieve your device from having to synchronize its
clock over a network connection.   However, it's important to realize that
*precision* is usually still a function of the device's onboard hardware, and
that not *all* GPS devices are capable of acting as clock sources.

Additionally, it's important to understand that the raw GPS signal gives a
timestamp that is not fully aligned to Coordinated Universal Time (UTC).
In 1980, near the introduction of the GPS system, the "GPS time" was set to
align with UTC, but it has not been adjusted for leap seconds that have occurred
since then.  As of July 2015, GPS time is 17 seconds ahead of UTC, but that
will change as more leap seconds are added in future years.

There are two approaches to convert GPS time to UTC time.  Traditionally, a
table of leap seconds would need to be maintained on the device.  However, many
modern GPS receivers can read an additional field sent in GPS data stream that
sends the GPS-UTC offset, and apply it directly.   It's important to know which
approach your device's GPS receiver uses.  If it automatically adjusts for
leap seconds, then you have nothing more to do.  But if it doesn't, you may
need to adjust the timestamp manually before using it in your application.

### Using Dedicated Timing Hardware

If you need both high accuracy and high precision, you might consider a
dedicated hardware clock.  There are [several manufacturers][clock-makers] of
high-precision timing hardware that use signals from other sources, such as
radio, CDMA, or GPS.  There are even atomic clocks for extreme precision
scenarios.  These devices can be attached to a field gateway, to your network,
or in some cases, directly to your IoT devices.  These timing source provide
some of the highest precision and accuracy levels available, but also come at a
cost.

See also [Choosing Reference Clocks][ntp-choosing].

[clock-makers]: http://www.nist.gov/pml/div688/grp40/receiverlist.cfm
[ntp-choosing]: http://support.ntp.org/bin/view/Support/ChoosingReferenceClocks


### Summary

The following tables summarize the differences between the various clock sources.

Clock Source                          | Latency            | Accuracy           | Precision
--------------------------------------|--------------------|--------------------|--------------------
Cloud Gateway                         | :-1:               | :star::star:       | :star:
Field Gateway                         | :star::star:       | :star::star:       | :star::star:
Field Gateway with Dedicated Hardware | :star::star:       | :star::star::star: | :star::star::star:
Device without RTC                    | :star::star::star: | :star:             | :star:
Device with RTC                       | :star::star::star: | :star::star:       | :star:
Device with Traditional GPS           | :star::star::star: | :star::star::star: | :star::star:
Device with Dedicated Hardware        | :star::star::star: | :star::star::star: | :star::star::star:

Clock Source | Pros | Cons
-------------|------|------
Cloud Gateway | <ul><li>Simple</li><li>Already synchronized</li><li>Acceptable for some always-online scenarios</li></ul> | <ul><li>High latency impacts data</li><li>Risk of bad data or dropped events when devices are offline</li><li>VMs may impact precision</li><li>No ability to attach dedicated clock hardware</li></ul>
Field Gateway | <ul><li>Single source of time across devices</li><li>Can attach dedicated clock hardware for improved accuracy and precision</li></ul> | <ul><li>F.G. not always incorporated in the solution</li><li>Not practical for ocassionally-connected devices</li></ul>
Device | <ul><li>Works when disconnected</li><li>May have a GPS clock already</li><li>May be possible to attach dedicated clock hardware</li></ul> | <ul><li>Synchronization is more difficult</li><li>Device may not have an RTC </li><li>Device precision may be lower than a typical computer</li></ul>


## Event Timestamps

Since events occur at a specific instant in time, it's important that you record
it using a value that is unambiguous.   There are two options that are viable:

#### Coordinated Universal Time (UTC)

Most programming languages make it very simple to get the current UTC time.  For
example, in .NET you can call `DateTime.UtcNow`.  The value returned is a date
and time that represents a single moment in time that will be the same anywhere
on the planet, regardless of time zone.  Additionally, UTC does not use daylight
saving time, so it is never ambiguous.

Note that UTC is sometimes referred to as GMT.  Both are equivalent in modern
times, but the term UTC is preferred because it has a precise scientific
definition.

#### Local Time + Offset

Sometimes UTC isn't good enough, because it doesn't provide any information
about the local time.  Perhaps it matters to your scenario that an event
happened in the morning or in the evening.  Or perhaps your data needs to be
grouped by the local date, where "local" is different depending on where in the
world the device is located.

In these scenarios, it may be tempting to timestamp your events with just the
local date and time, such as when using `DateTime.Now` from .NET.  However, that
is usually not a good idea, as it creates ambiguity about when exactly that
event occurred.  Even if all your events are located within the same time zone,
it's still possible to get ambiguous data, due to [the mechanics of daylight
saving time][dst-wiki].

To overcome this problem, you can pass the local time *and* the current offset
from UTC.  For example, calling `DateTimeOffset.Now` in .NET will return a data
type that has the current local time, along with the current UTC offset.  Note
that in many time zones, the offset may change for daylight saving time.  It is
not just a fixed number.

In some cases, you may find that combining the offset with the date and time is
not supported.  For example, Azure Stream Analytics is a common component used
by IoT scenarios, and it only has a `datetime` type.  It does not support
`datetimeoffset`.  In this case, you should timestamp your events by the UTC
time, and then include either the offset or the local time in a separate field.
Alternatively, you could send *just* the UTC timestamp, and then compute the
local offset at a later time (see the section on **Time Zones** later in this
document).

See also:  [`DateTime` vs `DateTimeOffset`][dt-vs-dto]

[dst-wiki]: http://stackoverflow.com/tags/dst/info
[dt-vs-dto]: http://stackoverflow.com/questions/4331189/datetime-vs-datetimeoffset

## Time Formats

There are many different date and time formats used in programming. The format
you choose may be specific to your choice of transport and serialization
protocols. However, these are the two most commonly used options:

#### ISO 8601

The [ISO 8601][iso-8601] specification provides multiple formats for different
representations of date and time values.  Since events need to be unambiguous,
it's important that you use the subset defined in [RFC 3339][rfc-3339].

- An example of a UTC-based value is:  `2015-07-30T01:23:59.999Z`  
  The `Z` at the end of the string indicates "Zulu Time", which is another name
  for UTC.

- An example of a date-time-offset value is: `2015-07-29T18:23:59.999-07:00`  
  Note that this represents the same instant in time as the prior example, but
  falls into a different date and time in its local time zone.

Also note that these particular timestamps include three decimal places of
precision, which isn't necessarily required.  You can use fewer decimals or more
decimals.  You may wish to align this precision with the actual precision of
your clock source.   In other words, there's no point in sending seven digits
if you're only certain of the time to the millisecond.

[iso-8601]: https://en.wikipedia.org/wiki/ISO_8601
[rfc-3339]: https://tools.ietf.org/html/rfc3339

#### Unix Timestamps

Another way to transmit a timestamp is as an integer number of some unit of time
(typically seconds or millseconds), since some particular point in time (called
an *epoch*.

While there have been several different epochs used in computing, the most
common one is `1970-01-01T00:00:00Z`, which is known as the "Unix Epoch".  When
transmitting a timestamp as a number, such as in an IoT event message, you
should base your timestamps on this epoch, regardless of operating system or
language.   Note that the epoch is in UTC, not in any particular local time
zone.

Strictly speaking, "Unix Time" uses *seconds* as its unit of measure.  However,
in practice you'll find many implementations that require higher precision and
thus use *milliseconds* instead.  Use whichever makes sense for your precision
requirements, but make sure that both sides understand which measurement is
being used.

Use this approach when you are sending your event messages in a compact binary
representation, as integers require fewer bytes than strings.  For JSON, XML,
and other text formats that are intended to be human readable, use ISO 8601
instead.

As an example, consider that the same `2015-07-30T01:23:59.999Z` timestamp used
above can be represented by the number `1438219439999`, which is a Unix
timestamp based on milliseconds.  This only occupies 4 bytes, instead of the
24 bytes used by the ISO 8601 string.

[unix-time]: https://en.wikipedia.org/wiki/Unix_time


## Time Zones

Time zones may play in to your IoT story in several different ways.  We already
discussed that you may want to have an understanding of the local offset of the
event's timestamp, but you might also want to have a time zone associated with
the device, or with a group of devices.  This can be useful when performing
certain types of aggregations in ad-hoc exploration, and when querying long-term
storage.

As an example, consider the thermostat devices used by Fabrikam in our IoT
Journey project.  Each thermostat belongs to a particular building.  We can
assign a time zone to the building, and then use that time zone to group
events by the local day of that particular building.

When using time zones in this manner, it's important to recognize that a *time
zone* and an *offset* are two different things.   An offset just tells you how
a single point in time relates to UTC.  A time zone *might* use a single offset,
or it might use different offsets for different points in time.  For example,
the US Pacific time zone has a standard offset of `-08:00`, but switches to
`-07:00` when [daylight saving time][dst-wiki] is in effect.

So in Fabrikam's database, each building would have a time zone *identifier*,
rather than a numerical offset.  This could be either a Windows time zone ID
such as `"Pacific Standard Time"`, or could be an IANA time zone ID such as
`"America/Los_Angeles"`.  In .NET, you can use the
[`TimeZoneInfo`][timezoneinfo] class to work with Windows time zones, or you
can use the open-source library [Noda Time][nodatime] to work with either type
of time zone identifier.

See also: [The timezone tag wiki on StackOverflow][tz-wiki]

[timezoneinfo]: https://msdn.microsoft.com/en-us/library/system.timezoneinfo.aspx
[nodatime]: http://nodatime.org
[tz-wiki]: http://stackoverflow.com/tags/timezone/info


## Time-Based Load Leveling

It's a common scenario to send an event from a device at some predetermined
interval, such as how Fabrikam's thermostats send the temperature once per
minute.  Other devices might send an event once per hour, or perhaps just once
per day.

When doing so, it's *absolutely critical* that all your devices do not send
events at the exact same time.  Doing so can create peaks of high activity
spaced by long stretches of idle time.  Depending on load, this may make it
difficult to process your event stream efficiently.

Instead, consider leveling out the load.  One easy way to do this is to simple
pause for some amount of random delay on device startup, then start a timer on
the device to fire every interval.

Consider:

Device | Bad Example            | Good Example
-------|------------------------|-----------------------
 A     | `0:00`, `1:00`, `2:00` | `0:00`, `1:00`, `2:00`
 B     | `0:00`, `1:00`, `2:00` | `0:01`, `1:01`, `2:01`
 C     | `0:00`, `1:00`, `2:00` | `0:02`, `1:02`, `2:02`
 D     | `0:00`, `1:00`, `2:00` | `0:03`, `1:03`, `2:03`

If it's important for your scenario that event times are synchronized, then you
can *record* the event at the exact time, but then you should introduce some
small delay before *sending* the event.  Remember, it's not the *value* of the
timestamp that creates a problem, but rather the *simultaneous transmittal* of
many messages from many devices.

In other words - Don't [DDOS][ddos] your own services.

[ddos]: https://en.wikipedia.org/wiki/Denial-of-service_attack#Distributed_attack

## Time-Based Aggregation and Windowing

When using an event stream processor such as Azure Stream Analytics, you should
be familiar with the different types of windowing functions that are available.
Windowing functions provide a way to group together events that occur within a
period of time.

There are different types of windowing functions to choose from.  For example,
you could use a 1-hour *tumbling* window to see which hours have the most
activity.  Or, you might use a 5-minute *sliding* window to watch for trends in
the incoming data across groups of devices.  Other window types such as a
*hopping* window also have value in stream analysis.  You can read more about
windowing functions in the [ASA documentation][asa-windowing].

Also, keep in mind that the timestamp used by the windowing function in ASA is
determined by the `TIMESTAMP BY` keyword.  If you do not use that, ASA will
revert to the message arrival time.  Read more in the [ASA query language reference][asa-langref].

[asa-windowing]: https://msdn.microsoft.com/en-us/library/azure/dn835019.aspx
[asa-langref]: https://msdn.microsoft.com/en-us/library/azure/dn834998.aspx
