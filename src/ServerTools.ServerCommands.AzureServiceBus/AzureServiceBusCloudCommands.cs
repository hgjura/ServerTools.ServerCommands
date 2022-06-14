using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace ServerTools.ServerCommands.AzureServiceBus
{
    public class CloudCommands2 : ICloudCommands
    {
        string q_name_reqs;
        string q_name_resp;
        bool isInitialized;
        ServiceBusAdministrationClient admin_client;
        ServiceBusClient client;
        ServiceBusSender reqs_sender;
        ServiceBusReceiver reqs_receiver;
        ServiceBusSender resp_sender;
        ServiceBusReceiver resp_receiver;
        CommandContainer container;
        readonly AzureServiceBusConnectionOptions connectionOptions;
        public CloudCommands(CommandContainer Container, AzureServiceBusConnectionOptions ConnectionOptions)
        {
            this.container = Container;
            this.connectionOptions = ConnectionOptions;

            Validators.ValidateContainer(Container);

            q_name_reqs = $"{connectionOptions.QueueNamePrefix.NullIfEmptyOrWhitespace() ?? "commands"}-reqs";
            q_name_resp = $"{connectionOptions.QueueNamePrefix.NullIfEmptyOrWhitespace() ?? "commands"}-resp";

            AzureServiceBusValidators.ValidateNameForAzureServiceBusQueue(q_name_reqs);
            AzureServiceBusValidators.ValidateNameForAzureServiceBusQueue(q_name_resp);

            admin_client = new ServiceBusAdministrationClient(connectionOptions.ConnectionString);
            client = new ServiceBusClient(connectionOptions.ConnectionString);
            reqs_sender = client.CreateSender(q_name_reqs);
            reqs_receiver = client.CreateReceiver(q_name_reqs);
            resp_sender = client.CreateSender(q_name_resp);
            resp_receiver = client.CreateReceiver(q_name_resp);
        }

        public async Task ClearAllAsync()
        {
            if (!(await admin_client.QueueExistsAsync(q_name_reqs)).Value) await admin_client.DeleteQueueAsync(q_name_reqs);
            if (!(await admin_client.QueueExistsAsync(q_name_resp)).Value) await admin_client.DeleteQueueAsync(q_name_resp);
        }


        #region Single Commands functionality
        public async Task<bool> PostCommandAsync<T>(dynamic CommandContext)
        {
            return await PostCommandAsync(typeof(T).Name, CommandContext);
        }

        public async Task<bool> PostCommandAsync(Type type, dynamic CommandContext)
        {
            return await PostCommandAsync(type.Name, CommandContext);
        }

        public async Task<bool> PostCommandAsync(string type_name, dynamic CommandContext)
        {
            if (!isInitialized) await _InitializeService();

            var commandBody = new ExpandoObject() as dynamic;
            commandBody.CommandContext = new ExpandoObject() as dynamic;

            commandBody.Metadata = new CommandMetadata()
            {
                UniqueId = Guid.NewGuid(),
                CommandType = type_name,
                CommandPostedOn = DateTime.UtcNow
            };

            commandBody.CommandContext = CommandContext;

            var r = await reqs_sender.SendMessageAsync(JsonConvert.SerializeObject(commandBody));

            return r != null ? true : false;
        }

        public async Task<(bool, int, List<string>)> ExecuteCommandsAsync(int timeWindowinMinues = 1)
        {
            if (!isInitialized) await _InitializeService();

            //tupple return: Item1: (bool) - if all commands have been processed succesfully or not | Item2: (int) - number of commands that were processed/returned from the remote queue | Item3: (List<string>) - List of all exception messages when Item1 = false;

            var result = (true, 0, new List<string>());

            while (true)
            {
                IReadOnlyList<ServiceBusReceivedMessage> messages = await reqs_receiver.ReceiveMessagesAsync(maxMessages: 32, maxWaitTime: TimeSpan.FromMinutes(timeWindowinMinues));

                if (messages.Count == 0) break;

                result.Item2 = messages.Count;

                foreach (var m in messages)
                {

                    var b = await _ProcessCommands(m.Body.ToString(), connectionOptions.Log);

                    if (b.Item1)
                    {
                        await reqs_receiver.CompleteMessageAsync(m);
                    }
                    else
                    {
                        result.Item1 = false;
                        result.Item3.Add($"{m.MessageId}:{b.Item2?.Message}:{b.Item2?.InnerException?.Message}");

                        connectionOptions.Log?.LogError(b.Item2?.Message);
                        connectionOptions.Log?.LogWarning($"Message could not be processed: [{q_name_reqs}] {m.Body}");

                        if (m.DeliveryCount >= connectionOptions.MaxDequeueCountForError)
                        {
                            connectionOptions.Log?.LogWarning($"Message {m.MessageId} will be moved to dead letter queue.");

                            _ = await _PostCommandToDeadLetter(m, b.Item2);

                            await reqs_receiver.CompleteMessageAsync(m);
                        }
                    }
                }

                var properties = await admin_client.GetQueueRuntimePropertiesAsync(q_name_reqs);

                connectionOptions.Log?.LogWarning($"[{q_name_reqs}] {properties.Value.ActiveMessageCount} messages left in queue.");
            }

            return result;
        }
        #endregion

        #region Single Response functionality
        public async Task<bool> PostResponseAsync<T>(dynamic ResponseContext, CommandMetadata OriginalCommandMetadata)
        {
            return await PostResponseAsync(typeof(T), ResponseContext, OriginalCommandMetadata);
        }

        public async Task<bool> PostResponseAsync(Type ResponseType, dynamic ResponseContext, CommandMetadata OriginalCommandMetadata)
        {
            if (!isInitialized) await _InitializeService();

            var commandBody = new ExpandoObject() as dynamic;
            commandBody.Metadata = OriginalCommandMetadata;
            commandBody.ResponseContext = new ExpandoObject() as dynamic;

            commandBody.ResponseContext = ResponseContext;

            commandBody.Metadata.ResponseUniqueId = Guid.NewGuid();
            commandBody.Metadata.ResponseType = ResponseType.Name;
            commandBody.Metadata.ResponsePostedOn = DateTime.UtcNow;

            var r = await resp_sender.SendMessageAsync(JsonConvert.SerializeObject(commandBody));

            return r != null ? true : false;
        }

        public async Task<(bool, int, List<string>)> ExecuteResponsesAsync(int timeWindowinMinues = 1)
        {
            if (!isInitialized) await _InitializeService();

            //tupple return: Item1: (bool) - if all commands have been processed succesfully or not | Item2: (int) - number of commands that were processed/returned from the remote queue | Item3: (List<string>) - List of all exception messages when Item1 = false;
            var result = (true, 0, new List<string>());

            while (true)
            {
                IReadOnlyList<ServiceBusReceivedMessage> messages = await resp_receiver.ReceiveMessagesAsync(maxMessages: 32, maxWaitTime: TimeSpan.FromMinutes(timeWindowinMinues));

                if (messages.Count == 0) break;

                result.Item2 = messages.Count;

                foreach (var m in messages)
                {

                    var b = await _ProcessResponses(m.Body.ToString(), connectionOptions.Log);

                    if (b.Item1)
                    {
                        await resp_receiver.CompleteMessageAsync(m);
                    }
                    else
                    {
                        result.Item1 = false;
                        result.Item3.Add($"{m.MessageId}:{b.Item2?.Message}:{b.Item2?.InnerException?.Message}");

                        connectionOptions.Log?.LogError(b.Item2?.Message);
                        connectionOptions.Log?.LogWarning($"Message could not be processed: [{q_name_resp}] {m.Body}");

                        if (m.DeliveryCount >= connectionOptions.MaxDequeueCountForError)
                        {
                            connectionOptions.Log?.LogWarning($"Message {m.MessageId} will be moved to dead letter queue.");

                            _ = await _PostResponseToDeadLetter(m, b.Item2);

                            await resp_receiver.CompleteMessageAsync(m);
                        }
                    }
                }

                var properties = await admin_client.GetQueueRuntimePropertiesAsync(q_name_resp);

                connectionOptions.Log?.LogWarning($"[{q_name_resp}] {properties.Value.ActiveMessageCount} messages left in queue.");
            }

            return result;
        }
        #endregion
        private async Task _InitializeService()
        {
            var options1 = new CreateQueueOptions(q_name_reqs)
            {
                AutoDeleteOnIdle = TimeSpan.FromDays(7),
                DefaultMessageTimeToLive = TimeSpan.FromDays(1),
                DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(1),
                EnableBatchedOperations = true,
                DeadLetteringOnMessageExpiration = true,
                EnablePartitioning = false,
                ForwardDeadLetteredMessagesTo = null,
                ForwardTo = null,
                LockDuration = TimeSpan.FromSeconds(45),
                MaxDeliveryCount = 8,
                MaxSizeInMegabytes = 2048,
                RequiresDuplicateDetection = true,
                RequiresSession = true
            };

            options1.AuthorizationRules.Add(new SharedAccessAuthorizationRule(
                "allClaims",
                new[] { AccessRights.Manage, AccessRights.Send, AccessRights.Listen }));

            if (!(await admin_client.QueueExistsAsync(q_name_reqs)).Value) await admin_client.CreateQueueAsync(options1);

            var options2 = new CreateQueueOptions(q_name_resp)
            {
                AutoDeleteOnIdle = TimeSpan.FromDays(7),
                DefaultMessageTimeToLive = TimeSpan.FromDays(1),
                DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(1),
                EnableBatchedOperations = true,
                DeadLetteringOnMessageExpiration = true,
                EnablePartitioning = false,
                ForwardDeadLetteredMessagesTo = null,
                ForwardTo = null,
                LockDuration = TimeSpan.FromSeconds(45),
                MaxDeliveryCount = 8,
                MaxSizeInMegabytes = 2048,
                RequiresDuplicateDetection = true,
                RequiresSession = true
            };

            options2.AuthorizationRules.Add(new SharedAccessAuthorizationRule(
                "allClaims",
                new[] { AccessRights.Manage, AccessRights.Send, AccessRights.Listen }));


            if (!(await admin_client.QueueExistsAsync(q_name_resp)).Value) await admin_client.CreateQueueAsync(options2);

            client.CreateSender(q_name_reqs);
            client.CreateSender(q_name_resp);

            isInitialized = true;
        }
        private async Task<(bool, Exception, dynamic, dynamic)> _ProcessCommands(string commandBody, ILogger log)
        {
            try
            {
                dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(commandBody);

                dynamic context = (dynamic)m.CommandContext;
                var meta = (CommandMetadata)m.Metadata;

                string type = meta.CommandType?.ToString();

                if (!string.IsNullOrEmpty(type))
                {
                    if (container.IsCommandRegistered(type))
                    {

                        var cmd = container.ResolveCommand(type);

                        if (meta.UniqueId != null) meta.CommandExecutedOn = DateTime.UtcNow;

                        var r = await cmd?.ExecuteAsync(context, meta);

                        if (cmd.RequiresResponse && r.Item3 != null && r.Item4 != null)
                        {
                            var resp_meta = (CommandMetadata)r.Item4;
                            if (resp_meta.UniqueId != null) resp_meta.CommandCompletedOn = DateTime.UtcNow;

                            var resp = container.ResolveResponseFromCommand(cmd.GetType());

                            if (resp != null)
                                PostResponseAsync(resp.GetType(), r.Item3, resp_meta);
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
                return (false, ex, null, null);
            }
        }
        private async Task<bool> _PostCommandToDeadLetter(ServiceBusReceivedMessage message, Exception ex)
        {
            dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(message.Body.ToString());

            var context = (dynamic)m.CommandContext;
            var meta = (CommandMetadata)m.Metadata;

            var commandDLQBody = new ExpandoObject() as dynamic;

            commandDLQBody.OriginalCommandContext = new ExpandoObject() as dynamic;
            commandDLQBody.OriginalCommandContext = context;

            meta.DeadletterQueueUniqueId = Guid.NewGuid();
            meta.DeadletterQueueErrorMessage = ex.Message;
            meta.DeadletterQueueInnerErrorMesssage = ex.InnerException?.Message;
            meta.PostedToDeadletterQueueOn = DateTime.UtcNow;

            commandDLQBody.Metadata = meta;

            var r = await reqs_receiver.DeadLetterMessageAsync(JsonConvert.SerializeObject(commandDLQBody));

            return r != null;
        }
        private async Task<(bool, Exception)> _ProcessResponses(string responseBody, ILogger log)
        {
            try
            {
                dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(responseBody);

                dynamic context = (dynamic)m.ResponseContext;
                var meta = (CommandMetadata)m.Metadata;
                string type = meta.ResponseType?.ToString();
                if (!string.IsNullOrEmpty(type))
                {

                    if (container.IsResponseRegistered(type))
                    {
                        if (meta.UniqueId != null) meta.ResponseExecutedOn = DateTime.UtcNow;

                        var r = await container.ResolveResponse(type)?.ExecuteAsync(context, meta);

                        if (meta.UniqueId != null && r.Item1) meta.ResponseProcessedOn = DateTime.UtcNow;

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
                return (false, ex);
            }
        }
        private async Task<bool> _PostResponseToDeadLetter(ServiceBusReceivedMessage message, Exception ex)
        {
            dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(message.Body.ToString());
            var context = (dynamic)m.CommandContext;
            var meta = (CommandMetadata)m.Metadata;

            var responseDLQBody = new ExpandoObject() as dynamic;

            responseDLQBody.OriginalResponseContext = new ExpandoObject() as dynamic;
            responseDLQBody.OriginalResponseContext = context;

            meta.DeadletterQueueUniqueId = Guid.NewGuid();
            meta.DeadletterQueueErrorMessage = ex.Message;
            meta.DeadletterQueueInnerErrorMesssage = ex.InnerException?.Message;
            meta.DeadletterQueuePostedOn = DateTime.UtcNow;
            responseDLQBody.Metadata = meta;

            var r = await resp_receiver.DeadLetterMessageAsync(JsonConvert.SerializeObject(responseDLQBody));

            return r != null;
        }
    }

    public class CloudCommands : BaseCloudCommands
    {
        string q_name_reqs;
        string q_name_resp;
        bool isInitialized;
        ServiceBusAdministrationClient admin_client;
        ServiceBusClient client;
        ServiceBusSender reqs_sender;
        ServiceBusReceiver reqs_receiver;
        ServiceBusSender resp_sender;
        ServiceBusReceiver resp_receiver;

        private CloudCommands() { }

        public override async Task<BaseCloudCommands> InitializeAsync(CommandContainer Container, ConnectionOptions ConnectionOptions)
        {
            connectionOptions = ConnectionOptions;
            container = Container;

            var conn = connectionOptions as AzureServiceBusConnectionOptions;

            Validators.ValidateContainer(Container);

            q_name_reqs = $"{conn.QueueNamePrefix.NullIfEmptyOrWhitespace() ?? "commands"}-reqs";
            q_name_resp = $"{conn.QueueNamePrefix.NullIfEmptyOrWhitespace() ?? "commands"}-resp";

            AzureServiceBusValidators.ValidateNameForAzureServiceBusQueue(q_name_reqs);
            AzureServiceBusValidators.ValidateNameForAzureServiceBusQueue(q_name_resp);

            admin_client = new ServiceBusAdministrationClient(conn.ConnectionString);
            client = new ServiceBusClient(conn.ConnectionString);
            reqs_sender = client.CreateSender(q_name_reqs);
            reqs_receiver = client.CreateReceiver(q_name_reqs);
            resp_sender = client.CreateSender(q_name_resp);
            resp_receiver = client.CreateReceiver(q_name_resp);

            await _InitializeService();

            return await Task.FromResult(this);
        }

        public override async Task ClearAllAsync()
        {
            if (!(await admin_client.QueueExistsAsync(q_name_reqs)).Value) await admin_client.DeleteQueueAsync(q_name_reqs);
            if (!(await admin_client.QueueExistsAsync(q_name_resp)).Value) await admin_client.DeleteQueueAsync(q_name_resp);
        }

        public override async Task DeleteCommandAsync(object message)
        {
            await reqs_receiver.CompleteMessageAsync((ServiceBusReceivedMessage)message);
        }

        public override async Task DeleteResponseAsync(object message)
        {
            await resp_receiver.CompleteMessageAsync((ServiceBusReceivedMessage)message);
        }

        public override async Task<Message[]> GetCommandsAsync(int timeWindowinMinues)
        {
            IReadOnlyList<ServiceBusReceivedMessage> messages = await reqs_receiver.ReceiveMessagesAsync(maxMessages: 32, maxWaitTime: TimeSpan.FromMinutes(timeWindowinMinues));

            return messages?.Select(x => new Message(x.MessageId, x.Body.ToString(), x.DeliveryCount, x)).ToArray();
        }

        public override async Task<long> GetCommandsCountAsync()
        {
            var properties = await admin_client.GetQueueRuntimePropertiesAsync(q_name_reqs);

            return properties?.Value?.ActiveMessageCount ?? -1;
        }

        public override async Task<Message[]> GetResponsesAsync(int timeWindowinMinues)
        {
            IReadOnlyList<ServiceBusReceivedMessage> messages = await resp_receiver.ReceiveMessagesAsync(maxMessages: 32, maxWaitTime: TimeSpan.FromMinutes(timeWindowinMinues));

            return messages?.Select(x => new Message(x.MessageId, x.Body.ToString(), x.DeliveryCount, x)).ToArray();
        }

        public override async Task<long> GetResponsessCountAsync()
        {
            var properties = await admin_client.GetQueueRuntimePropertiesAsync(q_name_resp);

            return properties?.Value?.ActiveMessageCount ?? -1;
        }

        public override async Task<bool> PostCommandAsync(string messageBody)
        {
            await reqs_sender.SendMessageAsync(new ServiceBusMessage(messageBody));

            return true;
        }

        public override async Task<bool> PostCommandToDlqAsync(string messageBody)
        {
            var r = await reqs_receiver.DeadLetterMessageAsync(messageBody);
            return r != null;
        }

        public override async Task<bool> PostResponseAsync(string messageBody)
        {
            await reqs_sender.SendMessageAsync(new ServiceBusMessage(messageBody));
            return true;
        }

        public override async Task<bool> PostResponseToDlqAsync(string messageBody)
        {
            var r = await resp_receiver.DeadLetterMessageAsync(messageBody);
            return r != null;
        }

        private async Task _InitializeService()
        {
            var options1 = new CreateQueueOptions(q_name_reqs)
            {
                AutoDeleteOnIdle = TimeSpan.FromDays(7),
                DefaultMessageTimeToLive = TimeSpan.FromDays(1),
                DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(1),
                EnableBatchedOperations = true,
                DeadLetteringOnMessageExpiration = true,
                EnablePartitioning = false,
                ForwardDeadLetteredMessagesTo = null,
                ForwardTo = null,
                LockDuration = TimeSpan.FromSeconds(45),
                MaxDeliveryCount = 8,
                MaxSizeInMegabytes = 2048,
                RequiresDuplicateDetection = true,
                RequiresSession = true
            };

            options1.AuthorizationRules.Add(new SharedAccessAuthorizationRule(
                "allClaims",
                new[] { AccessRights.Manage, AccessRights.Send, AccessRights.Listen }));

            if (!(await admin_client.QueueExistsAsync(q_name_reqs)).Value) await admin_client.CreateQueueAsync(options1);

            var options2 = new CreateQueueOptions(q_name_resp)
            {
                AutoDeleteOnIdle = TimeSpan.FromDays(7),
                DefaultMessageTimeToLive = TimeSpan.FromDays(1),
                DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(1),
                EnableBatchedOperations = true,
                DeadLetteringOnMessageExpiration = true,
                EnablePartitioning = false,
                ForwardDeadLetteredMessagesTo = null,
                ForwardTo = null,
                LockDuration = TimeSpan.FromSeconds(45),
                MaxDeliveryCount = 8,
                MaxSizeInMegabytes = 2048,
                RequiresDuplicateDetection = true,
                RequiresSession = true
            };

            options2.AuthorizationRules.Add(new SharedAccessAuthorizationRule(
                "allClaims",
                new[] { AccessRights.Manage, AccessRights.Send, AccessRights.Listen }));


            if (!(await admin_client.QueueExistsAsync(q_name_resp)).Value) await admin_client.CreateQueueAsync(options2);

            client.CreateSender(q_name_reqs);
            client.CreateSender(q_name_resp);

            isInitialized = true;
        }
    }
}
