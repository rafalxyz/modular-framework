# Modular Framework

![Overview](https://raw.githubusercontent.com/devmentors/modular-framework/master/assets/modules.png)

## About

Modular framework is a set of helpful components for building the modular applications, especially based on [modular monolith](https://www.infoq.com/news/2019/09/design-ddd-modular-monolith/) architecture. The code is written in C# language using the latest version of .NET framework for cross-platform capabilities. The project isn't published as NuGet package, so it's best to clone it and adjust to your own needs. Solution is split into 2 separate projects:
 - **Abstractions** - mostly set of interfaces that can be easily injected into your code
 - **Infrastructure** - the actual code implementing the provided public abstractions

Within the modular framework, you can find the following components:

- Dynamic module loading & configuration
- Autonomous  module clients for communication & integration
- Command, Query, Event handlers & dispatchers
- Message broker - sync & async, background broker with the usage of channels
- Application & Correlation context metadata
- Local contracts rules & validation
- JWT authentication extensions with cookie & certificate support
- Exception handling & mappers
- Domain event handlers & dispatcher
- Transactional decorators for distinct schema-based unit of work
- Logging decorators for tracking the requests & messages
- Inbox & Outbox support for EF Core + PostgreSQL
- Security - encryption, hashing
- Redis caching extensions 

## Samples

- [Confab](https://github.com/devmentors/Confab) - conference management system + UI
- [Trill](https://github.com/devmentors/Trill-modular-monolith) - simple Twitter clone + UI
- [Inflow](https://github.com/devmentors/Inflow) - virtual payments platform
- [Template](https://github.com/devmentors/modular-monolith-template) - basic template with empty modules
  
## Tutorials

- [Building Modular Monolith](https://www.youtube.com/c/DevMentors/videos) series on YouTube
- [Modular Monolith](https://devmentors.io/courses) premium course - coming soon!