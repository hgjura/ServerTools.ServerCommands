using Azure.Storage;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServerTools.ServerCommands.AzureStorageQueues
{

    public class CloudCommands : BaseCloudCommands
    {
        QueueClient qsc_requests;
        QueueClient qsc_requests_deadletter;
        QueueClient qsc_responses;
        QueueClient qsc_responses_deadletter;

        public override async Task ClearAllAsync()
        {
            _ = await qsc_requests_deadletter.ClearMessagesAsync();
            _ = await qsc_requests_deadletter.DeleteAsync();

            _ = await qsc_responses_deadletter.ClearMessagesAsync();
            _ = await qsc_responses_deadletter.DeleteAsync();

            _ = await qsc_requests.ClearMessagesAsync();
            _ = await qsc_requests.DeleteAsync();

            _ = await qsc_responses.ClearMessagesAsync();
            _ = await qsc_responses.DeleteAsync();
        }
        public async Task ClearAllAsync(bool WaitForQueuesToClear)
        {
            await ClearAllAsync();

            //this is needed since you cannot recreate a queue with same name for 30 seconds. Pausing for 35 seconds.
            if (WaitForQueuesToClear) Thread.Sleep(35000);
        }
        public override async Task DeleteCommandAsync(object message)
            => await _deleteMessageFromQueueAsync(qsc_requests, message);
        public override async Task DeleteCommandFromDlqAsync(object message)
            => await _deleteMessageFromQueueAsync(qsc_requests_deadletter, message);

        public override async Task DeleteResponseAsync(object message)
            => await _deleteMessageFromQueueAsync(qsc_responses, message);
        public override async Task DeleteResponseFromDlqAsync(object message)
            => await _deleteMessageFromQueueAsync(qsc_responses_deadletter, message);


        public override async Task<Message[]> GetCommandsAsync(int timeWindowinMinutes)
        {
            QueueMessage[] messages = await qsc_requests.ReceiveMessagesAsync(32, TimeSpan.FromMinutes(timeWindowinMinutes));

            List<Message> list = new List<Message>();

            foreach (var item in messages)
            {
                dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(item.Body.ToString());

                var meta = JsonConvert.DeserializeObject<CommandMetadata>(JsonConvert.SerializeObject(m.Metadata));

                list.Add(new Message(item.MessageId, JsonConvert.SerializeObject(m.CommandContext), item.DequeueCount, meta.CommandDeadletterQueueRetryCount, meta, item));
            }

            return list.ToArray();
        }

        public override async Task<Message[]> GetCommandsFromDlqAsync(int timeWindowinMinutes)
        {
            QueueMessage[] messages = await qsc_requests_deadletter.ReceiveMessagesAsync(32, TimeSpan.FromMinutes(timeWindowinMinutes));

            List<Message> list = new List<Message>();

            foreach (var item in messages)
            {
                var commandBody = new ExpandoObject() as dynamic;
                commandBody.CommandContext = new ExpandoObject() as dynamic;

                dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(item.Body.ToString());

                commandBody.CommandContext = m.OriginalCommandContext;
                var meta = JsonConvert.DeserializeObject<CommandMetadata>(JsonConvert.SerializeObject(m.Metadata));
                

                list.Add(new Message(item.MessageId, JsonConvert.SerializeObject(commandBody), item.DequeueCount, meta.CommandDeadletterQueueRetryCount++, meta, item));
            }

            return list.ToArray();
        }

        public override async Task<long> GetCommandsCountAsync() => await _getQueueCountAsync(qsc_requests);
        public override async Task<long> GetCommandsDlqCountAsync() => await _getQueueCountAsync(qsc_requests_deadletter);

        public override async Task<Message[]> GetResponsesAsync(int timeWindowinMinutes = 1)
        {
            List<Message> list = new List<Message>();

            QueueMessage[] messages = await qsc_responses.ReceiveMessagesAsync(32, TimeSpan.FromMinutes(timeWindowinMinutes));

            foreach (var item in messages)
            {
                dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(item.Body.ToString());

                var meta = JsonConvert.DeserializeObject<CommandMetadata>(JsonConvert.SerializeObject(m.Metadata));

                list.Add(new Message(item.MessageId, JsonConvert.SerializeObject(m.ResponseContext), item.DequeueCount, meta.ResponseDeadletterQueueRetryCount, meta, item));
            }

            return list.ToArray();
        }
        public override async Task<Message[]> GetResponsesFromDlqAsync(int timeWindowinMinutes = 1)
        {
            List<Message> list = new List<Message>();

            QueueMessage[] messages = await qsc_responses.ReceiveMessagesAsync(32, TimeSpan.FromMinutes(timeWindowinMinutes));

            foreach (var item in messages)
            {
                var body = new ExpandoObject() as dynamic;
                body.ResponseContext = new ExpandoObject() as dynamic;

                dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(item.Body.ToString());

                body.ResponseContext = (dynamic)m.OriginalResponseContext;
                var meta = JsonConvert.DeserializeObject<CommandMetadata>(JsonConvert.SerializeObject(m.Metadata));

                list.Add(new Message(item.MessageId, JsonConvert.SerializeObject(body), item.DequeueCount, meta.ResponseDeadletterQueueRetryCount++, meta, item));
            }

            return list.ToArray();
        }

        public override async Task<long> GetResponsesCountAsync() => await _getQueueCountAsync(qsc_responses);
        public override async Task<long> GetResponsesDlqCountAsync() => await _getQueueCountAsync(qsc_responses_deadletter);

        public override async Task<bool> PostCommandAsync(Message message) => await _PostMessageAsync(qsc_requests, message);
        public override async Task<bool> PostCommandToDlqAsync(Message message)
        {
            if (await _PostMessageAsync(qsc_requests_deadletter, message))
            {
                await DeleteCommandAsync(message.OriginalMessage);

                return true;
            }
            return false;
        }

        public override async Task<bool> PostResponseAsync(Message message) => await _PostMessageAsync(qsc_responses, message);
        public override async Task<bool> PostResponseToDlqAsync(Message message)
        {
            if (await _PostMessageAsync(qsc_responses_deadletter, message))
            {
                await DeleteResponseAsync(message.OriginalMessage);

                return true;
            }
            return false;

        }

        public override async Task<BaseCloudCommands> InitializeAsync(CommandContainer Container, ConnectionOptions ConnectionOptions)
        {

            connectionOptions = ConnectionOptions;
            container = Container;

            var conn = connectionOptions as AzureStorageQueuesConnectionOptions; 
            

            var q_name_reqs = conn.CommandQueueName = $"{conn.QueueNamePrefix.NullIfEmptyOrWhitespace() ?? "cmd"}-reqs";
            var q_name_reqs_dlq = $"{conn.QueueNamePrefix.NullIfEmptyOrWhitespace() ?? "cmd"}-reqs-dlq";
            var q_name_resp = conn.ResponseQueueName = $"{conn.QueueNamePrefix.NullIfEmptyOrWhitespace() ?? "cmd"}-resp";
            var q_name_resp_dlq = $"{conn.QueueNamePrefix.NullIfEmptyOrWhitespace() ?? "cmd"}-resp-dlq";

            Validators.ValidateContainer(Container);

            AzureStorageQueuesValidators.ValidateNameForAzureStorageQueue(q_name_reqs);
            AzureStorageQueuesValidators.ValidateNameForAzureStorageQueue(q_name_reqs_dlq);
            AzureStorageQueuesValidators.ValidateNameForAzureStorageQueue(q_name_resp);
            AzureStorageQueuesValidators.ValidateNameForAzureStorageQueue(q_name_resp_dlq);

            qsc_requests ??= new QueueClient(new Uri($"https://{conn.AccountName}.queue.core.windows.net/{q_name_reqs}"), new StorageSharedKeyCredential(conn.AccountName, conn.AccountKey));
            qsc_requests_deadletter ??= new QueueClient(new Uri($"https://{conn.AccountName}.queue.core.windows.net/{q_name_reqs_dlq}"), new StorageSharedKeyCredential(conn.AccountName, conn.AccountKey));
            qsc_responses ??= new QueueClient(new Uri($"https://{conn.AccountName}.queue.core.windows.net/{q_name_resp}"), new StorageSharedKeyCredential(conn.AccountName, conn.AccountKey));
            qsc_responses_deadletter ??= new QueueClient(new Uri($"https://{conn.AccountName}.queue.core.windows.net/{q_name_resp_dlq}"), new StorageSharedKeyCredential(conn.AccountName, conn.AccountKey));

            _ = qsc_requests.CreateIfNotExists();
            _ = qsc_requests_deadletter.CreateIfNotExists();
            _ = qsc_responses.CreateIfNotExists();
            _ = qsc_responses_deadletter.CreateIfNotExists();

            return await Task.FromResult(this);
        }






        private async Task<bool> _PostMessageAsync(QueueClient client, Message message)
        {
            dynamic body = JsonConvert.DeserializeObject<ExpandoObject>(message.Text);
            body.Metadata = message.Metadata;

            var r = await client.SendMessageAsync(JsonConvert.SerializeObject(body));

            return r != null ? true : false;
        }

        private async Task<long> _getQueueCountAsync(QueueClient queue)
        {
            var properties = await queue?.GetPropertiesAsync();
            return properties?.Value?.ApproximateMessagesCount ?? -1;
        }

        private async Task _deleteMessageFromQueueAsync(QueueClient client, object message)
        {
            var m = (QueueMessage)message;
            _ = await client.DeleteMessageAsync(m.MessageId, m.PopReceipt);
        }
    }
}
