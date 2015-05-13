# Scenario

This is a fictitious scenario that aims to reflect common business
requirements.
The scenario is not meant to be _realistic_, but rather _representative_.

This scenario will drive our [backlog][]. As we progress, we will expand the
scenario by adding new [milestones][]. Each milestone will have a specific set
of goals, deliverables, and target date.

## Narrative

Our narrative has three significant “characters”:
 1. Fabrikam; a startup hoping to be a smart-building service provider
 1. The owners of the apartment building
 1. The tenant who live in the apartment buildings

Fabrikam plans to provide "environmental monitoring services" on a contractual
basis. Once contracted, they would install devices in apartment belonging to a
building owner. These devices would report on temperature, humidity, smoke
alarm and other environmental conditions.

Fabrikam will use the collected data to provide various services to the
building owners related to:
- Cost-saving on utilities
- Fire alerts
- Flood alerts (broken pipes, slow leaks, etc.)
- Unexpected temperature changes (indicating a broken HVAC)
- and more

In addition, Fabrikam thinks that giving individual tenants the ability to view
the current state of their own apartment through a mobile app can provide value.

Fabrikam landed their first contact before they even finished their prototype.
They now have a few months to build system hosted on Azure that will support:
- 100,000 provisioned devices
- sending approximately 1 event per minute

## Milestones

### [The Prototype](https://github.com/mspnp/iot-journey/milestones/Milestone%2001)

Fabrikam's engineering team has very little experience with high-scale
event-oriented systems. They want to quickly prove to themselves that they can
deliver. For their first milestone they want to:
- Ingest events from simulated devices
- Store the events for later analytics (cold storage)
- Increase the scale until they match the targets for there first contract

In addition, they also want to answer some fundamental questions they'll need
for future milestones.
- What questions should we be asking in order to be succcessful?
- [What protocol should our devices use to communicate with our backend?][protocol-choice]
- [How should we serialize the data that our devices are sending?][serialziation-choice]

[protocol-choice]: https://github.com/mspnp/iot-journey/issues/3
[serialziation-choice]: https://github.com/mspnp/iot-journey/issues/4
[backlog]: https://github.com/mspnp/iot-journey/issues
[milestones]: https://github.com/mspnp/iot-journey/milestones
