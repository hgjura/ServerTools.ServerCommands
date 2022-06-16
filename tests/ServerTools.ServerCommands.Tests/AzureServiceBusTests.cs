using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServerTools.ServerCommands.AzureServiceBus;
using ServerTools.ServerCommands.AzureStorageQueues;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ServerTools.ServerCommands.Tests
{
    [TestClass]
    public class AzureServiceBusTests
    {
        static CommandContainer _container;
        static string _queueNamePrefix;
        static BaseCloudCommands commands;
        static ILogger logger;
        static IConfiguration Configuration { get; set; }


        /// <summary>
        /// Execute once before the test-suite
        /// </summary>
        /// 

        [ClassInitialize()]
        public static async Task InitTestSuiteAsync(TestContext testContext)
        {
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<UnitTestsForServerCommands>(true)
                .AddJsonFile("local.tests.settings.json", true);

            Configuration = builder.Build();

            logger = new DebugLoggerProvider().CreateLogger("default");

            _container = new CommandContainer();
            _queueNamePrefix = nameof(UnitTestsForServerCommands).ToLower();
            //commands = await new AzureServiceBus.CloudCommands().InitializeAsync(_container, new AzureServiceBusConnectionOptions(Configuration["ASBConnectionString"], 3, logger, QueueNamePrefix: _queueNamePrefix));
        }

        [ClassCleanup()]
        public static void CleanTestSuite()
        {
            //commands.ClearAllAsync().GetAwaiter().GetResult();
        }


        [TestMethod]
        public void I1000_TestIntegrationWithServiceBus()
        {
            _container
                .Use(logger)
                .RegisterCommand<AddNumbersCommand>()
                .RegisterResponse<AddNumbersCommand, AddNumbersResponse>();

            //_ = commands.PostCommandAsync<AddNumbersCommand>(new { Number1 = 2, Number2 = 3 }).GetAwaiter().GetResult();

            //var result1 = commands.ExecuteCommandsAsync().GetAwaiter().GetResult();

            //var result2 = commands.ExecuteResponsesAsync().GetAwaiter().GetResult();

        }



    }


}
