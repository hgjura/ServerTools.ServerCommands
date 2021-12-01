using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net.Http;

namespace ServerTools.ServerCommands.Tests
{
    [TestClass]
    public class UnitTestsForServerCommands
    {
        static CommandContainer _container;
        static string _queueNamePrefix;
        /// <summary>
        /// Execute once before the test-suite
        /// </summary>
        /// 

        IConfiguration Configuration { get; set; }

        public UnitTestsForServerCommands()
        {
            var builder = new ConfigurationBuilder()
                .AddUserSecrets<UnitTestsForServerCommands>(true)
                .AddJsonFile("local.tests.settings.json", true);

            Configuration = builder.Build();
        }


        [ClassInitialize()]
        public static void InitTestSuite(TestContext testContext)
        {
            _container = new CommandContainer();
            _queueNamePrefix = nameof(UnitTestsForServerCommands).ToLower();
            //_ = new Commands(_container, Environment.GetEnvironmentVariable("StorageAccounName"), Environment.GetEnvironmentVariable("StorageAccountKey"), null, QueueNamePrefix: _queueNamePrefix);
        }

        [ClassCleanup()]
        public static void CleanTestSuite()
        {
           //new Commands(_container, Environment.GetEnvironmentVariable("StorageAccounName"), Environment.GetEnvironmentVariable("StorageAccountKey"), null, QueueNamePrefix: _queueNamePrefix).Clear();
        }


        [TestMethod]
        public void A1000_TestValidateSecretsAreNotNull()
        {
            Assert.IsNotNull(Configuration["StorageAccountName"]);
            Assert.IsNotNull(Configuration["StorageAccountKey"]);
        }

        [TestMethod]
        public void A1010_TestInitializedContainerIsNotNull()
        {
            Assert.IsNotNull(_container);
        }

        [TestMethod]
        public void A1020_TestContainerCannotBeNull()
        {
            var ex = Assert.ThrowsException<ArgumentNullException>(() => new Commands(null, "accountname", "queuename"));

            Assert.IsTrue(ex.Message.Contains("CommandContainer"));
        }

        [TestMethod]
        public void A1030_TestAccountNameIsValid()
        {
            ////check for null, empty or whitespace

            Assert.IsTrue(Assert.ThrowsException<ArgumentNullException>(() => Validators.ValidateNameForAzureStorage(null)).Message.Contains("String argument cannot be null"));
            Assert.IsTrue(Assert.ThrowsException<ArgumentNullException>(() => Validators.ValidateNameForAzureStorage("")).Message.Contains("String argument cannot be null"));
            Assert.IsTrue(Assert.ThrowsException<ArgumentNullException>(() => Validators.ValidateNameForAzureStorage("   ")).Message.Contains("String argument cannot be null"));

            //check for special characters, capital letters, or too long
            var ex4 = Assert.ThrowsException<ArgumentOutOfRangeException>(() => Validators.ValidateNameForAzureStorage("_storage0123account"));

            Assert.IsTrue(ex4.Message.Contains("Invalid storage name"));

            var ex5 = Assert.ThrowsException<ArgumentOutOfRangeException>(() => Validators.ValidateNameForAzureStorage("AStorage0Account"));

            Assert.IsTrue(ex5.Message.Contains("Invalid storage name"));

            var ex6 = Assert.ThrowsException<ArgumentOutOfRangeException>(() => Validators.ValidateNameForAzureStorage("storageaccountthatisabittoolongwithnumbers123456789"));

            Assert.IsTrue(ex6.Message.Contains("Invalid storage name"));



            //this should not fail as it has a valid storage account name: all lower case + numbers, no special or other charcters, between 3 to 24 chars in length
            Validators.ValidateNameForAzureStorage("valid0storage1account");

        }

        [TestMethod]
        public void A1040_TestQueuetNameIsValid()
        {
            //check for null, empty or whitespace
            Assert.IsTrue(Assert.ThrowsException<ArgumentNullException>(() => Validators.ValidateNameForAzureStorageQueue(null)).Message.Contains("String argument cannot be null"));
            Assert.IsTrue(Assert.ThrowsException<ArgumentNullException>(() => Validators.ValidateNameForAzureStorageQueue("")).Message.Contains("String argument cannot be null"));
            Assert.IsTrue(Assert.ThrowsException<ArgumentNullException>(() => Validators.ValidateNameForAzureStorageQueue("   ")).Message.Contains("String argument cannot be null"));

            //check for special characters (except - ), capital letters, too long, doesnt start or end with -, or doesnt have a repeating --
            Assert.IsTrue(Assert.ThrowsException<ArgumentOutOfRangeException>(() => Validators.ValidateNameForAzureStorageQueue("-queue1name")).Message.Contains("Invalid queue name"));

            Assert.IsTrue(Assert.ThrowsException<ArgumentOutOfRangeException>(() => Validators.ValidateNameForAzureStorageQueue("queue1name-")).Message.Contains("Invalid queue name"));

            Assert.IsTrue(Assert.ThrowsException<ArgumentOutOfRangeException>(() => Validators.ValidateNameForAzureStorageQueue("queue_1name")).Message.Contains("Invalid queue name"));

            Assert.IsTrue(Assert.ThrowsException<ArgumentOutOfRangeException>(() => Validators.ValidateNameForAzureStorageQueue("queue--1name")).Message.Contains("Invalid queue name"));

            Assert.IsTrue(Assert.ThrowsException<ArgumentOutOfRangeException>(() => Validators.ValidateNameForAzureStorageQueue("Queue1Name")).Message.Contains("Invalid queue name"));

            Assert.IsTrue(Assert.ThrowsException<ArgumentOutOfRangeException>(() => Validators.ValidateNameForAzureStorageQueue("queue1namethatisabittoolongwithnumbers123456789aaaaaaaaaaaaaaaaaaa")).Message.Contains("Invalid queue name"));



            //this should not fail as it has a valid storage account name: all lower case + numbers, no special or other charcters, between 3 to 24 chars in length
            Validators.ValidateNameForAzureStorageQueue("valid-storage-queue-name1");

        }

        [TestMethod]
        public void A1040_TestAccountkeyIsValid()
        {
            //check for null, empty or whitespace

            Assert.IsTrue(Assert.ThrowsException<ArgumentNullException>(() => Validators.ValidateNameForAzureStorageAccountKey(null)).Message.Contains("String argument cannot be null"));
            Assert.IsTrue(Assert.ThrowsException<ArgumentNullException>(() => Validators.ValidateNameForAzureStorageAccountKey("")).Message.Contains("String argument cannot be null"));
            Assert.IsTrue(Assert.ThrowsException<ArgumentNullException>(() => Validators.ValidateNameForAzureStorageAccountKey("   ")).Message.Contains("String argument cannot be null"));

            //check for string not being a base 64 string
            Assert.IsTrue(Assert.ThrowsException<ArgumentOutOfRangeException>(() => Validators.ValidateNameForAzureStorageAccountKey("accountkey")).Message.Contains("Account key is not in the right format [Base 64]"));


            //this should not fail as it has a valid storage account key: a string64 encoded
            Validators.ValidateNameForAzureStorageAccountKey("/jTXyUjLpws9kUjoxWc1WT68L06FwhfvkLHdKqWfM7SxViWlcLAElo1qOCpNieyjtUkS6u8+");

            new Commands(_container, Environment.GetEnvironmentVariable("StorageAccounName"), Environment.GetEnvironmentVariable("StorageAccountKey"), null, QueueNamePrefix: _queueNamePrefix).Clear();
        }




    }
}
