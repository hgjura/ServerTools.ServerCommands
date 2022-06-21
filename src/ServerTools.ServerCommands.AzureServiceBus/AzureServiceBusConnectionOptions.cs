using Microsoft.Extensions.Logging;
using Polly;

namespace ServerTools.ServerCommands.AzureServiceBus
{
    public class AzureServiceBusConnectionOptions : ConnectionOptions
    {
        public string ConnectionString { get; }
        public string QueueNamePrefix { get; }

        public AzureServiceBusConnectionOptions(string ConnectionString, int MaxDequeueCountForError = 5, ILogger Log = null, AsyncPolicy RetryPolicy = null, string QueueNamePrefix = null) : base(Log, RetryPolicy, MaxDequeueCountForError)
        {
            this.ConnectionString = ConnectionString;
            this.QueueNamePrefix = QueueNamePrefix;
        }
    }
}
