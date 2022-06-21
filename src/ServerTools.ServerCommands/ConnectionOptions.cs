using Microsoft.Extensions.Logging;
using Polly;
using System;

namespace ServerTools.ServerCommands
{
    public abstract class ConnectionOptions
    {
        public readonly ILogger Log = null;
        public readonly AsyncPolicy RetryPolicy = null;
        public readonly int MaxDequeueCountForError;
        public string CommandQueueName;
        public string ResponseQueueName;
        protected ConnectionOptions(ILogger Log = null, AsyncPolicy RetryPolicy = null, int maxDequeueCountForError = 0)
        {
            this.Log ??= Log;

            this.RetryPolicy = RetryPolicy ?? Policy
               .Handle<Exception>()
               .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (result, timeSpan, retryCount, context) =>
               {
                   Log?.LogWarning($"Calling service failed [{result.Message} | {result.InnerException?.Message}]. Waiting {timeSpan} before next retry. Retry attempt {retryCount}.");
               });

            MaxDequeueCountForError = maxDequeueCountForError;
        }
    }
}
