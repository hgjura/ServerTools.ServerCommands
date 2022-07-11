## About

ServerCommands facilitates running of units of code or commands remotely. It incorporates principles of messaging architectures used by most messaging tools and frameworks, like [Azure Service Bus](https://docs.microsoft.com/en-ca/azure/service-bus-messaging/), [AWS SQS](https://aws.amazon.com/sqs/), [RabbitMQ](https://www.rabbitmq.com/), or [Azure Storage Queues](https://docs.microsoft.com/en-ca/azure/storage/queues/storage-dotnet-how-to-use-queues?tabs=dotnet), [Apache Kafka](https://kafka.apache.org/) without any of the knowledge and configuration expertise to manage such installations and configurations. 

This library is a spcific implementation that uses [Azure Storage Queues](https://docs.microsoft.com/en-ca/azure/storage/queues/storage-dotnet-how-to-use-queues?tabs=dotnet). Other implementations are listed below. If you want to create your own extension skip to the section on How to create your own extension to this library.

## Implementations

* [Azure Storage Queues](https://www.nuget.org/packages/ServerTools.ServerCommands.AzureStorageQueues/)
* [Azure Service Bus](https://www.nuget.org/packages/ServerTools.ServerCommands.AzureServiceBus/)
* AWS SQS (coming soon)
* RabbitMQ (coming soon)
* Apache Kafka (coming soon)

More documentation is available at the [ServerCommands](https://github.com/hgjura/ServerTools.ServerCommands).


## How to Use

To post a command:

```csharp
var logger = new DebugLoggerProvider().CreateLogger("default");
var prefix = "sample";
var as_accountname = Environment.GetEnvironmentVariable["AccountName"]; //this is the account name for the Azure Storage
var as_accountkey = Environment.GetEnvironmentVariable["AccountKey"]; //this is the account key for the Azure Storage

var connection_options = new AzureStorageQueuesConnectionOptions(as_accountname, as_accountkey, MaxDequeueCountForError: 3, Log: logger, QueueNamePrefix: prefix);


var c = await new CloudCommands().InitializeAsync(new CommandContainer(), connection_options);

_ = await c.PostCommandAsync<AddNumbersCommand>(new { Number1 = 2, Number2 = 3 });
//or _ = await c.PostCommandAsync(typeof(AddNumbersCommand), new { Number1 = 2, Number2 = 3 });

```

To execute commands, responses or deadletter queues:

```csharp

var logger = new DebugLoggerProvider().CreateLogger("default");

var retry_policy = Policy
   .Handle<Exception>()
   .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (result, timeSpan, retryCount, context) =>
   {
       logger?.LogWarning($"Calling service failed [{result.Message} | {result.InnerException?.Message}]. Waiting {timeSpan} before next retry. Retry attempt {retryCount}.");
   }); //This is your retry policy. It retries 5 times using exponential backoff. If not set, or set to null, the default is similar: it tries 3 times with exponential backoff 

var prefix = "sample"; //this is the prefix that will be added to the queues created by the package

var maxDequeueCountForError = 3; //this is the number of times a message will be dequeued before sent to DLQ. If not set, defaults to 5.
var maxMessagesToRetrieve = 50; //this is the number of messages read by the queue at once.  If not set, defaults to 32.
var as_accountname = Environment.GetEnvironmentVariable["AccountName"]; //this is the account name for the Azure Storage
var as_accountkey = Environment.GetEnvironmentVariable["AccountKey"]; //this is the account key for the Azure Storage
var visibilityTimeout = TimeSpan.FromSeconds(20); //this is the amount of time the message will be invisible after if dequeued. If not set, defaults to 60 seconds.

var connection_options = new AzureStorageQueuesConnectionOptions(as_accountname, as_accountkey, , MaxDequeueCountForError: 3, Log: logger, RetryPolicy:retry_policy, QueueNamePrefix: prefix, MaxMessagesToRetrieve: maxMessagesToRetrieve, VisibilityTimeout: visibilityTimeout);

var _container = new CommandContainer();

_container
   .Use(logger)
   .RegisterCommand<AddNumbersCommand>()
   .RegisterResponse<AddNumbersCommand, AddNumbersResponse>();


var c = await new CloudCommands().InitializeAsync(_container, connection_options);

var result = await c.ExecuteCommandsAsync();

//check if something was wrong or if any items were processed at all
Assert.IsTrue(!result.Item1);

//check if 1 or more items were processed
Assert.IsTrue(result.Item2 > 0);

//check if there was any errors
Assert.IsTrue(result.Item3.Count > 0); //This value keeps the list of error messages that were encountered. After retrying 3 times the command is moved to the deadletterqueue.

//If a command generates a response, than you also execute and responses:

var responses = commands.ExecuteResponsesAsync();

Assert.IsTrue(!responses.Item1);
Assert.IsTrue(responses.Item2 > 0);

```


And that's that!

For more detailed documentation and more complex use cases head to the offical documentation at [the GitHub repo](https://github.com/hgjura/ServerTools.ServerCommands). If there are [questions](https://github.com/hgjura/ServerTools.ServerCommands/issues/new?assignees=hgjura&labels=question&title=ask%3A+) or [request new feautures](https://github.com/hgjura/ServerTools.ServerCommands/issues/new?assignees=hgjura&labels=request&title=newfeature%3A+) do not hesitate to post them there.

## How to create your own implementation

To create your own implementation of this functionality, using your own messaging service or storage, go to the base package [Server.ServerCommands](https://www.nuget.org/packages/ServerTools.ServerCommands/) and follow the instructions there. 


## Key Features
* Cross-usability between services
* Cross-functionality between clouds or on-prem
* Enhanced simplicity
* Asynchroneous remote execution
* Batching and correlation of commands
* Commands with remote response execution
* High performance
* Supports .NET 6.0+

## Related Packages

* Azure.Storage.Queues: [GitHub](https://github.com/Azure/azure-sdk-for-net) | [Nuget](Azure.Storage.Queues)
* Microsoft.Extensions.Logging: [Nuget](https://www.nuget.org/packages/Microsoft.Extensions.Logging)
* DryIoc: [GitHub](https://github.com/dadhi/DryIoc) | [Nuget](https://www.nuget.org/packages/DryIoc.dll/)
* Polly: [GitHub](https://github.com/App-vNext/Polly) | [Nuget](https://www.nuget.org/packages/polly)


## Feedback

ServerCommands is released as open source under the [MIT license](https://github.com/hgjura/ServerTools.ServerCommands/blob/main/LICENSE). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/hgjura/ServerTools.ServerCommands/issues).


