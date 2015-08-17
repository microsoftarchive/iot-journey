# What is an IoT solution?

When talking about the _Internet of Things_ or _IoT_, the trend is to focus on "smart" devices and how they communicate. To paraphrase [Wikipedia's definition][wikipedia-iot], IoT is the network of physical objects or "things" embedded with electronics, software, sensors, and connectivity to enable objects to exchange data. Of course, that is an intentionally broad definition meant to describe _the_ Internet of Things. We're interested in concrete systems built around this broad idea that target specific business needs.

In our context, when we talk about building an IoT _solution_ we are specifically talking about a system that typically has these general characteristics:
- There is a set of devices generating data.
- There is a need to interact with and manage the devices.
- There is a cloud-hosted backend that ingests and processes data from the devices.
- The volume of data is large and it's generated at a high velocity.
- The system needs to detect business-relevant events and react in a timely manner.
- The system is inherently distributed.

These are very high level characteristics and there is always room for exceptions. 

These kinds of solutions are applicable across many different industries and business domains. Popular examples include smart buildings, connected cars, and industrial automation. These solutions are both enriching existing business models and enabling new ones.

If you are interested in learning more about the potential relevance of IoT for your business, additional resources are available at [InternetOfYourThings.com][your-things] .

## What's really new about IoT?

Most of the concepts and techniques for building these types of solutions are not new. Systems supporting an array of devices generating and transmitting data have been in use for decades. Many of the characteristics mentioned have been present in manufacturing, utilities management, and other industries. Search for terms like "industrial control systems (ICS)" or "supervisory control and data acquisition (SCADA)" and you'll find a wealth of knowledge.

Nevertheless, something is new.

The cost of developing IoT solutions has dramatically decreased. Even a hobbyist developer can now easily afford to prototype custom devices. Likewise, the rise of the cloud has made high-scale backends economically feasible.
The infrastructure needed to support these kinds of systems is now common place. The Internet has become ubiquitous and there is an increasing abundance of bandwidth.
There is also a proliferation of hardware manufacturers, development tools and programming stacks targeting IoT solutions. There's a surge of interested communities, standards bodies, and interested businesses.

This democratization of IoT has produced a collision of worlds. Traditionally separated developer communities, with different experiences and philosophies, are beginning to collaborate on IoT solutions. These different backgrounds, line-of-business, enterprise, consumer software, control systems, embedded devices and others, each bring their own insights (and oversights) to IoT. There are differences in strategy, culture, and concerns.

For example, the embedded systems developer, who understands the inaccessibility of a resource-constrained device deployed to a remote location, will often emphasize the need for thorough up-front planning. Once a device is deployed, it might be in the field for years. On the other hand, a developer with a background in cloud-hosted web services might favor a ["release early, release often"][rero] philosophy. Feedback is a critical mechanism for evolutionary design.

We raise this topic about differing perspectives, because a successful IoT solution will need to incorporate ideas from many sources. This is especially true with respect to the question of security in IoT.

## Further information

- [Internet of Things Overview](http://channel9.msdn.com/Events/Build/2015/2-652)

- [Best practices for creating IoT solutions with Azure](http://blogs.microsoft.com/iot/2015/04/30/best-practices-for-creating-iot-solutions-with-azure/)

- [IoT Security Fundamentals](http://channel9.msdn.com/Events/Ignite/2015/BRK4553)

[wikipedia-iot]: https://en.wikipedia.org/wiki/Internet_of_Things
[your-things]: http://www.internetofyourthings.com
[rero]: https://en.wikipedia.org/wiki/Release_early,_release_often