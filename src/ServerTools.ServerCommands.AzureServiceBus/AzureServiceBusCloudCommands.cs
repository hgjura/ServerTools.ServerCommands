using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ServerTools.ServerCommands.AzureServiceBus
{
    public class CloudCommands : BaseCloudCommands
    {
        string q_name_reqs;
        string q_name_resp;
        bool isInitialized;
        bool isSessionEnabled;
        ServiceBusAdministrationClient admin_client;
        ServiceBusClient client;
        ServiceBusSender reqs_sender;
        ServiceBusReceiver reqs_receiver;
        ServiceBusSessionReceiver reqs_receiver_session;
        ServiceBusReceiver reqs_receiver_dlq;
        ServiceBusSender resp_sender;
        ServiceBusReceiver resp_receiver;
        ServiceBusSessionReceiver resp_receiver_session;
        ServiceBusReceiver resp_receiver_dlq;

        #region Single Command functionality
        public override async Task<Message[]> GetCommandsAsync()
        {
            AzureServiceBusConnectionOptions conn = (AzureServiceBusConnectionOptions)connectionOptions;
            IReadOnlyList<ServiceBusReceivedMessage> messages = await connectionOptions.RetryPolicy.ExecuteAsync(() => reqs_receiver.ReceiveMessagesAsync(maxMessages: conn.MaxMessagesToRetrieve, maxWaitTime: conn.MaxWaitTime ?? TimeSpan.FromMinutes(1)));

            List<Message> list = new List<Message>();

            foreach (var item in messages)
            {
                dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(item.Body.ToString());

                var context = m.CommandContext;
                var meta = JsonConvert.DeserializeObject<CommandMetadata>(JsonConvert.SerializeObject(m.Metadata));

                list.Add(new Message(item.MessageId, JsonConvert.SerializeObject(context), item.DeliveryCount, meta.CommandDeadletterQueueRetryCount, meta, item));
            }

            return list.ToArray();
        }
        public override async Task<Message[]> GetCommandsFromDlqAsync()
        {
            ServiceBusReceiver dlq_reqs_receiver = client.CreateReceiver(reqs_receiver.EntityPath, new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });

            AzureServiceBusConnectionOptions conn = (AzureServiceBusConnectionOptions)connectionOptions;
            
            IReadOnlyList<ServiceBusReceivedMessage> messages = await connectionOptions.RetryPolicy.ExecuteAsync(() => dlq_reqs_receiver.ReceiveMessagesAsync(maxMessages: conn.MaxMessagesToRetrieve, maxWaitTime: conn.MaxWaitTime ?? TimeSpan.FromMinutes(1)));

            List<Message> list = new List<Message>();

            foreach (var item in messages)
            {
                dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(item.Body.ToString());

                var context = m.CommandContext;
                var meta = JsonConvert.DeserializeObject<CommandMetadata>(JsonConvert.SerializeObject(m.Metadata));

                list.Add(new Message(item.MessageId, JsonConvert.SerializeObject(context), item.DeliveryCount, meta.CommandDeadletterQueueRetryCount, meta, item));
            }

            return list.ToArray();
        }

        public override async Task<long> GetCommandsCountAsync() 
            => await _getQueueCountAsync(reqs_receiver.EntityPath);
        public override async Task<long> GetCommandsDlqCountAsync()
            => await _getQueueCountAsync(reqs_receiver_dlq.EntityPath);

        public override async Task DeleteCommandAsync(object message)
            => await connectionOptions.RetryPolicy.ExecuteAsync(() => reqs_receiver.CompleteMessageAsync((ServiceBusReceivedMessage)message));
        public override async Task DeleteCommandFromDlqAsync(object message)
           => await connectionOptions.RetryPolicy.ExecuteAsync(() => reqs_receiver_dlq.CompleteMessageAsync((ServiceBusReceivedMessage)message));

        public override async Task<bool> PostCommandAsync(Message message)
        {
            dynamic body = JsonConvert.DeserializeObject<ExpandoObject>(message.Text);
            body.Metadata = message.Metadata;

            await connectionOptions.RetryPolicy.ExecuteAsync(() => reqs_sender.SendMessageAsync(new ServiceBusMessage(JsonConvert.SerializeObject(body)) { Subject = typeof(Message).Name }));

            return true;
        }
        public override async Task<bool> PostCommandToDlqAsync(Message message)
        {
            await connectionOptions.RetryPolicy.ExecuteAsync(() => reqs_receiver.DeadLetterMessageAsync((ServiceBusReceivedMessage)message.OriginalMessage));
            return true;
        }

        #endregion


        #region Single Response functionality


        public override async Task<bool> PostResponseAsync(Message message)
        {
            dynamic body = JsonConvert.DeserializeObject<ExpandoObject>(message.Text);
            body.Metadata = message.Metadata;

            await resp_sender.SendMessageAsync(new ServiceBusMessage(JsonConvert.SerializeObject(body)));

            return true;
        }
        public override async Task<bool> PostResponseToDlqAsync(Message message)
        {
            await resp_receiver.DeadLetterMessageAsync((ServiceBusReceivedMessage)message.OriginalMessage);
            return true;
        }

        public override async Task<Message[]> GetResponsesAsync()
        {
            AzureServiceBusConnectionOptions conn = (AzureServiceBusConnectionOptions)connectionOptions;

            IReadOnlyList<ServiceBusReceivedMessage> messages = await resp_receiver.ReceiveMessagesAsync(maxMessages: conn.MaxMessagesToRetrieve, maxWaitTime: conn.MaxWaitTime ?? TimeSpan.FromMinutes(1));

            List<Message> list = new List<Message>();

            foreach (var item in messages)
            {
                dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(item.Body.ToString());

                var context = m.ResponseContext;
                var meta = JsonConvert.DeserializeObject<CommandMetadata>(JsonConvert.SerializeObject(m.Metadata));

                list.Add(new Message(item.MessageId, JsonConvert.SerializeObject(context), item.DeliveryCount, meta.CommandDeadletterQueueRetryCount, meta, item));
            }

            return list.ToArray();
        }
        public override async Task<Message[]> GetResponsesFromDlqAsync()
        {

            ServiceBusReceiver dlq_resp_receiver = client.CreateReceiver(resp_receiver.EntityPath, new ServiceBusReceiverOptions
            {
                SubQueue = SubQueue.DeadLetter
            });

            AzureServiceBusConnectionOptions conn = (AzureServiceBusConnectionOptions)connectionOptions;

            IReadOnlyList<ServiceBusReceivedMessage> messages = await resp_receiver.ReceiveMessagesAsync(maxMessages: conn.MaxMessagesToRetrieve, maxWaitTime: conn.MaxWaitTime ?? TimeSpan.FromMinutes(1));

            List<Message> list = new List<Message>();

            foreach (var item in messages)
            {
                dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(item.Body.ToString());

                var context = m.ResponseContext;
                var meta = JsonConvert.DeserializeObject<CommandMetadata>(JsonConvert.SerializeObject(m.Metadata));

                list.Add(new Message(item.MessageId, JsonConvert.SerializeObject(context), item.DeliveryCount, meta.CommandDeadletterQueueRetryCount, meta, item));
            }

            return list.ToArray();
        }

        public override async Task<long> GetResponsesCountAsync() 
            => await _getQueueCountAsync(resp_receiver.EntityPath);
        public override async Task<long> GetResponsesDlqCountAsync()
            => await _getQueueCountAsync(resp_receiver_dlq.EntityPath);

        public override async Task DeleteResponseAsync(object message)
            => await resp_receiver.CompleteMessageAsync((ServiceBusReceivedMessage)message);
        public override async Task DeleteResponseFromDlqAsync(object message)
            => await resp_receiver_dlq.CompleteMessageAsync((ServiceBusReceivedMessage)message);

        #endregion



        #region Ordered Commands + Responses
        public async Task<bool> PostOrderedCommandAsync<T>(dynamic CommandContext, Guid SessionId, int Order, bool IsLast, CommandMetadata PreviousMatadata = new()) => await PostOrderedCommandAsync(typeof(T).Name, CommandContext, SessionId, Order, IsLast, PreviousMatadata);
        public async Task<bool> PostOrderedCommandAsync(Type type, dynamic CommandContext, Guid SessionId, int Order, bool IsLast, CommandMetadata PreviousMatadata = new()) => await PostOrderedCommandAsync(type.Name, CommandContext, SessionId, Order, IsLast, PreviousMatadata);
        public async Task<bool> PostOrderedCommandAsync(string type_name, dynamic CommandContext, Guid SessionId, int Order, bool IsLast, CommandMetadata PreviousMatadata = new())
        {
            var commandBody = new ExpandoObject() as dynamic;
            commandBody.CommandContext = new ExpandoObject() as dynamic;
            commandBody.CommandContext = CommandContext;

            var meta = PreviousMatadata;

            if (PreviousMatadata.UniqueId == null)
                meta.CommandStartNew(type_name);
            else
                meta.CommandResubmitFromDLQ();

            var r = await PostOrderedCommandAsync(SessionId, Order, IsLast, new Message("", JsonConvert.SerializeObject(commandBody), 0, meta.CommandDeadletterQueueRetryCount, meta, null));

            return r;
        }
        public async Task<bool> PostOrderedCommandAsync(Guid SessionId, int Order, bool IsLast, Message message)
        {

            dynamic body = JsonConvert.DeserializeObject<ExpandoObject>(message.Text);
            body.Metadata = message.Metadata;

            await connectionOptions.RetryPolicy.ExecuteAsync(() => reqs_sender.SendMessageAsync(new ServiceBusMessage(JsonConvert.SerializeObject(body)) { Subject = "OrderedMessage" }));

            return true;
        }


        public override async Task<(bool, int, List<string>)> ExecuteCommandsAsync()
        {
            //tupple return: Item1: (bool) - if all commands have been processed succesfully or not | Item2: (int) - number of commands that were processed/returned from the remote queue | Item3: (List<string>) - List of all exception messages when Item1 = false;
            var result = (true, 0, new List<string>());

            while (true)
            {

                AzureServiceBusConnectionOptions conn = (AzureServiceBusConnectionOptions)connectionOptions;
                IReadOnlyList<ServiceBusReceivedMessage> messages;

                messages  = isSessionEnabled 
                    ? await connectionOptions.RetryPolicy.ExecuteAsync(() => reqs_receiver_session.ReceiveMessagesAsync(maxMessages: conn.MaxMessagesToRetrieve, maxWaitTime: conn.MaxWaitTime ?? TimeSpan.FromMinutes(1))) 
                    : await connectionOptions.RetryPolicy.ExecuteAsync(() => reqs_receiver.ReceiveMessagesAsync(maxMessages: conn.MaxMessagesToRetrieve, maxWaitTime: conn.MaxWaitTime ?? TimeSpan.FromMinutes(1)));

                if (messages.Count == 0) break;

                result.Item2 = messages.Count;
                
                foreach (var item in messages)
                {

                    switch (item.Subject)
                    {
                        case "OrderedMessage":
                            {
                                var order = (int)item.ApplicationProperties["Order"];
                                var isLast = item.ApplicationProperties["IsLast"].ToString().ToLower() == "true";

                                var state_data = await reqs_receiver_session.GetSessionStateAsync();

                                var session_state = state_data != null ? state_data.ToObjectFromJson<SessionStateManager>() : new SessionStateManager();

                                if (order == session_state.LastProcessedCount + 1)  //check if message is next in the sequence
                                {
                                    var r = await _executeCommandMessageAsync(item);

                                    if (!r.Item1)
                                    {
                                        result.Item1 = false;
                                        result.Item3.Add(r.Item2);
                                    }

                                    if (!isLast)
                                    {
                                        session_state.LastProcessedCount = order;
                                        await reqs_receiver_session.SetSessionStateAsync(new BinaryData(session_state, null, typeof(SessionStateManager)));
                                    }
                                    else
                                    {
                                        await reqs_receiver_session.SetSessionStateAsync(null);
                                    }

                                    await _processDeferredListAsync(session_state, reqs_receiver_session);
                                }
                                else
                                {
                                    await _addMessageToDeferredListAsync(session_state, order, item);
                                }
                            }
                            break;
                        case "Message":
                            {
                                var r = await _executeCommandMessageAsync(item);

                                if (!r.Item1)
                                {
                                    result.Item1 = false;
                                    result.Item3.Add(r.Item2);
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }

                var count = await GetCommandsCountAsync();
                connectionOptions.Log?.LogWarning($"[{connectionOptions.CommandQueueName ?? "unknown"}] {count} messages left in queue.");
            }

            return result;
        }


        private async Task _processDeferredListAsync(SessionStateManager session_state, ServiceBusSessionReceiver session_receiver)
        {
            int x = session_state.LastProcessedCount + 1;

            long seq2 = 0;

            while (true)
            {

                if (!session_state.DeferredList.TryGetValue(x, out seq2)) break;

                //-------------------------------
                var deferredMessage = await session_receiver.ReceiveDeferredMessageAsync(seq2);

                var r = await _executeCommandMessageAsync(deferredMessage);

                if (deferredMessage.ApplicationProperties["IsLast"].ToString().ToLower() == "true")
                {
                    await session_receiver.SetSessionStateAsync(null);
                }
                else
                {
                    session_state.LastProcessedCount = ((int)deferredMessage.ApplicationProperties["Order"]);
                    session_state.DeferredList.Remove(x);
                    await session_receiver.SetSessionStateAsync(new BinaryData(session_state, null, typeof(SessionStateManager)));
                }

                x++;

            }
        }
        private async Task _addMessageToDeferredListAsync(SessionStateManager session_state, int order, ServiceBusReceivedMessage item)
        {
            session_state.DeferredList.Add(order, item.SequenceNumber);
            await reqs_receiver_session.DeferMessageAsync(item);
            await reqs_receiver_session.SetSessionStateAsync(new BinaryData(session_state, null, typeof(SessionStateManager)));
        }
        private async Task<(bool, string)> _executeCommandMessageAsync(ServiceBusReceivedMessage item)
        {
            dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(item.Body.ToString());

            var context = m.CommandContext;
            var meta = JsonConvert.DeserializeObject<CommandMetadata>(JsonConvert.SerializeObject(m.Metadata));

            var r = await ExecuteCommandMessage(new Message(item.MessageId, JsonConvert.SerializeObject(context), item.DeliveryCount, meta.CommandDeadletterQueueRetryCount, meta, item));

            return r;
        }

        public class SessionStateManager
        {
            public SessionStateManager()
            {
                LastProcessedCount = 0;
                DeferredList = new Dictionary<int, long>();
            }
            public int LastProcessedCount { get; set; }
            public Dictionary<int, long> DeferredList { get; set; }
        }


        #endregion





        public override async Task<ICloudCommands> InitializeAsync(CommandContainer Container, ConnectionOptions ConnectionOptions)
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

            var options1 = new CreateQueueOptions(q_name_reqs)
            {
                DefaultMessageTimeToLive = TimeSpan.FromDays(1),
                DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(1),
                EnableBatchedOperations = true,
                DeadLetteringOnMessageExpiration = true,
                EnablePartitioning = false,
                ForwardDeadLetteredMessagesTo = null,
                ForwardTo = null,
                LockDuration = TimeSpan.FromSeconds(45),
                MaxDeliveryCount = 8,
                MaxSizeInMegabytes = 2048
            };

            isSessionEnabled = conn.Tier != AzureServiceBusTier.Basic;

            if (conn.Tier != AzureServiceBusTier.Basic)
            {
                options1.AutoDeleteOnIdle = TimeSpan.FromDays(7);
                options1.RequiresDuplicateDetection = true;
                options1.RequiresSession = true;
            }

            options1.AuthorizationRules.Add(new SharedAccessAuthorizationRule(
                "allClaims",
                new[] { AccessRights.Manage, AccessRights.Send, AccessRights.Listen }));

            if (!(await admin_client.QueueExistsAsync(q_name_reqs)).Value) await admin_client.CreateQueueAsync(options1);

            await connectionOptions.RetryPolicy.ExecuteAsync(async () =>
            {
                if (!(await admin_client.QueueExistsAsync(q_name_reqs)).Value) await admin_client.CreateQueueAsync(options1);

                reqs_sender = client.CreateSender(q_name_reqs);
                if (isSessionEnabled)
                    reqs_receiver_session = await client.AcceptNextSessionAsync(q_name_reqs);
                else
                    reqs_receiver = client.CreateReceiver(q_name_reqs);
                reqs_receiver_dlq = client.CreateReceiver(q_name_reqs, new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });
            });

            var options2 = new CreateQueueOptions(q_name_resp)
            {
                DefaultMessageTimeToLive = TimeSpan.FromDays(1),
                DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(1),
                EnableBatchedOperations = true,
                DeadLetteringOnMessageExpiration = true,
                EnablePartitioning = false,
                ForwardDeadLetteredMessagesTo = null,
                ForwardTo = null,
                LockDuration = TimeSpan.FromSeconds(45),
                MaxDeliveryCount = 8,
                MaxSizeInMegabytes = 2048
            };

            if (conn.Tier != AzureServiceBusTier.Basic)
            {
                options2.AutoDeleteOnIdle = TimeSpan.FromDays(7);
                options2.RequiresDuplicateDetection = true;
                options2.RequiresSession = true;
            }

            options2.AuthorizationRules.Add(new SharedAccessAuthorizationRule(
                "allClaims",
                new[] { AccessRights.Manage, AccessRights.Send, AccessRights.Listen }));

            await connectionOptions.RetryPolicy.ExecuteAsync(async () =>
            {
                if (!(await admin_client.QueueExistsAsync(q_name_resp)).Value) await admin_client.CreateQueueAsync(options2);

                resp_sender = client.CreateSender(q_name_resp);

                if (isSessionEnabled)                
                    resp_receiver_session = await client.AcceptNextSessionAsync(q_name_resp);
                else
                    resp_receiver = client.CreateReceiver(q_name_resp);

                resp_receiver_dlq = client.CreateReceiver(q_name_resp, new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });
            });








            isInitialized = true;

            return await Task.FromResult(this);
        }

        public override async Task ClearAllAsync()
        {
            await connectionOptions.RetryPolicy.ExecuteAsync(async () =>
            {
                if ((await admin_client.QueueExistsAsync(q_name_reqs)).Value) await admin_client.DeleteQueueAsync(q_name_reqs);
                if ((await admin_client.QueueExistsAsync(q_name_resp)).Value) await admin_client.DeleteQueueAsync(q_name_resp);
            });
        }

        private async Task<long> _getQueueCountAsync(string name)
        {

            var properties = await connectionOptions.RetryPolicy.ExecuteAsync(() => admin_client.GetQueueRuntimePropertiesAsync(name));

            return properties?.Value?.ActiveMessageCount ?? -1;
        }
    }

   
}
