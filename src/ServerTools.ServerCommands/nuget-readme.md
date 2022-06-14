## About

ServerCommands facilitates running of units of code or commands remotely. It incorporates principles of messaging architectures used by most messaging tools and frameworks, like [Azure Service Bus](https://docs.microsoft.com/en-ca/azure/service-bus-messaging/), [AWS SQS](https://aws.amazon.com/sqs/), [RabbitMQ](https://www.rabbitmq.com/), or [Azure Storage Queues](https://docs.microsoft.com/en-ca/azure/storage/queues/storage-dotnet-how-to-use-queues?tabs=dotnet), [Apache Kafka](https://kafka.apache.org/) without any of the knowledge and configuration expertise to manage such installations and configurations. 

Currently, the library requires an Azure Storage account to run. But by no means this is a major dependency. All messaging services work very similarly, and the choice to use Azure Storage is purely for simplictiy and cost. Azure Storage provides both storage and queueing service at a minimal cost. In future iterations, and upon demand, versions of this library that work with all the other messaging services will be provided.  


More documentation is available at the [ServerCommands](https://github.com/hgjura/ServerTools.ServerCommands).

## How to Use

To post a command:
```csharp

var c = new Commands(new CommandContainer(), Environment.GetEnvironmentVariable("StorageAccounName"), Environment.GetEnvironmentVariable("StorageAccountKey"));

_ = await c.PostCommandAsync<AddNumbersCommand>(new { Number1 = 2, Number2 = 3 });
//or _ = await c.PostCommandAsync(typeof(AddNumbersCommand), new { Number1 = 2, Number2 = 3 });

```

To execute commands:

```csharp
var _container = new CommandContainer();

_container.RegisterCommand<AddNumbersCommand>();

var c = new Commands(_container, Environment.GetEnvironmentVariable("StorageAccounName"), Environment.GetEnvironmentVariable("StorageAccountKey"));

var result = await c.ExecuteCommands();

//check if something was wrong or if any items were processed at all
Assert.IsTrue(!result.Item1);

//check if 1 or more items were processed
Assert.IsTrue(result.Item2 > 0);

//check if there was any errors
Assert.IsTrue(result.Item3.Count > 0); //This value keeps the list of error messages that were encountered. After retrying 5 times the command is moved to the deadletterqueue.

```

And that's that!

For more detailed documentation and more complex use cases head to the offical documentation at [the GitHub repo](https://github.com/hgjura/ServerTools.ServerCommands). If theer are [questions](https://github.com/hgjura/ServerTools.ServerCommands/issues/new?assignees=hgjura&labels=question&title=ask%3A+) or [request new feautures](https://github.com/hgjura/ServerTools.ServerCommands/issues/new?assignees=hgjura&labels=request&title=newfeature%3A+) do not hesitate to post them there.


## Key Features

* Enhanced simplicity
* Asynchroneous remote execution
* Batching and correlation of commands
* Commands with remote response execution
* High performance
* Supports .NET 5.0+

## Related Packages

* Azure.Storage.Queues: [GitHub](https://github.com/Azure/azure-sdk-for-net) | [Nuget](Azure.Storage.Queues)
* Microsoft.Extensions.Logging: [Nuget](https://www.nuget.org/packages/Microsoft.Extensions.Logging)
* DryIoc: [GitHub](https://github.com/dadhi/DryIoc) | [Nuget](https://www.nuget.org/packages/DryIoc.dll/)
* Polly: [GitHub](https://github.com/App-vNext/Polly) | [Nuget](https://www.nuget.org/packages/polly)


## Feedback

ServerCommands is released as open source under the [MIT license](https://github.com/hgjura/ServerTools.ServerCommands/blob/main/LICENSE). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/hgjura/ServerTools.ServerCommands/issues).
