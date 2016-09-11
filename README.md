# IoT Journey
_An exploration into building a solution in the cloud_

:memo: **This project is not under active development.** 
Please see the  [**Azure IoT Hub**](https://azure.microsoft.com/en-us/documentation/services/iot-hub/) and [Azure IoT Suite](https://www.azureiotsuite.com/) for Microsoft's latest offerings in this space. 

## Why

There is no one-size-fits-all answer when it comes to building an [IoT solution][intro-to-iot].
Our approach to guidance is to embark on a collaborative journey into
understanding the mechanics and challenges surrounding an end-to-end system.
Our purpose is _not_ to tell you all the answers that you'll need, but rather
to help you ask the right questions.

## What

We will be constructing an IoT solution hosted in Azure. 
Our focus will be on problems related to the back-end architecture, such as:
- high-scale event ingestion
- event stream processing
- facilitation of analysis

We have identified a number of high-level scenarios; described and explored in the [docs][/docs]. We will be using the same tools and services that are available to you. 
We are creating a set of _reference implementations_ (our fancy name for an
end-to-end sample). Some of the reference implementation are meant to work in concert while others are alternative implementations.

In addition to the **source code**, we'll also produce a **set of written
articles** covering the general concepts and patterns, the rationale behind
design choices, and a few other things to help you navigate the guidance.

We intend this to be an interactive act of discovery.

## How

We are basing our scenarios on requirements we've gathered from customers and advisors. The scenario is not meant to be realistic, but rather representative. That is, it should represent the most common needs and [challenges][] encountered in this space.

We are very interested in your feedback. We are publishing updates about the work on [our video blog](https://channel9.msdn.com/blogs/mspnp). We encourage you to ask questions in the chat room or to open issue on this repo.
Any and all feedback is welcome.

## Who

Our intended audience for this guidance is any senior developer or architect interested in developing an IoT solution. 
We want all developers, regardless of their preferred development stack, to benefit from the written guidance.
If you feel that there is anything more that we can do to make this guidance accessible to a broader audience, you are encouraged to share.

## Next Step

- Go to the [docs](docs) folder, review the readme, and begin reviewing the content.

## Problems, Concerns, and Feedback
If something doesn't make sense, start with the [FAQ](FAQ.md).
If that doesn't help, open an issue.
If you want to contribute directly, please review our
[contribution guidelines](CONTRIBUTING.md).

| Current Backlog Status 
| :------
| [![Ready](https://badge.waffle.io/mspnp/iot-journey.svg?label=ready&title=Ready)](https://waffle.io/mspnp/iot-journey)
| [![In Progress](https://badge.waffle.io/mspnp/iot-journey.svg?label=in progress&title=In Progress)](https://waffle.io/mspnp/iot-journey)
| [![Awaiting Review](https://badge.waffle.io/mspnp/iot-journey.svg?label=awaiting-review&title=Awaiting Review)](https://waffle.io/mspnp/iot-journey)

[intro-to-iot]: docs/01-what-is-an-IoT-solution.md
