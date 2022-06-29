using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;

namespace ServerTools.ServerCommands
{
    public abstract class BaseCloudCommands : ICloudCommands
    {
        protected CommandContainer container;
        protected ConnectionOptions connectionOptions;

        #region Single Commands functionality

        public async Task<bool> PostCommandAsync<T>(dynamic CommandContext, CommandMetadata PreviousMatadata = new())
        {
            return await PostCommandAsync(typeof(T).Name, CommandContext);
        }
        public async Task<bool> PostCommandAsync(Type type, dynamic CommandContext, CommandMetadata PreviousMatadata = new())
        {
            return await PostCommandAsync(type.Name, CommandContext);
        }
        public async Task<bool> PostCommandAsync(string type_name, dynamic CommandContext, CommandMetadata PreviousMatadata = new ())
        {
            var commandBody = new ExpandoObject() as dynamic;
            commandBody.CommandContext = new ExpandoObject() as dynamic;         
            commandBody.CommandContext = CommandContext;

            var meta = new CommandMetadata();

            if (PreviousMatadata.UniqueId == null)
                meta.CommandStartNew(type_name);
            else
                meta.CommandResubmitFromDLQ();            

            var r = await PostCommandAsync(new Message("", JsonConvert.SerializeObject(commandBody), 0, meta.CommandDeadletterQueueRetryCount, meta, null));

            return r;
        }

        public async Task<(bool, int, List<string>)> ExecuteCommandsAsync(int timeWindowinMinutes = 1)
        {
            //tupple return: Item1: (bool) - if all commands have been processed succesfully or not | Item2: (int) - number of commands that were processed/returned from the remote queue | Item3: (List<string>) - List of all exception messages when Item1 = false;
            var result = (true, 0, new List<string>());

            while (true)
            {
                var messages = await GetCommandsAsync(timeWindowinMinutes);

                if (messages.Length == 0) break;

                result.Item2 = messages.Length;

                foreach (var m in messages)
                {
                    var meta = m.Metadata;

                    meta.CommandExeuted();

                    var b = await _ProcessCommands(m.Text, meta, connectionOptions.Log);

                    if (b.Item1)
                    {                        
                        await DeleteCommandAsync(m.OriginalMessage);
                    }
                    else
                    {
                        meta.CommandFailed();

                        result.Item1 = false;
                        result.Item3.Add($"{m.Id}:{b.Item2?.Message}:{b.Item2?.InnerException?.Message}");

                        connectionOptions.Log?.LogError(b.Item2?.Message);
                        connectionOptions.Log?.LogWarning($"Message could not be processed: [{connectionOptions.CommandQueueName ?? "unknown" }] {m.Text}");

                        if (m.DequeueCount > connectionOptions.MaxDequeueCountForError)
                        {
                            connectionOptions.Log?.LogWarning($"Message {m.Id} will be moved to dead letter queue.");

                            meta.CommandSentToDlq(b.Item2);

                            var commandDLQBody = new ExpandoObject() as dynamic;
                            commandDLQBody.OriginalCommandContext = new ExpandoObject() as dynamic;
                            commandDLQBody.OriginalCommandContext = JsonConvert.DeserializeObject<ExpandoObject>(m.Text);

                            var r = await PostCommandToDlqAsync(new Message(meta.CommandDeadletterQueueUniqueId.ToString(), JsonConvert.SerializeObject(commandDLQBody), 0, meta.CommandDeadletterQueueRetryCount, meta, m.OriginalMessage));
                        }
                    }
                }

                var count = await GetCommandsCountAsync();
                connectionOptions.Log?.LogWarning($"[{connectionOptions.CommandQueueName ?? "unknown"}] {count} messages left in queue.");
            }

            return result;
        }
        private async Task<(bool, Exception, dynamic, CommandMetadata)> _ProcessCommands(string commandBody, CommandMetadata metadata, ILogger log)
        {
            try
            {
                dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(commandBody);

                string type = metadata.CommandType?.ToString();

                if (!string.IsNullOrEmpty(type))
                {
                    if (container.IsCommandRegistered(type))
                    {
                        var cmd = container.ResolveCommand(type);
                        var r = await connectionOptions.RetryPolicy.ExecuteAsync(async () => await cmd?.ExecuteAsync(m, metadata));

                        metadata = r.Item4;
                        metadata.CommandCompleted();

                        if (cmd.RequiresResponse && r.Item3 != null)
                        {
                            var resp = container.ResolveResponseFromCommand(cmd.GetType());                            

                            if (resp != null)
                                PostResponseAsync(resp.GetType(), r.Item3, metadata);
                        }

                        return r;
                    }
                    else
                    {
                        log?.LogError($"Command type [{type}] is not registered with the container and cannot be processed. Skipping.");
                        throw new ApplicationException($"Command type [{type}] is not registered with the container and cannot be processed.");
                    }
                }
                else
                {
                    throw new ApplicationException("Command type was null or empty.");
                }
            }
            catch (Exception ex)
            {
                return (false, ex, null, new CommandMetadata());
            }
        }

