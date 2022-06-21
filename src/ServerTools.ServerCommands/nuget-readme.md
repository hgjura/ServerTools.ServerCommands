## About

ServerCommands facilitates running of units of code or commands remotely. It incorporates principles of messaging architectures used by most messaging tools and frameworks, like [Azure Service Bus](https://docs.microsoft.com/en-ca/azure/service-bus-messaging/), [AWS SQS](https://aws.amazon.com/sqs/), [RabbitMQ](https://www.rabbitmq.com/), or [Azure Storage Queues](https://docs.microsoft.com/en-ca/azure/storage/queues/storage-dotnet-how-to-use-queues?tabs=dotnet), [Apache Kafka](https://kafka.apache.org/) without any of the knowledge and configuration expertise to manage such installations and configurations. 

This library is a spcific implementation that uses [Azure Storage Queues](https://docs.microsoft.com/en-ca/azure/storage/queues/storage-dotnet-how-to-use-queues?tabs=dotnet). 

Currently, the library ServerTools.ServerCommands is the core base library that is used by the more specific libraries below. In itself this library does not provide any specific functionality, unless you want to extend from it and create your own implementations of any services that are not listed below.

## Implementations

* [Azure Storage Queues]()
* [Azure Service Bus]()
* AWS SQS (coming soon)
* RabbitMQ (coming soon)
* Apache Kafka (coming soon)

More documentation is available at the [ServerCommands](https://github.com/hgjura/ServerTools.ServerCommands).


## Key Features

* Enhanced simplicity
* Asynchroneous remote execution
* Batching and correlation of commands
* Commands with remote response execution
* High performance
* Supports .NET 5.0+

## Related Packages

* Microsoft.Extensions.Logging: [Nuget](https://www.nuget.org/packages/Microsoft.Extensions.Logging)
* DryIoc: [GitHub](https://github.com/dadhi/DryIoc) | [Nuget](https://www.nuget.org/packages/DryIoc.dll/)
* Polly: [GitHub](https://github.com/App-vNext/Polly) | [Nuget](https://www.nuget.org/packages/polly)


## Feedback

ServerCommands is released as open source under the [MIT license](https://github.com/hgjura/ServerTools.ServerCommands/blob/main/LICENSE). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/hgjura/ServerTools.ServerCommands/issues).
