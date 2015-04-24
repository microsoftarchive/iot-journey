# Scenario

Here we'll describe the high-level business motivations for the project. This
does not need to realistic, but rather representative.
We can also list of the phases of development and the goals for each phase.
e.g.,

> **Phase 1**: Proof of concept. Can we do what we think we can?

## Narrative

Our story has three significant “characters”:
 1. Apartment building owners
 1. Tenants
 1. A startup company hoping to be a smart-building service provider

Our focus will mostly be on the fictitious startup: Contoso Environmental Monitoring Services. 
They plan to provide environmental monitoring services to the apartment building owners.
Once contracted, they would install devices in all of the apartments belonging to a building owner. These devices would report on temperature, humidity, smoke alarm and other environmental conditions.
- With the data they collect Contoso provides services to the building owners:
- Fire alerts
- Flood alerts (broken pipes, slow leaks, etc.)
- Unexpected temperature changes (indicating a broken HVAC)
- etc.

In addition, individual tenant can view the current state of their own apartment through a mobile app.

## Contoso Building Management

Digital Thermostats

  - Temperature
  - Humidity
  - Health (up-time, firmware version, etc.)

Deployed in apartment complexes, offices, high-rise buildings, etc.

≈ 100k devices

## Users

- Building Owners
- Landlords / Management
- Residents / Occupants
- Maintenance Staff

## Scope
- Devices are read-only.  (No command and control)
- Devices speak modern protocols. (No field gateway)

## Protocols
- Phase 1 - Devices will speak AMQP + JSON
- Phase 2 - Mix in MQTT + ProtoBuf devices, using a protocol translation layer

## User Interfaces
- Provisioning UI web app
- Device Status UI mobile app (Xamarin)
- Power BI Dashboard

## Back End Services
- Cloud Gateway
- Device Registry
- Event Hubs

## Data Destinations
- SQL Server
- Azure Stream Analytics
- HDInsight

## Principles
- No one-offs.  (multi-user, multi-sensor, multi-protocol etc.)
- Simple implementation, should apply to other scenarios.
