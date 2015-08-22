# What is an IoT solution?

When talking about the _Internet of Things_ or _IoT_, the trend is to focus on "smart" devices and how they communicate. For example, Wikipedia [defines][wikipedia-iot] IoT as a network of physical objects with embedded software, sensors, and connectivity. This is a broad definition; we're interested in concrete systems that target specific business needs.

In our context, when we talk about an IoT _solution_, we mean a system that typically has these general characteristics:

- There is a set of devices that generate data.
- There is a need to interact with and manage these devices.
- There is a cloud-hosted backend that ingests and processes data from the devices.
- The volume of data is large and it's generated at a high velocity.
- The system needs to detect business-relevant events and react in a timely manner.
- The system is inherently distributed.

These are very high-level characteristics and there is always room for exceptions. 

These kinds of solutions are applicable across many different industries and business domains. Popular examples include smart buildings, connected cars, and industrial automation. These solutions are both enriching existing business models and enabling new ones.

To learn more about the potential relevance of IoT for your business, additional resources are available at [InternetOfYourThings.com][your-things].

## What's really new about IoT?

Most of the concepts in IoT are not new. Many of the characteristics listed above have been present in manufacturing, utilities management, and other industries for decades. If you search for terms like "industrial control systems (ICS)" or "supervisory control and data acquisition (SCADA)", you'll find a wealth of knowledge.

Nevertheless, something is new.

The cost of developing IoT solutions has dramatically decreased. On the device side, even a hobbyist developer can now easily afford to prototype custom devices. Meanwhile, the rise of the cloud has made high-scale backends economically feasible.

The infrastructure needed to support these systems is now commonplace. The Internet is ubiquitous and there is an increasing abundance of bandwidth. There is also a proliferation of hardware manufacturers, development tools, and programming stacks targeting IoT solutions. There's a surge of interested communities, standards bodies, and interested businesses.

The democratization of IoT brings together developer communities that were traditionally separate domains &mdash; whether enterprise, consumer software, control systems, or embedded devices. Each of these communities brings their own insights (and possibly oversights) to the world of IoT.

For example, the embedded systems developer, who understands the inaccessibility of a resource-constrained device deployed to a remote location, will often emphasize the need for thorough up-front planning. Once a device is deployed, it might be in the field for years. On the other hand, a developer with a background in cloud-hosted web services might favor a ["release early, release often"][rero] philosophy. Feedback is a critical mechanism for evolutionary design.

We raise this point about differing perspectives, because a successful IoT solution will need to incorporate ideas from many sources. This is especially true with respect to the question of security in IoT.

## Further information

- [Internet of Things Overview](http://channel9.msdn.com/Events/Build/2015/2-652)
- [Best practices for creating IoT solutions with Azure](http://blogs.microsoft.com/iot/2015/04/30/best-practices-for-creating-iot-solutions-with-azure/)
- [IoT Security Fundamentals](http://channel9.msdn.com/Events/Ignite/2015/BRK4553)

[wikipedia-iot]: https://en.wikipedia.org/wiki/Internet_of_Things
[your-things]: http://www.internetofyourthings.com
[rero]: https://en.wikipedia.org/wiki/Release_early,_release_often