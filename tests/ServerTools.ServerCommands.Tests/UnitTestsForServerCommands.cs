using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ServerTools.ServerCommands.Tests
{
    [TestClass]
    public class UnitTestsForServerCommands
    {
        private static CommandContainer _container;

        /// <summary>
        /// Execute once before the test-suite
        /// </summary>
        [ClassInitialize()]
        public static void InitTestSuite(TestContext testContext)
        {
            _container = new CommandContainer();
        }


        [TestMethod]
        public void A1010_TestInitializedContainerIsNotNull()
        {
            Assert.IsNull(_container);
        }
    }
}