        public async Task<(bool, int)> HandleCommandsDlqAsync(Func<Message, bool> ValidateProcessing = null, int timeWindowinMinutes = 1) 
        {
            //tupple return: Item1: (bool) - if all commands have been processed succesfully or not | Item2: (int) - number of commands that were processed/returned from the remote queue
            var result = (true, 0);

            while (true)
            {
                var messages = await GetCommandsFromDlqAsync(timeWindowinMinutes);

                if (messages.Length == 0) break;

                result.Item2 = messages.Length;

                foreach (var m in messages)
                {
                    if (ValidateProcessing != null && !ValidateProcessing(m))
                    {
                        await DeleteCommandFromDlqAsync(m.OriginalMessage);
                        connectionOptions.Log?.LogWarning($"DLQ: [{connectionOptions.CommandQueueName ?? "unknown"}] : message {m.Id} will not be processed and will be removed permanently.");
                    }
                    else
                    {
                        if (await PostCommandAsync(m))
                        {
                            await DeleteCommandFromDlqAsync(m.OriginalMessage);
                        }
                    }
                }

                var count = await GetCommandsDlqCountAsync();
                connectionOptions.Log?.LogWarning($"DLQ: [{connectionOptions.CommandQueueName ?? "unknown"}] {count} messages left in queue.");
            }

            return result;
        }
        public abstract Task<bool> PostCommandAsync(Message message);
        public abstract Task<bool> PostCommandToDlqAsync(Message message);
        public abstract Task<Message[]> GetCommandsAsync(int timeWindowinMinutes);
        public abstract Task<Message[]> GetCommandsFromDlqAsync(int timeWindowinMinutes);
        public abstract Task DeleteCommandAsync(object message);
        public abstract Task DeleteCommandFromDlqAsync(object message);
        public abstract Task<long> GetCommandsCountAsync();
        public abstract Task<long> GetCommandsDlqCountAsync();

        #endregion

        #region Single Response functionality
        public async Task<bool> PostResponseAsync<T>(dynamic ResponseContext, CommandMetadata OriginalCommandMetadata)
        {
            return await PostResponseAsync(typeof(T), ResponseContext, OriginalCommandMetadata);
        }
        public async Task<bool> PostResponseAsync(Type ResponseType, dynamic ResponseContext, CommandMetadata OriginalCommandMetadata)
        {
            var commandBody = new ExpandoObject() as dynamic;
            commandBody.ResponseContext = new ExpandoObject() as dynamic;
            commandBody.ResponseContext = ResponseContext;

            if (OriginalCommandMetadata.ResponseUniqueId == null)
                OriginalCommandMetadata.ResponseStartNew(ResponseType.Name);
            else
                OriginalCommandMetadata.ResponseResubmitFromDLQ();

            var r = await PostResponseAsync(new Message("", JsonConvert.SerializeObject(commandBody), 0, OriginalCommandMetadata.ResponseDeadletterQueueRetryCount, OriginalCommandMetadata, null));

            return r;
        }
        public async Task<(bool, int, List<string>)> ExecuteResponsesAsync(int timeWindowinMinutes = 1)
        {
            //tupple return: Item1: (bool) - if all commands have been processed succesfully or not | Item2: (int) - number of commands that were processed/returned from the remote queue | Item3: (List<string>) - List of all exception messages when Item1 = false;
            var result = (true, 0, new List<string>());

            while (true)
            {
                var messages = await GetResponsesAsync(timeWindowinMinutes);

                if (messages.Length == 0) break;

                result.Item2 = messages.Length;

                foreach (var m in messages)
                {
                    var meta = m.Metadata;

                    meta.ResponseExeuted();

                    var b = await _ProcessResponses(m.Text, meta, connectionOptions.Log);

                    if (b.Item1)
                    {
                        await DeleteResponseAsync(m.OriginalMessage);
                    }
                    else
                    {
                        meta.ResponseFailed();

                        result.Item1 = false;
                        result.Item3.Add($"{m.Id}:{b.Item2?.Message}:{b.Item2?.InnerException?.Message}");

                        connectionOptions.Log?.LogError(b.Item2?.Message);
                        connectionOptions.Log?.LogWarning($"Message could not be processed: [{connectionOptions.ResponseQueueName ?? "unknown"}] {m.Text}");

                        if (m.DequeueCount > connectionOptions.MaxDequeueCountForError)
                        {
                            connectionOptions.Log?.LogWarning($"Message {m.Id} will be moved to dead letter queue.");

                            meta.ResponseSentToDlq(b.Item2);

                            var responseDLQBody = new ExpandoObject() as dynamic;
                            responseDLQBody.OriginalResponseContext = new ExpandoObject() as dynamic;
                            responseDLQBody.OriginalResponseContext = JsonConvert.DeserializeObject<ExpandoObject>(m.Text);

                            _ = await PostResponseToDlqAsync(new Message(meta.ResponseDeadletterQueueUniqueId.ToString(), JsonConvert.SerializeObject(responseDLQBody), 0, meta.ResponseDeadletterQueueRetryCount, meta, m.OriginalMessage));

                        }
                    }
                }

                var count = await GetResponsesCountAsync();

                connectionOptions.Log?.LogWarning($"[{connectionOptions.ResponseQueueName ?? "unknown"}] {count} messages left in queue.");
            }

            return result;
        }

