![Microsoft patterns & practices](http://pnp.azurewebsites.net/images/pnp-logo.png)
# IoT Journey

## Why

There is no one-size-fits-all answer when it comes to building an IoT solution.
Our approach to guidance is to embark on a collaborative journey into
understanding the mechanics and challenges surrounding an end-to-end system.
Our purpose is _not_ to tell you all the answers that you'll need, but rather
to help you ask the right questions.

## What

We will be constructing an IoT solution hosted in Azure. We will be using the
same tools and services that are available to you. Instead of a single final
snapshot of our source code, we'll be sharing the history and intermediate
[releases][]. We'll grow the _reference implementation_ (our fancy name for an
end-to-end sample) over time; responding to new business requirements and
taking advantage of new services as they are released.

In addition to the **source code**, we'll also produce a **set of written
articles** covering the general concepts and patterns, the rationale behind
design choices, and a few other things to help you navigate the guidance.

We intend this to be an interactive act of discovery.

## How

We've constructed a [scenario][] that reflects business requirements we've
gathered from customers and advisors. The scenario is not meant to be
realistic, but rather representative. That is, it should represent the most
command needs and [challenges][] you will face.

:memo: (We'd like your immediate feedback on the [scenario][] to help make sure
that it is truly representative.)

We'll use this scenario to define our [backlog][] for the reference
implementation. Both the scenario and backlog will change over time. We'll
deliberately break the scenario up into [milestones][]. Each milestone will have
a specific set of goals, deliverables, and target date. We will tag the source
as a [release][releases] at the end of each milestone. Our engineering team will
be working in two week iterations against the backlog.

We will also establish an advisory council with regular meetings. The council
will be asked to continuously review our work and provide critical feedback.

Likewise, we intend to keep the conversation open. Any and all feedback is
welcome.

## Who

Our intended audience for this guidance is any senior developer or architect
interested in developing an IoT solution. Our reference implementation will
primarily target the .NET platform, however we will aim to make the written
guidance as _platform agnostic_ as we reasonably can. It is likely that we will
be discussing various open source and non-.NET technologies as well.
If you feel that there is anything more that we can do to make this guidance
accessible to a broader audience, you are encouraged to share.

## FAQ

1. Will this result in an IoT library? Will there be a set of reusable bits or
a framework that I use to build my solution?

  _No, we do not intend to produce any NuGet packages, assemblies, or services
  as part of this project._

1. If this is an IoT project, why is the focus on the backend services and not
so much about devices?

  _In many way, an IoT solution is all about the data. Devices on the edge
produce the data. Backend services ingest, process, and analyze the data. Our
initial focus in this project is on the ingestion and processing of the data.
These are parts of the solution that can be hosted on Azure._

1. I haven't seen any activity on this project for some number of days? Does
that mean it's dead?

  _From time to time we'll let this guidance hibernate while we work on other
things. As new technologies and services become available, we'll rouse the
project and continue the effort. We will keep the status updated on this repo so
you can know what to expect._

[scenario]: docs/Scenario.md
[challenges]: docs/Challenges.md
[backlog]: https://github.com/mspnp/iot-journey/issues
[milestones]: https://github.com/mspnp/iot-journey/milestones
[releases]: https://help.github.com/articles/about-releases/
