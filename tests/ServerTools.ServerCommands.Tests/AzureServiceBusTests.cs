using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;
using ServerTools.ServerCommands.AzureServiceBus;
using ServerTools.ServerCommands.AzureStorageQueues;
using System;
using System.Threading.Tasks;

namespace ServerTools.ServerCommands.Tests
{
    [TestClass]
    public class AzureServiceBusTests
    {
        static CommandContainer _container;
        static string _queueNamePrefix;
        static ICloudCommands commands;
        static ILogger logger;
        static IConfiguration Configuration { get; set; }


        /// <summary>
        /// Execute once before the test-suite
        /// </summary>
        /// 

        [ClassInitialize()]
        public static async Task InitTestSuiteAsync(TestContext testContext)
        {

            try
            {

                var builder = new ConfigurationBuilder()
                 .AddUserSecrets<UnitTestsForServerCommands>(true)
                 .AddJsonFile("local.tests.settings.json", true);

                Configuration = builder.Build();

                logger = new DebugLoggerProvider().CreateLogger("default");

                _container = new CommandContainer();
                _queueNamePrefix = nameof(UnitTestsForServerCommands).ToLower();
                //commands = await new AzureServiceBus.CloudCommands().InitializeAsync(_container, new AzureServiceBusConnectionOptions(Configuration["ASBConnectionString"], AzureServiceBusTier.Standard, 3, logger, DefaultMessageTimeToLive: TimeSpan.FromMinutes(2), QueueNamePrefix: _queueNamePrefix, MaxWaitTime: TimeSpan.FromSeconds(10), RequiresSession: true));

            }
            catch (System.Exception ex)
            { 
                throw;
            }
         }

        [ClassCleanup()]
        public static async Task CleanTestSuiteAsync()
        {
            // commands.ClearAllAsync();
        }


        [TestMethod]
        public async Task I1000_TestIntegrationWithServiceBusAsync()
        {
            //_container
            //    .Use(logger)

            //    //.RegisterCommand<AddNumbersCommand>();
            //    //.RegisterResponse<AddNumbersResponse>();

            //    .RegisterCommand<AddNumbersCommand, AddNumbersResponse>();

            //    //.RegisterResponse<AddNumbersCommand, AddNumbersResponse>();

            //    //the three ways above to registering a command + response above are equivalent

            //_ = commands.PostCommandAsync<AddNumbersCommand>(new { Number1 = 2, Number2 = 3 });

            //var result1 = await commands.ExecuteCommandsAsync();
            //var result2 = await commands.ExecuteResponsesAsync();

            //var logger = new DebugLoggerProvider().CreateLogger("default");
            //var prefix = "sample";
            //var asb_connectionstring = Environment.GetEnvironmentVariable["ASBConnectionString"]; //this is the connections string for the Azure Service Bus

            //var connection_options = new AzureServiceBusConnectionOptions(asb_connectionstring, MaxDequeueCountForError: 3, Log: logger, QueueNamePrefix: prefix);


            //var c = await new CloudCommands().InitializeAsync(new CommandContainer(), connection_options);

            //_ = await c.PostCommandAsync<AddNumbersCommand>(new { Number1 = 2, Number2 = 3 });
            ////or _ = await c.PostCommandAsync(typeof(AddNumbersCommand), new { Number1 = 2, Number2 = 3 });

            //var logger = new DebugLoggerProvider().CreateLogger("default");
            //var prefix = "sample";
            //var asb_connectionstring = Environment.GetEnvironmentVariable["ASBConnectionString"]; //this is the connections string for the Azure Service Bus

            //var connection_options = new AzureServiceBusConnectionOptions(asb_connectionstring, MaxDequeueCountForError: 3, Log: logger, QueueNamePrefix: prefix);


            //var c = await new CloudCommands().InitializeAsync(new CommandContainer(), connection_options);

            //_ = await c.PostCommandAsync<AddNumbersCommand>(new { Number1 = 2, Number2 = 3 });
            ////or _ = await c.PostCommandAsync(typeof(AddNumbersCommand), new { Number1 = 2, Number2 = 3 });

            //var logger = new DebugLoggerProvider().CreateLogger("default");

            //var retry_policy = Policy
            //   .Handle<Exception>()
            //   .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (result, timeSpan, retryCount, context) =>
            //   {
            //       logger?.LogWarning($"Calling service failed [{result.Message} | {result.InnerException?.Message}]. Waiting {timeSpan} before next retry. Retry attempt {retryCount}.");
            //   }); //This is your retry policy. It retries 5 times using exponential backoff. If not set, or set to null, the default is similar: it tries 3 times with exponential backoff 

            //var prefix = "sample"; //this is the prefix that will be added to the queues created by the package

            //var maxDequeueCountForError = 3; //this is the number of times a message will be dequeued before sent to DLQ. If not set, defaults to 5.
            //var maxMessagesToRetrieve = 50; //this is the number of messages read by the queue at once.  If not set, defaults to 32.
            //var asb_connectionstring = Environment.GetEnvironmentVariable["ASBConnectionString"]; //this is the connections string for the Azure Service Bus
            //var maxWaitTime = TimeSpan.FromSeconds(10); //this is the minimum timewindow that the package keeps the connection to the ServiceBus open. If not set, defaults to 60 seconds.
            //var connection_options = new AzureServiceBusConnectionOptions(asb_connectionstring, MaxDequeueCountForError: 3, Log: logger, RetryPolicy:retry_policy, QueueNamePrefix: prefix, MaxMessagesToRetrieve: maxMessagesToRetrieve, MaxWaitTime: maxWaitTime);

            //var _container = new CommandContainer();

            //_container
            //   .Use(logger)
            //   .RegisterCommand<AddNumbersCommand>()
            //   .RegisterResponse<AddNumbersCommand, AddNumbersResponse>();


            //var c = await new CloudCommands().InitializeAsync(_container, connection_options);

            //var result = await c.ExecuteCommandsAsync();

            ////check if something was wrong or if any items were processed at all
            //Assert.IsTrue(!result.Item1);

            ////check if 1 or more items were processed
            //Assert.IsTrue(result.Item2 > 0);

            ////check if there was any errors
            //Assert.IsTrue(result.Item3.Count > 0); //This value keeps the list of error messages that were encountered. After retrying 3 times the command is moved to the deadletterqueue.

        }

