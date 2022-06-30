using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServerTools.ServerCommands.AzureServiceBus;
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
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<UnitTestsForServerCommands>(true)
                .AddJsonFile("local.tests.settings.json", true);

            Configuration = builder.Build();

            logger = new DebugLoggerProvider().CreateLogger("default");

            _container = new CommandContainer();
            _queueNamePrefix = nameof(UnitTestsForServerCommands).ToLower();
            commands = await new AzureServiceBus.CloudCommands().InitializeAsync(_container, new AzureServiceBusConnectionOptions(Configuration["ASBConnectionString"], 3, logger, QueueNamePrefix: _queueNamePrefix));
        }

        [ClassCleanup()]
        public static async Task CleanTestSuiteAsync()
        {
            await commands.ClearAllAsync();
        }


        [TestMethod]
        public async Task I1000_TestIntegrationWithServiceBusAsync()
        {
            _container
                .Use(logger)
                
                //.RegisterCommand<AddNumbersCommand>();
                //.RegisterResponse<AddNumbersResponse>();
                
                .RegisterCommand<AddNumbersCommand, AddNumbersResponse>();

                //.RegisterResponse<AddNumbersCommand, AddNumbersResponse>();

                //the three ways above to registering a command + response above are equivalent

            _ = commands.PostCommandAsync<AddNumbersCommand>(new { Number1 = 2, Number2 = 3 });

            var result1 = await commands.ExecuteCommandsAsync();
            var result2 = await commands.ExecuteResponsesAsync();

        }
    }
}
