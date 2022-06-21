## About

ServerCommands facilitates running of units of code or commands remotely. It incorporates principles of messaging architectures used by most messaging tools and frameworks, like [Azure Service Bus](https://docs.microsoft.com/en-ca/azure/service-bus-messaging/), [AWS SQS](https://aws.amazon.com/sqs/), [RabbitMQ](https://www.rabbitmq.com/), or [Azure Storage Queues](https://docs.microsoft.com/en-ca/azure/storage/queues/storage-dotnet-how-to-use-queues?tabs=dotnet), [Apache Kafka](https://kafka.apache.org/) without any of the knowledge and configuration expertise to manage such installations and configurations. 

This library (ServerTools.ServerCommands ) is the core library that is used by all the specicif implementations (currently, for Azure Service Bus and Azure Storage Queues). In itself this library does not provide any specific functionality, unless you want to extend from it and create your own implementations of any services that are not listed below.

## Implementations

* [Azure Storage Queues]()
* [Azure Service Bus]()
* AWS SQS (coming soon)
* RabbitMQ (coming soon)
* Apache Kafka (coming soon)

More documentation is available at the [ServerCommands](https://github.com/hgjura/ServerTools.ServerCommands).


## How to extend this library and create your own implementation

If you want to extend this library, you will need to implement the ```ICloudCommands``` interface. Creating a messaging platform and architecture from scratch is a daunting task so I suggest yuo wor with some of the existing implementatins libraries and servcies above, before creating your own.

The implementation goes as follows:

- Create the rampup / rampdown functionality.
1) Need to extent your own implemnetation of the ```ConnectionOptions``` class. This provides any connectivity details to the library, and thisng like Retry Policies and logger details.
2) Implement the ``` Task<ICloudCommands> InitializeAsync(CommandContainer Container, ConnectionOptions ConnectionOptions)``` method to ramp up the service and initialize all the infrastructure to support the activities.
3) Implement the ```ClearAllAsync()``` method to ramp down the service and undo what the ```InitializeAsync``` does.
Note: Create a parameterless empty constructor, and do not add anything to the contrustor, as most of these ramp-up calls need to be async. 

- Create command functionality
1) Need to implement the following self-explenatory methods that posts, executes commands and handle the deadleter queue..
```PostCommandAsync<T>(dynamic CommandContext, CommandMetadata PreviousMatadata = new())```, 
```PostCommandAsync(Type type, dynamic CommandContext, CommandMetadata PreviousMatadata = new())```, 
```PostCommandAsync(string type_name, dynamic CommandContext, CommandMetadata PreviousMatadata = new())```, 
````PostCommandAsync(Message message)```.
```ExecuteCommandsAsync(int timeWindowinMinutes = 1)```, 
```HandleCommandsDlqAsync(Func<Message, bool> ValidateProcessing = null, int timeWindowinMinutes = 1)```.

- Create response functionality
1) Need to implement the following self-explenatory methods that posts, executes responses and handle the deadleter queue..
```PostResponseAsync<T>(dynamic ResponseContext, CommandMetadata OriginalCommandMetadata)```, 
```PostResponseAsync(Type ResponseType, dynamic ResponseContext, CommandMetadata OriginalCommandMetadata)```, 
```ExecuteResponsesAsync(int timeWindowinMinutes = 1)```, 
```HandleResponsesDlqAsync(Func<Message, bool> ValidateProcessing = null, int timeWindowinMinutes = 1)```.


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