        [TestMethod]
        public async Task I1001_TestIntegrationWithServiceBusAsyncOrderedMessages()
        {
            try
            {

                //var _container = new CommandContainer();

                //_container.RegisterCommand<AddNumbersCommand, AddNumbersResponse>();

                ////need to cast from IServerCommand to the actual AzureServiceBus.CloudCommaands, since the ICommand does not have the PostOrderedCommandAsync() (yet), but the implementati
                //var c = (ServerTools.ServerCommands.AzureServiceBus.CloudCommands) await new ServerTools.ServerCommands.AzureServiceBus.CloudCommands().InitializeAsync(_container, new AzureServiceBusConnectionOptions(Configuration["ASBConnectionString"], AzureServiceBusTier.Standard, 3, logger, DefaultMessageTimeToLive: TimeSpan.FromMinutes(2), QueueNamePrefix: _queueNamePrefix, MaxWaitTime: TimeSpan.FromSeconds(10), RequiresSession: true));


                //var session1 = Guid.NewGuid();
                //var session2 = Guid.NewGuid();

                //_ = await c.PostOrderedCommandAsync<AddNumbersCommand>(new { Number1 = 4, Number2 = 0 }, session1, 4, false);
                //_ = await c.PostOrderedCommandAsync<AddNumbersCommand>(new { Number1 = 2, Number2 = 0 }, session1, 2, false);
                //_ = await c.PostOrderedCommandAsync<AddNumbersCommand>(new { Number1 = 3, Number2 = 0 }, session1, 3, false);
                //_ = await c.PostOrderedCommandAsync<AddNumbersCommand>(new { Number1 = 5, Number2 = 0 }, session1, 5, true);
                //_ = await c.PostOrderedCommandAsync<AddNumbersCommand>(new { Number1 = 1, Number2 = 0 }, session1, 1, false);

                //_ = await c.PostOrderedCommandAsync<AddNumbersCommand>(new { Number1 = 4, Number2 = 0 }, session2, 4);
                //_ = await c.PostOrderedCommandAsync<AddNumbersCommand>(new { Number1 = 2, Number2 = 0 }, session2, 2);
                //_ = await c.PostOrderedCommandAsync<AddNumbersCommand>(new { Number1 = 3, Number2 = 0 }, session2, 3);
                //_ = await c.PostOrderedCommandAsync<AddNumbersCommand>(new { Number1 = 5, Number2 = 0 }, session2, 5, true);
                //_ = await c.PostOrderedCommandAsync<AddNumbersCommand>(new { Number1 = 1, Number2 = 0 }, session2, 1);

            
                //var result = await c.ExecuteCommandsAsync();
                //var result2 = await c.ExecuteResponsesAsync();

                //Assert.IsTrue(result.Item1);
                //Assert.IsTrue(result.Item2 == 10);
                //Assert.IsTrue(result.Item3.Count == 0);

                //Assert.IsTrue(result2.Item1);
                //Assert.IsTrue(result2.Item2 == 10);
                //Assert.IsTrue(result2.Item3.Count == 0);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [TestMethod]
        public async Task I1001_TestIntegrationWithServiceBusAsyncOrderedMessages_ProcessIncompleteOrderSets()
        {
            try
            {


                //var _container = new CommandContainer();

                //_container.RegisterCommand<AddNumbersCommand, AddNumbersResponse>();

                //var c = (ServerTools.ServerCommands.AzureServiceBus.CloudCommands) await new AzureServiceBus.CloudCommands().InitializeAsync(_container, new AzureServiceBusConnectionOptions(Configuration["ASBConnectionString"], AzureServiceBusTier.Standard, 3, logger, DefaultMessageTimeToLive: TimeSpan.FromMinutes(2), QueueNamePrefix: _queueNamePrefix, MaxWaitTime: TimeSpan.FromSeconds(10), RequiresSession: true));


                //var session1 = Guid.NewGuid();
                //var session2 = Guid.NewGuid();


                ////create ordered sets wth missing items

                ////this one will have 3,4,5 stuck in the deferred list
                //_ = await c.PostOrderedCommandAsync<AddNumbersCommand>(new { Number1 = 4, Number2 = 0 }, session1, 4, false);
                ////_ = await c.PostOrderedCommandAsync<AddNumbersCommand>(new { Number1 = 2, Number2 = 0 }, session1, 2, false);
                //_ = await c.PostOrderedCommandAsync<AddNumbersCommand>(new { Number1 = 3, Number2 = 0 }, session1, 3, false);
                //_ = await c.PostOrderedCommandAsync<AddNumbersCommand>(new { Number1 = 5, Number2 = 0 }, session1, 5, true);
                //_ = await c.PostOrderedCommandAsync<AddNumbersCommand>(new { Number1 = 1, Number2 = 0 }, session1, 1, false);

                ////this one will have 4,5 stuck in the deferred list
                //_ = await c.PostOrderedCommandAsync<AddNumbersCommand>(new { Number1 = 4, Number2 = 0 }, session2, 4, false);
                //_ = await c.PostOrderedCommandAsync<AddNumbersCommand>(new { Number1 = 2, Number2 = 0 }, session2, 2, false);
                ////_ = await c.PostOrderedCommandAsync<AddNumbersCommand>(new { Number1 = 3, Number2 = 0 }, session2, 3, false);
                //_ = await c.PostOrderedCommandAsync<AddNumbersCommand>(new { Number1 = 5, Number2 = 0 }, session2, 5, true);
                //_ = await c.PostOrderedCommandAsync<AddNumbersCommand>(new { Number1 = 1, Number2 = 0 }, session2, 1, false);

                ////in total there will be 5 items in the defreed list that cannot be processed by simply running ExecuteCommandsAsync


                //var result = await c.ExecuteCommandsAsync();
                //var result2 = await c.ExecuteResponsesAsync();
                //var r1 = await c.HandleCommandsDlqAsync();
                //var r2 = await c.HandleResponsesDlqAsync();

            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public bool HandleDlqMessage(Message m)
        {
            return false;
            //return m.DlqDequeueCount >= 2 || m.Metadata.CommandPostedOn < DateTime.UtcNow.AddMinutes(-30) ? false : true;
        }


     
    }
}
