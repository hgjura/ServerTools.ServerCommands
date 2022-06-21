using FastExpressionCompiler.LightExpression;
using Microsoft.Azure.Amqp.Encoding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServerTools.ServerCommands.AzureStorageQueues;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ServerTools.ServerCommands.Tests
{
    [TestClass]
    public class AzureStorageTests
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
            commands = await new CloudCommands().InitializeAsync(_container, new AzureStorageQueuesConnectionOptions(Configuration["StorageAccountName"], Configuration["StorageAccountKey"], 3, logger, QueueNamePrefix: _queueNamePrefix));
        }

        [ClassCleanup()]
        public static void CleanTestSuite()
        {
            //new AzureStorageQueuesCloudCommands(_container, Configuration["StorageAccountName"], Configuration["StorageAccountKey"], null, QueueNamePrefix: _queueNamePrefix).ClearAll();
        }


        [TestMethod]
        public void I1000_TestIntegrationWithAzureStorageQueues()
        {
            _container
                .Use(logger)
                .RegisterCommand<AddNumbersCommand>()
                .RegisterResponse<AddNumbersCommand, AddNumbersResponse>();

            //_ = commands.PostCommandAsync<AddNumbersCommand>(new { Number1 = 2, Number2 = 3 }).GetAwaiter().GetResult();

            //var result1 = commands.ExecuteCommandsAsync().GetAwaiter().GetResult();

            //var result2 = commands.ExecuteResponsesAsync().GetAwaiter().GetResult();

            //var dlq = commands.HandleCommandsDlqAsync(HandleDlqMessage).GetAwaiter().GetResult();

        }


        public bool HandleDlqMessage(Message m)
        {
            return m.DlqDequeueCount >= 2 ? false : true;
        }
       

    }

    public class AddNumbersCommand : IRemoteCommand
    {
        private ILogger logger;
        public AddNumbersCommand(ILogger logger)
        {
            this.logger = logger;
        }

        public bool RequiresResponse => true;

        public async Task<(bool, Exception, dynamic, CommandMetadata)> ExecuteAsync(dynamic command, CommandMetadata meta)
        {
            logger ??= new DebugLoggerProvider().CreateLogger("default");

            try
            {
                throw new Exception("TEST EXCEPTION COMMAND");
                int n1 = (int)command.Number1;
                int n2 = (int)command.Number2;

                int result = n1 + n2;

                logger.LogInformation($"<< {n1} + {n2} = {n1 + n2} >>");

                return await Task.FromResult<(bool, Exception, dynamic, CommandMetadata)>((true, null, new { Result = result, Message = "Ok." }, meta));


            }
            catch (Exception ex)
            {
                //logger.LogError(ex.Message);
                throw ex;
                return await Task.FromResult<(bool, Exception, dynamic, CommandMetadata)>((false, ex, null, meta));
            }
            finally
            {

            }
        }

    }

    public class AddNumbersResponse : IRemoteResponse
    {
        private ILogger logger;
        public AddNumbersResponse(ILogger logger)
        {
            this.logger = logger;
        }
        public async Task<(bool, Exception, CommandMetadata)> ExecuteAsync(dynamic response, CommandMetadata metadata)
        {
            logger ??= new DebugLoggerProvider().CreateLogger("default");

            try
            {
                throw new Exception("TEST EXCEPTION");

                var r = (int)response.Result;
                var m = (string)response.Message;

                logger.LogInformation($"<< Result from the command is in: Result = {r} | Message = {m} >>");

                return await Task.FromResult<(bool, Exception, CommandMetadata)>((true, null, metadata));


            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);

                return await Task.FromResult<(bool, Exception, CommandMetadata)>((false, ex, metadata));
            }
            finally
            {

            }
        }
    }
}