        private async Task<(bool, Exception, CommandMetadata)> _ProcessResponses(string responseBody, CommandMetadata metadata, ILogger log)
        {
            try
            {
                dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(responseBody);
                string type = metadata.ResponseType?.ToString();

                if (!string.IsNullOrEmpty(type))
                {

                    if (container.IsResponseRegistered(type))
                    {
                        var r = await connectionOptions.RetryPolicy.ExecuteAsync(async () => await container.ResolveResponse(type)?.ExecuteAsync(m, metadata));

                        metadata = r.Item3;
                        metadata.ResponseCompleted();

                        return r;
                    }
                    else
                    {
                        log?.LogError($"Response type [{type}] is not registered with the container and cannot be processed. Skipping.");
                        throw new ApplicationException($"Response type [{type}] is not registered with the container and cannot be processed.");
                    }
                }
                else
                {
                    throw new ApplicationException("Response type was null or empty.");
                }
            }
            catch (Exception ex)
            {
                return (false, ex, metadata);
            }
        }

        public async Task<(bool, int)> HandleResponsesDlqAsync(Func<Message, bool> ValidateProcessing = null, int timeWindowinMinutes = 1)
        {
            var result = (true, 0);

            while (true)
            {
                var messages = await GetResponsesFromDlqAsync(timeWindowinMinutes);

                if (messages.Length == 0) break;

                result.Item2 = messages.Length;

                foreach (var m in messages)
                {
                    if (ValidateProcessing != null && !ValidateProcessing(m))
                    {
                        await DeleteResponseFromDlqAsync(m.OriginalMessage);
                        connectionOptions.Log?.LogWarning($"DLQ: [{connectionOptions.ResponseQueueName ?? "unknown"}] : message {m.Id} will not be processed and will be removed permanenlty.");
                    }
                    else
                    {
                        if (await PostResponseAsync(m))
                        {
                            await DeleteResponseFromDlqAsync(m.OriginalMessage);
                        }
                    }
                }

                var count = await GetResponsesDlqCountAsync();
                connectionOptions.Log?.LogWarning($"DLQ: [{connectionOptions.ResponseQueueName ?? "unknown"}] {count} messages left in queue.");
            }

            return result;
        }

        public abstract Task<bool> PostResponseAsync(Message message);
        public abstract Task<bool> PostResponseToDlqAsync(Message message);
        public abstract Task<Message[]> GetResponsesAsync(int timeWindowinMinutes);
        public abstract Task<Message[]> GetResponsesFromDlqAsync(int timeWindowinMinutes);
        public abstract Task DeleteResponseAsync(object message);
        public abstract Task DeleteResponseFromDlqAsync(object message);
        public abstract Task<long> GetResponsesCountAsync();
        public abstract Task<long> GetResponsesDlqCountAsync();
        #endregion


        public abstract Task ClearAllAsync();
        public abstract Task<ICloudCommands> InitializeAsync(CommandContainer Container, ConnectionOptions ConnectionOptions);
    }
}
