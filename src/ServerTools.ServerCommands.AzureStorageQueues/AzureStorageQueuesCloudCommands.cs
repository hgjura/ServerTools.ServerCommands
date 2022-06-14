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
    //public class AzureStorageQueuesCloudCommands2 //: BaseCloudCommands
    //{
    //    //readonly Queue<dynamic> queue = new Queue<dynamic>();

    //    CommandContainer container;

    //    readonly AzureStorageQueuesConnectionOptions connectionOptions;

    //    QueueClient qsc_requests;
    //    QueueClient qsc_requests_deadletter;
    //    QueueClient qsc_responses;
    //    QueueClient qsc_responses_deadletter;

    //    public AzureStorageQueuesCloudCommands2(CommandContainer Container, ILogger Log, AzureStorageQueuesConnectionOptions ConnectionOptions)
    //    {
    //        container = Container;
    //        connectionOptions = ConnectionOptions;

    //        var q_name_reqs = $"{connectionOptions.QueueNamePrefix.NullIfEmptyOrWhitespace() ?? "cmd"}-reqs";
    //        var q_name_reqs_dlq = $"{connectionOptions.QueueNamePrefix.NullIfEmptyOrWhitespace() ?? "cmd"}-reqs-dlq";
    //        var q_name_resp = $"{connectionOptions.QueueNamePrefix.NullIfEmptyOrWhitespace() ?? "cmd"}-resp";
    //        var q_name_resp_dlq = $"{connectionOptions.QueueNamePrefix.NullIfEmptyOrWhitespace() ?? "cmd"}-resp-dlq";

    //        Validators.ValidateContainer(Container);

    //        AzureStorageQueuesValidators.ValidateNameForAzureStorageQueue(q_name_reqs);
    //        AzureStorageQueuesValidators.ValidateNameForAzureStorageQueue(q_name_reqs_dlq);
    //        AzureStorageQueuesValidators.ValidateNameForAzureStorageQueue(q_name_resp);
    //        AzureStorageQueuesValidators.ValidateNameForAzureStorageQueue(q_name_resp_dlq);

    //        qsc_requests ??= new QueueClient(new Uri($"https://{connectionOptions.AccountName}.queue.core.windows.net/{q_name_reqs}"), new StorageSharedKeyCredential(connectionOptions.AccountName, connectionOptions.AccountKey));
    //        qsc_requests_deadletter ??= new QueueClient(new Uri($"https://{connectionOptions.AccountName}.queue.core.windows.net/{q_name_reqs_dlq}"), new StorageSharedKeyCredential(connectionOptions.AccountName, connectionOptions.AccountKey));
    //        qsc_responses ??= new QueueClient(new Uri($"https://{connectionOptions.AccountName}.queue.core.windows.net/{q_name_resp}"), new StorageSharedKeyCredential(connectionOptions.AccountName, connectionOptions.AccountKey));
    //        qsc_responses_deadletter ??= new QueueClient(new Uri($"https://{connectionOptions.AccountName}.queue.core.windows.net/{q_name_resp_dlq}"), new StorageSharedKeyCredential(connectionOptions.AccountName, connectionOptions.AccountKey));

    //        _ = qsc_requests.CreateIfNotExists();
    //        _ = qsc_requests_deadletter.CreateIfNotExists();
    //        _ = qsc_responses.CreateIfNotExists();
    //        _ = qsc_responses_deadletter.CreateIfNotExists();
    //    }

    //    #region Single Commands functionality

    //    public async Task<bool> PostCommandAsync<T>(dynamic CommandContext)
    //    {
    //        return await PostCommandAsync(typeof(T).Name, CommandContext);
    //    }

    //    public async Task<bool> PostCommandAsync(Type type, dynamic CommandContext)
    //    {
    //        return await PostCommandAsync(type.Name, CommandContext);
    //    }

    //    public async Task<bool> PostCommandAsync(string type_name, dynamic CommandContext)
    //    {
    //        var commandBody = new ExpandoObject() as dynamic;
    //        commandBody.CommandContext = new ExpandoObject() as dynamic;

    //        commandBody.Metadata = new CommandMetadata() {
    //            UniqueId = Guid.NewGuid(),
    //            CommandType = type_name,
    //            CommandPostedOn = DateTime.UtcNow
    //            };            

    //        commandBody.CommandContext = CommandContext;

    //        var r = await qsc_requests.SendMessageAsync(JsonConvert.SerializeObject(commandBody));

    //        return r != null ? true : false;
    //    }

    //    public async Task<(bool, int, List<string>)> ExecuteCommandsAsync(int timeWindowinMinutes = 1)
    //    {
    //        //tupple return: Item1: (bool) - if all commands have been processed succesfully or not | Item2: (int) - number of commands that were processed/returned from the remote queue | Item3: (List<string>) - List of all exception messages when Item1 = false;
    //        var result = (true, 0, new List<string>());

    //        while (true)
    //        {
    //            QueueMessage[] messages = await qsc_requests.ReceiveMessagesAsync(32, TimeSpan.FromMinutes(timeWindowinMinutes));

    //            if (messages.Length == 0) break;

    //            result.Item2 = messages.Length;

    //            foreach (var m in messages)
    //            {
    //                var b = await _ProcessCommands(m.MessageText, connectionOptions.Log);

    //                if (b.Item1)
    //                {
    //                    _ = await qsc_requests.DeleteMessageAsync(m.MessageId, m.PopReceipt);
    //                }
    //                else
    //                {
    //                    result.Item1 = false;
    //                    result.Item3.Add($"{m.MessageId}:{b.Item2?.Message}:{b.Item2?.InnerException?.Message}");

    //                    connectionOptions.Log?.LogError(b.Item2?.Message);
    //                    connectionOptions.Log?.LogWarning($"Message could not be processed: [{qsc_requests.Name}] {m.MessageText}");

    //                    if (m.DequeueCount >= connectionOptions.MaxDequeueCountForError)
    //                    {
    //                        connectionOptions.Log?.LogWarning($"Message {m.MessageId} will be moved to dead letter queue.");

    //                        _ = await _PostCommandToDeadLetter(m, b.Item2);
    //                        _ = await qsc_requests.DeleteMessageAsync(m.MessageId, m.PopReceipt);
    //                    }
    //                }
    //            }

    //            QueueProperties properties = await qsc_requests.GetPropertiesAsync();
    //            connectionOptions.Log?.LogWarning($"[{qsc_requests.Name}] {properties.ApproximateMessagesCount} messages left in queue.");
    //        }

    //        return result;
    //    }

    //    #endregion 

    //    #region Single Response functionality

    //    public async Task<bool> PostResponseAsync<T>(dynamic ResponseContext, CommandMetadata OriginalCommandMetadata)
    //    {
    //        return await PostResponseAsync(typeof(T), ResponseContext, OriginalCommandMetadata);
    //    }

    //    public async Task<bool> PostResponseAsync(Type ResponseType, dynamic ResponseContext, CommandMetadata OriginalCommandMetadata)
    //    {
    //        var commandBody = new ExpandoObject() as dynamic;

    //        commandBody.ResponseContext = new ExpandoObject() as dynamic;

    //        commandBody.ResponseContext = ResponseContext;

    //        OriginalCommandMetadata.ResponseUniqueId = Guid.NewGuid();
    //        OriginalCommandMetadata.ResponseType = ResponseType.Name;
    //        OriginalCommandMetadata.ResponsePostedOn = DateTime.UtcNow;

    //        commandBody.Metadata = OriginalCommandMetadata;

    //        var r = await qsc_responses.SendMessageAsync(JsonConvert.SerializeObject(commandBody));

    //        return r != null ? true : false;
    //    }

    //    public async Task<(bool, int, List<string>)> ExecuteResponsesAsync(int timeWindowinMinutes = 1)
    //    {
    //        //tupple return: Item1: (bool) - if all commands have been processed succesfully or not | Item2: (int) - number of commands that were processed/returned from the remote queue | Item3: (List<string>) - List of all exception messages when Item1 = false;
    //        var result = (true, 0, new List<string>());

    //        while (true)
    //        {
    //            QueueMessage[] messages = await qsc_responses.ReceiveMessagesAsync(32, TimeSpan.FromMinutes(timeWindowinMinutes));

    //            if (messages.Length == 0) break;

    //            result.Item2 = messages.Length;

    //            foreach (var m in messages)
    //            {
    //                var b = await _ProcessResponses(m.MessageText, connectionOptions.Log);

    //                if (b.Item1)
    //                {
    //                    _ = await qsc_responses.DeleteMessageAsync(m.MessageId, m.PopReceipt);
    //                }
    //                else
    //                {
    //                    result.Item1 = false;
    //                    result.Item3.Add($"{m.MessageId}:{b.Item2?.Message}:{b.Item2?.InnerException?.Message}");

    //                    connectionOptions.Log?.LogError(b.Item2?.Message);
    //                    connectionOptions.Log?.LogWarning($"Message could not be processed: [{qsc_responses.Name}] {m.MessageText}");

    //                    if (m.DequeueCount >= connectionOptions.MaxDequeueCountForError)
    //                    {
    //                        connectionOptions.Log?.LogWarning($"Message {m.MessageId} will be moved to dead letter queue.");

    //                        _ = await _PostResponseToDeadLetter(m, b.Item2);
    //                        _ = await qsc_responses.DeleteMessageAsync(m.MessageId, m.PopReceipt);
    //                    }
    //                }
    //            }

    //            QueueProperties properties = await qsc_responses.GetPropertiesAsync();
    //            connectionOptions.Log?.LogWarning($"[{qsc_responses.Name}] {properties.ApproximateMessagesCount} messages left in queue.");
    //        }

    //        return result;
    //    }

    //    #endregion           

    //    public Task ClearAllAsync()
    //    {
    //        _ = qsc_requests_deadletter.ClearMessages();
    //        _ = qsc_requests_deadletter.Delete();

    //        _ = qsc_responses_deadletter.ClearMessages();
    //        _ = qsc_responses_deadletter.Delete();

    //        _ = qsc_requests.ClearMessages();
    //        _ = qsc_requests.Delete();

    //        _ = qsc_responses.ClearMessages();
    //        _ = qsc_responses.Delete();

    //        return Task.CompletedTask;
    //    }

    //    public async Task ClearAllAsync(bool WaitForQueuesToClear)
    //    {
    //        await ClearAllAsync();

    //        //this is needed since you cannot recreate a queue with same name for 30 seconds. Pausing for 35 seconds.
    //        if (WaitForQueuesToClear) Thread.Sleep(35000);
    //    }
    //    private async Task<(bool, Exception, dynamic, CommandMetadata)> _ProcessCommands(string commandBody, ILogger log)
    //    {
    //        try
    //        {
    //            dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(commandBody);

    //            dynamic context = (dynamic)m.CommandContext;
    //            var meta = (CommandMetadata)m.Metadata;

    //            string type = meta.CommandType?.ToString();

    //            if (!string.IsNullOrEmpty(type))
    //            {
    //                if (container.IsCommandRegistered(type))
    //                {

    //                    var cmd = container.ResolveCommand(type);

    //                    if (meta.UniqueId != null) meta.CommandExecutedOn = DateTime.UtcNow;

    //                    var r = await cmd?.ExecuteAsync(context, meta);                    

    //                    if (cmd.RequiresResponse && r.Item3 != null && r.Item4 != null)
    //                    {
    //                        var resp_meta = (CommandMetadata)r.Item4;
    //                        if (resp_meta.UniqueId != null) resp_meta.CommandCompletedOn = DateTime.UtcNow;

    //                        var resp = container.ResolveResponseFromCommand(cmd.GetType());

    //                        if (resp != null)
    //                            PostResponseAsync(resp.GetType(), r.Item3, resp_meta);
    //                    }

    //                    return r;
    //                }
    //                else
    //                {
    //                    log?.LogError($"Command type [{type}] is not registered with the container and cannot be processed. Skipping.");
    //                    throw new ApplicationException($"Command type [{type}] is not registered with the container and cannot be processed.");
    //                }
    //            }
    //            else
    //            {
    //                throw new ApplicationException("Command type was null or empty.");
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            return (false, ex, null, new CommandMetadata());
    //        }
    //    }
    //    private async Task<bool> _PostCommandToDeadLetter(QueueMessage message, Exception ex)
    //    {
    //        dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(message.MessageText);

    //        var context = (dynamic)m.CommandContext;
    //        var meta = (CommandMetadata)m.Metadata;

    //        var commandDLQBody = new ExpandoObject() as dynamic;

    //        commandDLQBody.OriginalCommandContext = new ExpandoObject() as dynamic;
    //        commandDLQBody.OriginalCommandContext = context;

    //        meta.DeadletterQueueUniqueId = Guid.NewGuid();
    //        meta.DeadletterQueueErrorMessage = ex.Message;
    //        meta.DeadletterQueueInnerErrorMesssage = ex.InnerException?.Message;
    //        meta.PostedToDeadletterQueueOn = DateTime.UtcNow;

    //        commandDLQBody.Metadata = meta;

    //        var r = await qsc_requests_deadletter.SendMessageAsync(JsonConvert.SerializeObject(commandDLQBody));

    //        return r != null;
    //    }
    //    private async Task<(bool, Exception)> _ProcessResponses(string responseBody, ILogger log)
    //    {
    //        try
    //        {
    //            dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(responseBody);

    //            dynamic context = (dynamic)m.ResponseContext;
    //            var meta = (CommandMetadata)m.Metadata;
    //            string type = meta.ResponseType?.ToString();
    //            if (!string.IsNullOrEmpty(type))
    //            {

    //                if (container.IsResponseRegistered(type))
    //                {
    //                    if (meta.UniqueId != null) meta.ResponseExecutedOn = DateTime.UtcNow;

    //                    var r = await container.ResolveResponse(type)?.ExecuteAsync(context, meta);

    //                    if (meta.UniqueId != null && r.Item1) meta.ResponseProcessedOn = DateTime.UtcNow;

    //                    return r;
    //                }
    //                else
    //                {
    //                    log?.LogError($"Response type [{type}] is not registered with the container and cannot be processed. Skipping.");
    //                    throw new ApplicationException($"Response type [{type}] is not registered with the container and cannot be processed.");
    //                }
    //            }
    //            else
    //            {
    //                throw new ApplicationException("Response type was null or empty.");
    //            }
    //        }
    //        catch (Exception ex)
    //        {

    //            return (false, ex);
    //        }
    //    }
    //    private async Task<bool> _PostResponseToDeadLetter(QueueMessage message, Exception ex)
    //    {
    //        dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(message.MessageText);
    //        var context = (dynamic)m.CommandContext;
    //        var meta = (CommandMetadata)m.Metadata;

    //        var responseDLQBody = new ExpandoObject() as dynamic;

    //        responseDLQBody.OriginalResponseContext = new ExpandoObject() as dynamic;
    //        responseDLQBody.OriginalResponseContext = context;

    //        meta.DeadletterQueueUniqueId = Guid.NewGuid();
    //        meta.DeadletterQueueErrorMessage = ex.Message;
    //        meta.DeadletterQueueInnerErrorMesssage = ex.InnerException?.Message;
    //        meta.DeadletterQueuePostedOn = DateTime.UtcNow;            
    //        responseDLQBody.Metadata = meta;

    //        var r = await qsc_responses_deadletter.SendMessageAsync(JsonConvert.SerializeObject(responseDLQBody));

    //        return r != null;
    //    }
    //}

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
        {
            var m = (QueueMessage)message;

            _ = await qsc_requests.DeleteMessageAsync(m.MessageId, m.PopReceipt);
        }

        public override async Task DeleteResponseAsync(object message)
        {
            var m = (QueueMessage)message;

            _ = await qsc_responses.DeleteMessageAsync(m.MessageId, m.PopReceipt);
        }

        public override async Task<Message[]> GetCommandsAsync(int timeWindowinMinutes)
        {
            QueueMessage[] messages = await qsc_requests.ReceiveMessagesAsync(32, TimeSpan.FromMinutes(timeWindowinMinutes));

            List<Message> list = new List<Message>();

            foreach (var item in messages)
            {
                dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(item.Body.ToString());

                var context = m.CommandContext;
                var meta = JsonConvert.DeserializeObject<CommandMetadata>(JsonConvert.SerializeObject(m.Metadata));

                list.Add(new Message(item.MessageId, JsonConvert.SerializeObject(context), item.DequeueCount, meta.CommandDeadletterQueueRetryCount, meta, item));
            }

            return list.ToArray();
        }

        public override async Task<long> GetCommandsCountAsync()
        {
            var properties = await qsc_requests.GetPropertiesAsync();
            return properties?.Value?.ApproximateMessagesCount ?? -1;
        }

        public override async Task<Message[]> GetResponsesAsync(int timeWindowinMinutes = 1)
        {
            List<Message> list = new List<Message>();

            QueueMessage[] messages = await qsc_responses.ReceiveMessagesAsync(32, TimeSpan.FromMinutes(timeWindowinMinutes));

            foreach (var item in messages)
            {
                dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(item.Body.ToString());

                dynamic context = (dynamic)m.ResponseContext;
                var meta = JsonConvert.DeserializeObject<CommandMetadata>(JsonConvert.SerializeObject(m.Metadata));

                list.Add(new Message(item.MessageId, JsonConvert.SerializeObject(context), item.DequeueCount, meta.ResponseDeadletterQueueRetryCount, meta, item));
            }

            return list.ToArray();
        }

        public override async Task<long> GetResponsessCountAsync()
        {
            var properties = await qsc_responses.GetPropertiesAsync();
            return properties?.Value?.ApproximateMessagesCount ?? -1;
        }

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
    }
}
