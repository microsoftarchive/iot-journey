# Data Pipeline Guidance (with Apache Storm)
[Microsoft patterns & practices](http://aka.ms/mspnp)

This project focuses on using Apache Storm/Trident with Java. For guidance on
using .NET _without_ Storm, see the companion
[Data Pipeline Guidance](https://github.com/mspnp/data-pipeline).

## Overview

The two primary concerns of this project are:

* Facilitating cold storage of data for later analytics.
That is, translating the _chatty_ stream of events into _chunky_ blobs.

* Demonstrate how to use OpaqueTridentEventHubSpout and
Apache Storm/Trident to store Microsoft Azure Eventhub messages to Microsoft Azure
Blob exactly-once.

## Next Steps

* [Architecture Overview](/docs/ArchitectureOverview.md)
* [Getting Started](/docs/GettingStarted.md)
* [Create Java Topology Project eventhub-blobwriter from Scratch](/docs/step-by-step-walkthrough.md)
* [Design Considerations and Technical How-To](/docs/DesignConsiderations.md)

## Backlog

* **Performance Resut**: The performance result will be published once we finishes the performance test.

* **Using Zookeeper to store the state**: The current sample stores state in Redis Cache. We plan to replace that with Zookeeper.
