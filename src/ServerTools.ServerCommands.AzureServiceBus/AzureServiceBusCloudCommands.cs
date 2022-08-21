using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading;
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
            throw new NotImplementedException();    
        }
        public override async Task<Message[]> GetCommandsFromDlqAsync()
        {
            AzureServiceBusConnectionOptions conn = (AzureServiceBusConnectionOptions)connectionOptions;
            
            IReadOnlyList<ServiceBusReceivedMessage> messages = await connectionOptions.RetryPolicy.ExecuteAsync(() => reqs_receiver_dlq.ReceiveMessagesAsync(maxMessages: conn.MaxMessagesToRetrieve, maxWaitTime: conn.MaxWaitTime ?? TimeSpan.FromMinutes(1)));

            List<Message> list = new List<Message>();

            foreach (var item in messages)
            {
                dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(item.Body.ToString());

                var context = m.CommandContext;
                var meta = JsonConvert.DeserializeObject<CommandMetadata>(JsonConvert.SerializeObject(m.Metadata));

                var commandBody = new ExpandoObject() as dynamic;
                commandBody.CommandContext = new ExpandoObject() as dynamic;
                commandBody.CommandContext = context;
                list.Add(new Message(item.MessageId, JsonConvert.SerializeObject(commandBody), item.DeliveryCount, meta.CommandDeadletterQueueRetryCount++, meta, item));
            }

            return list.ToArray();
        }

        public override async Task<long> GetCommandsCountAsync()
        {
            var properties = await connectionOptions.RetryPolicy.ExecuteAsync(() => admin_client.GetQueueRuntimePropertiesAsync(reqs_receiver.EntityPath));

            return properties?.Value?.ActiveMessageCount ?? -1;
        }

        public override async Task<long> GetCommandsDlqCountAsync()
        {
            var properties = await connectionOptions.RetryPolicy.ExecuteAsync(() => admin_client.GetQueueRuntimePropertiesAsync(reqs_receiver.EntityPath));

            return properties?.Value?.DeadLetterMessageCount ?? -1;
        }

        public override async Task DeleteCommandAsync(object message)
        {
            if (isSessionEnabled)
            {
                await connectionOptions.RetryPolicy.ExecuteAsync(() => reqs_receiver_session.CompleteMessageAsync((ServiceBusReceivedMessage)message));
            }               
            else
                await connectionOptions.RetryPolicy.ExecuteAsync(() => reqs_receiver.CompleteMessageAsync((ServiceBusReceivedMessage)message));
        }

        public override async Task DeleteCommandFromDlqAsync(object message)
           => await connectionOptions.RetryPolicy.ExecuteAsync(() => reqs_receiver_dlq.CompleteMessageAsync((ServiceBusReceivedMessage)message));

        public override async Task<bool> PostCommandAsync(Message message)
        {
            dynamic body = JsonConvert.DeserializeObject<ExpandoObject>(message.Text);

            if (isSessionEnabled)
            {
                var session = Guid.NewGuid();

                if(!message.Metadata.CustomMetadata.ContainsKey("Name"))
                    message.Metadata.CustomMetadata.Add("Name", typeof(Message).Name);

                if (!message.Metadata.CustomMetadata.ContainsKey("IsOrdered"))
                    message.Metadata.CustomMetadata.Add("IsOrdered", false);

                if (!message.Metadata.CustomMetadata.ContainsKey("SessionId"))
                    message.Metadata.CustomMetadata.Add("SessionId", session.ToString());

                if (!message.Metadata.CustomMetadata.ContainsKey("Order"))
                    message.Metadata.CustomMetadata.Add("Order", 1);

                if (!message.Metadata.CustomMetadata.ContainsKey("IsLast"))
                    message.Metadata.CustomMetadata.Add("IsLast", true);

                body.Metadata = message.Metadata;

                await connectionOptions.RetryPolicy.ExecuteAsync(() => reqs_sender.SendMessageAsync(new ServiceBusMessage(JsonConvert.SerializeObject(body)) { Subject = typeof(Message).Name, SessionId = Guid.NewGuid().ToString() }));             
            }
            else
            {
                body.Metadata = message.Metadata;
                await connectionOptions.RetryPolicy.ExecuteAsync(() => reqs_sender.SendMessageAsync(new ServiceBusMessage(JsonConvert.SerializeObject(body)) { Subject = typeof(Message).Name })); 
            }

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
            var m = new ServiceBusMessage(JsonConvert.SerializeObject(body));

            if (isSessionEnabled)
            {
                m.SessionId = message.Metadata.CustomMetadata["SessionId"].ToString();
                m.Subject = message.Metadata.CustomMetadata["Name"].ToString();
                m.ApplicationProperties["Order"] = Convert.ToInt32(message.Metadata.CustomMetadata["Order"]);
                m.ApplicationProperties["IsLast"] = (bool)message.Metadata.CustomMetadata["IsLast"];
            }
            
            await connectionOptions.RetryPolicy.ExecuteAsync(async() => await resp_sender.SendMessageAsync(m));
            return true;
        }
        public override async Task<bool> PostResponseToDlqAsync(Message message)
        {
            await resp_receiver.DeadLetterMessageAsync((ServiceBusReceivedMessage)message.OriginalMessage);
            return true;
        }

        public override async Task<(bool, int, List<string>)> ExecuteResponsesAsync()
        {
            //await _processExpiredDeferredMessagesAsync(resp_receiver, 10000, q_name_resp, TimeSpan.FromSeconds(30));

            //tupple return: Item1: (bool) - if all commands have been processed succesfully or not | Item2: (int) - number of commands that were processed/returned from the remote queue | Item3: (List<string>) - List of all exception messages when Item1 = false;
            var result = (true, 0, new List<string>());

            while (true)
            {
                AzureServiceBusConnectionOptions conn = (AzureServiceBusConnectionOptions)connectionOptions;
                IReadOnlyList<ServiceBusReceivedMessage> messages = null;

                if (isSessionEnabled)
                {
                    resp_receiver_session = await _getNextSessionAsync(q_name_resp, new CancellationTokenSource(conn.MaxWaitTime ?? TimeSpan.FromMinutes(1)).Token);

                    if (resp_receiver_session != null)
                    {
                        messages = await connectionOptions.RetryPolicy.ExecuteAsync(async () => await resp_receiver_session?.ReceiveMessagesAsync(maxMessages: conn.MaxMessagesToRetrieve, maxWaitTime: conn.MaxWaitTime ?? TimeSpan.FromSeconds(10)));
                    }
                }
                else
                {
                    if (resp_receiver != null)
                        messages = await connectionOptions.RetryPolicy.ExecuteAsync(async () => await resp_receiver?.ReceiveMessagesAsync(maxMessages: conn.MaxMessagesToRetrieve, maxWaitTime: conn.MaxWaitTime ?? TimeSpan.FromMinutes(1)));

                }

                if (messages == null || messages.Count == 0) break;

                result.Item2 += messages.Count;

                foreach (var item in messages)
                {

                    switch (item.Subject)
                    {
                        case "OrderedMessage":
                            {
                                var order = (int)item.ApplicationProperties["Order"];
                                var isLast = item.ApplicationProperties["IsLast"].ToString().ToLower() == "true";

                                var state_data = await resp_receiver_session.GetSessionStateAsync();

                                var session_state = state_data != null ? state_data.ToObjectFromJson<SessionStateManager>() : new SessionStateManager();

                                if (order == session_state.LastProcessedCount + 1)  //check if message is next in the sequence
                                {
                                    var r = await _executeResponseMessageAsync(item);

                                    if (!r.Item1)
                                    {
                                        result.Item1 = false;
                                        result.Item3.Add(r.Item2);
                                    }

                                    if (!isLast)
                                    {
                                        session_state.LastProcessedCount = order;
                                        await resp_receiver_session.SetSessionStateAsync(new BinaryData(session_state, null, typeof(SessionStateManager)));
                                    }
                                    else
                                    {
                                        await resp_receiver_session.SetSessionStateAsync(null);
                                    }

                                    await _processDeferredListAsync(session_state, resp_receiver_session, _executeResponseMessageAsync);
                                }
                                else
                                {
                                    await _addMessageToDeferredListAsync(session_state, order, item, resp_receiver_session);
                                }
                            }
                            break;
                        case "Message":
                            {
                                var r = await _executeResponseMessageAsync(item);

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
                connectionOptions.Log?.LogWarning($"[{connectionOptions.ResponseQueueName ?? "unknown"}] {count} messages left in queue.");
            }

            return result;



        }
        public override async Task<Message[]> GetResponsesAsync()
        {
            throw new NotImplementedException();
            //AzureServiceBusConnectionOptions conn = (AzureServiceBusConnectionOptions)connectionOptions;

            //IReadOnlyList<ServiceBusReceivedMessage> messages = await resp_receiver.ReceiveMessagesAsync(maxMessages: conn.MaxMessagesToRetrieve, maxWaitTime: conn.MaxWaitTime ?? TimeSpan.FromMinutes(1));

            //List<Message> list = new List<Message>();

            //foreach (var item in messages)
            //{
            //    dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(item.Body.ToString());

            //    var context = m.ResponseContext;
            //    var meta = JsonConvert.DeserializeObject<CommandMetadata>(JsonConvert.SerializeObject(m.Metadata));

            //    list.Add(new Message(item.MessageId, JsonConvert.SerializeObject(context), item.DeliveryCount, meta.CommandDeadletterQueueRetryCount, meta, item));
            //}

            //return list.ToArray();
        }
        public override async Task<Message[]> GetResponsesFromDlqAsync()
        {
            AzureServiceBusConnectionOptions conn = (AzureServiceBusConnectionOptions)connectionOptions;

            IReadOnlyList<ServiceBusReceivedMessage> messages = await resp_receiver_dlq.ReceiveMessagesAsync(maxMessages: conn.MaxMessagesToRetrieve, maxWaitTime: conn.MaxWaitTime ?? TimeSpan.FromMinutes(1));

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
        {
            var properties = await connectionOptions.RetryPolicy.ExecuteAsync(() => admin_client.GetQueueRuntimePropertiesAsync(resp_receiver.EntityPath));

            return properties?.Value?.ActiveMessageCount ?? -1;
        }
        public override async Task<long> GetResponsesDlqCountAsync()
        {
            var properties = await connectionOptions.RetryPolicy.ExecuteAsync(() => admin_client.GetQueueRuntimePropertiesAsync(resp_receiver.EntityPath));

            return properties?.Value?.DeadLetterMessageCount ?? -1;
        }

        public override async Task DeleteResponseAsync(object message)
        {
            if (isSessionEnabled)
            {
                await connectionOptions.RetryPolicy.ExecuteAsync(() => resp_receiver_session.CompleteMessageAsync((ServiceBusReceivedMessage)message));
            }
            else
                await connectionOptions.RetryPolicy.ExecuteAsync(() => resp_receiver.CompleteMessageAsync((ServiceBusReceivedMessage)message));
        }
        public override async Task DeleteResponseFromDlqAsync(object message)
            => await resp_receiver_dlq.CompleteMessageAsync((ServiceBusReceivedMessage)message);

        #endregion

        #region Ordered Commands + Responses
        public async Task<bool> PostOrderedCommandAsync<T>(dynamic CommandContext, Guid SessionId, int Order, bool IsLast = false, CommandMetadata PreviousMatadata = new()) => await PostOrderedCommandAsync(typeof(T).Name, CommandContext, SessionId, Order, IsLast, PreviousMatadata);
        public async Task<bool> PostOrderedCommandAsync(Type type, dynamic CommandContext, Guid SessionId, int Order, bool IsLast = false, CommandMetadata PreviousMatadata = new()) => await PostOrderedCommandAsync(type.Name, CommandContext, SessionId, Order, IsLast, PreviousMatadata);
        public async Task<bool> PostOrderedCommandAsync(string type_name, dynamic CommandContext, Guid SessionId, int Order, bool IsLast = false, CommandMetadata PreviousMatadata = new())
        {
            var commandBody = new ExpandoObject() as dynamic;
            commandBody.CommandContext = new ExpandoObject() as dynamic;
            commandBody.CommandContext = CommandContext;

            var meta = PreviousMatadata;

            if (PreviousMatadata.UniqueId == null)
                meta.CommandStartNew(type_name);
            else
                meta.CommandResubmitFromDLQ();

            var r = await PostOrderedCommandAsync(new Message("", JsonConvert.SerializeObject(commandBody), 0, meta.CommandDeadletterQueueRetryCount, meta, null), SessionId, Order, IsLast);

            return r;
        }
        public async Task<bool> PostOrderedCommandAsync(Message message, Guid SessionId, int Order, bool IsLast = false)
        {
            AzureServiceBusValidators.ValidateOrderedMessageFeature(isSessionEnabled);

            dynamic body = JsonConvert.DeserializeObject<ExpandoObject>(message.Text);

            message.Metadata.CustomMetadata["IsOrdered"] = true;
            message.Metadata.CustomMetadata["Name"] = "OrderedMessage";
            message.Metadata.CustomMetadata["SessionId"] = SessionId.ToString();
            message.Metadata.CustomMetadata["Order"] = Order;
            message.Metadata.CustomMetadata["IsLast"] = IsLast;

            body.Metadata = message.Metadata;

            var m = new ServiceBusMessage(JsonConvert.SerializeObject(body));
            m.SessionId = SessionId.ToString();
            m.Subject = "OrderedMessage";
            m.ApplicationProperties["Order"] = Order;
            m.ApplicationProperties["IsLast"] = IsLast;

            await connectionOptions.RetryPolicy.ExecuteAsync(() => reqs_sender.SendMessageAsync(m));

            return true;
        }


        public override async Task<(bool, int, List<string>)> ExecuteCommandsAsync()
        {
            //tupple return: Item1: (bool) - if all commands have been processed succesfully or not | Item2: (int) - number of commands that were processed/returned from the remote queue | Item3: (List<string>) - List of all exception messages when Item1 = false;
            var result = (true, 0, new List<string>());

            while (true)
            {

                AzureServiceBusConnectionOptions conn = (AzureServiceBusConnectionOptions)connectionOptions;
                IReadOnlyList<ServiceBusReceivedMessage> messages = null;

                if (isSessionEnabled)
                {
                    reqs_receiver_session = await _getNextSessionAsync(q_name_reqs, new CancellationTokenSource(conn.MaxWaitTime ?? TimeSpan.FromMinutes(1)).Token);

                    if (reqs_receiver_session != null)
                    {
                        messages = await connectionOptions.RetryPolicy.ExecuteAsync(async () => await reqs_receiver_session?.ReceiveMessagesAsync(maxMessages: conn.MaxMessagesToRetrieve, maxWaitTime: conn.MaxWaitTime ?? TimeSpan.FromSeconds(10)));
                    }
                }
                else
                {
                    if(reqs_receiver != null)
                        messages = await connectionOptions.RetryPolicy.ExecuteAsync(async () => await reqs_receiver?.ReceiveMessagesAsync(maxMessages: conn.MaxMessagesToRetrieve, maxWaitTime: conn.MaxWaitTime ?? TimeSpan.FromMinutes(1)));

                }

                if (messages == null || messages.Count == 0) break;

                result.Item2 += messages.Count;
                
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

                                    await _processDeferredListAsync(session_state, reqs_receiver_session, _executeCommandMessageAsync);
                                }
                                else
                                {
                                    await _addMessageToDeferredListAsync(session_state, order, item, reqs_receiver_session);
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

        public override async Task<(bool, int)> HandleCommandsDlqAsync(Func<Message, bool> ValidateProcessing = null)
        {
            await _processExpiredDeferredMessagesAsync(reqs_receiver, 10000, q_name_reqs, TimeSpan.FromSeconds(30));

            //tupple return: Item1: (bool) - if all commands have been processed succesfully or not | Item2: (int) - number of commands that were processed/returned from the remote queue
            var result = (true, 0);

            while (true)
            {
                var messages = await GetCommandsFromDlqAsync();

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
                        //repost to main queue 
                        await connectionOptions.RetryPolicy.ExecuteAsync(async () => await reqs_sender.SendMessageAsync(new ServiceBusMessage((ServiceBusReceivedMessage)m.OriginalMessage)));
                        await DeleteCommandFromDlqAsync(m.OriginalMessage);
                    }
                }
            }

            var count = await GetCommandsDlqCountAsync();
            connectionOptions.Log?.LogWarning($"DLQ: [{connectionOptions.CommandQueueName ?? "unknown"}] {count} messages left in queue.");

            return result;
        }
        public override async Task<(bool, int)> HandleResponsesDlqAsync(Func<Message, bool> ValidateProcessing = null)
        {
            await _processExpiredDeferredMessagesAsync(resp_receiver, 10000, q_name_resp, TimeSpan.FromSeconds(30));

            var result = (true, 0);

            while (true)
            {
                var messages = await GetResponsesFromDlqAsync();

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
                        //repost to main queue 
                        await connectionOptions.RetryPolicy.ExecuteAsync(async () => await resp_sender.SendMessageAsync(new ServiceBusMessage((ServiceBusReceivedMessage)m.OriginalMessage)));
                        await DeleteResponseFromDlqAsync(m.OriginalMessage);
                        
                    }
                }

                var count = await GetResponsesDlqCountAsync();
                connectionOptions.Log?.LogWarning($"DLQ: [{connectionOptions.ResponseQueueName ?? "unknown"}] {count} messages left in queue.");
            }

            return result;
        }

        private async Task _processDeferredListAsync(SessionStateManager session_state, ServiceBusSessionReceiver session_receiver, Func<ServiceBusReceivedMessage, Task<(bool, string)>> execute)
        {
            int x = session_state.LastProcessedCount + 1;

            long seq2 = 0;

            while (true)
            {

                if (!session_state.DeferredList.TryGetValue(x, out seq2)) break;

                //-------------------------------
                var deferredMessage = await session_receiver.ReceiveDeferredMessageAsync(seq2);

                //var r = await _executeCommandMessageAsync(deferredMessage);

                var r = await execute(deferredMessage);

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
        private async Task _processExpiredDeferredMessagesAsync(ServiceBusReceiver receiver, int peek_messages_limit, string session_queue_name, TimeSpan expire_session)
        {
            var sessionids = new Dictionary<long, string>();

            foreach (ServiceBusReceivedMessage m in await receiver.PeekMessagesAsync(peek_messages_limit))
            {
                if (m.State == ServiceBusMessageState.Deferred && m.ExpiresAt <= DateTime.UtcNow)
                {
                    sessionids.Add(m.SequenceNumber, m.SessionId);
                }
            }

            var ids = sessionids.Values.Distinct().ToList();

            foreach (var id in ids)
            {
                var session = await _getSessionAsync(session_queue_name, id, new CancellationTokenSource(expire_session).Token);

                if (session != null)
                {
                    var seqs = sessionids.Where(s => s.Value == id).Select(s => s.Key).ToList();
                    try
                    {
                        var msgs = await session.ReceiveDeferredMessagesAsync(seqs);
                    }
                    catch (ServiceBusException ex)
                    {
                        if (ex.Reason != ServiceBusFailureReason.MessageNotFound) throw;
                    }
                }
            }
        }
        private async Task _addMessageToDeferredListAsync(SessionStateManager session_state, int order, ServiceBusReceivedMessage item, ServiceBusSessionReceiver session_receiver)
        {
            session_state.DeferredList.Add(order, item.SequenceNumber);
            await session_receiver.DeferMessageAsync(item);
            await session_receiver.SetSessionStateAsync(new BinaryData(session_state, null, typeof(SessionStateManager)));
        }
        private async Task<(bool, string)> _executeCommandMessageAsync(ServiceBusReceivedMessage item)
        {
            dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(item.Body.ToString());

            var context = m.CommandContext;
            var meta = JsonConvert.DeserializeObject<CommandMetadata>(JsonConvert.SerializeObject(m.Metadata));

            var r = await ExecuteCommandMessage(new Message(item.MessageId, JsonConvert.SerializeObject(context), item.DeliveryCount, meta.CommandDeadletterQueueRetryCount, meta, item));

            return r;
        }
        private async Task<(bool, string)> _executeResponseMessageAsync(ServiceBusReceivedMessage item)
        {
            dynamic m = JsonConvert.DeserializeObject<ExpandoObject>(item.Body.ToString());

            var context = m.ResponseContext;
            var meta = JsonConvert.DeserializeObject<CommandMetadata>(JsonConvert.SerializeObject(m.Metadata));

            var r = await ExecuteResponseMessage(new Message(item.MessageId, JsonConvert.SerializeObject(context), item.DeliveryCount, meta.CommandDeadletterQueueRetryCount, meta, item));

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
                DefaultMessageTimeToLive = conn.DefaultMessageTimeToLive,
                DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(1),
                EnableBatchedOperations = conn.EnableBatchedOperations,
                DeadLetteringOnMessageExpiration = conn.DeadLetteringOnMessageExpiration,
                EnablePartitioning = false,
                ForwardDeadLetteredMessagesTo = null,
                ForwardTo = null,
                LockDuration = conn.LockDuration,
                MaxDeliveryCount = conn.MaxDeliveryCount,
                MaxSizeInMegabytes = conn.MaxSizeInMegabytes
            };

            options1.AutoDeleteOnIdle = conn.AutoDeleteOnIdle;
            options1.RequiresDuplicateDetection = conn.RequiresDuplicateDetection;
            isSessionEnabled = options1.RequiresSession = conn.RequiresSession;

            options1.AuthorizationRules.Add(new SharedAccessAuthorizationRule(
                "allClaims",
                new[] { AccessRights.Manage, AccessRights.Send, AccessRights.Listen }));

            await connectionOptions.RetryPolicy.ExecuteAsync(async () =>
            {
                if (!(await admin_client.QueueExistsAsync(q_name_reqs)).Value) await admin_client.CreateQueueAsync(options1);

                options1.Name = q_name_resp;
                if (!(await admin_client.QueueExistsAsync(q_name_resp)).Value) await admin_client.CreateQueueAsync(options1);

                reqs_sender = client.CreateSender(q_name_reqs);
                reqs_receiver = client.CreateReceiver(q_name_reqs);
                reqs_receiver_dlq = client.CreateReceiver(q_name_reqs, new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });

                resp_sender = client.CreateSender(q_name_resp);
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

        private async Task<ServiceBusSessionReceiver> _getNextSessionAsync(string queuename, CancellationToken token = default)
        {
            try
            {
                if (isSessionEnabled)
                {
                    return await client.AcceptNextSessionAsync(queuename, new ServiceBusSessionReceiverOptions() { PrefetchCount = 100, ReceiveMode = ServiceBusReceiveMode.PeekLock }, token);
                }
            }
            catch (TaskCanceledException ex)
            {
                return null;
            }
            catch (ServiceBusException ex)
            {
                if (ex.Reason == ServiceBusFailureReason.ServiceTimeout)
                    return null;
                else
                    throw;
            }

            return null;
        }

        private async Task<ServiceBusSessionReceiver> _getSessionAsync(string queuename, string sessionid, CancellationToken token = default)
        {
            try
            {
                if (isSessionEnabled)
                {
                    return await client.AcceptSessionAsync(queuename, sessionid, new ServiceBusSessionReceiverOptions() { PrefetchCount = 100, ReceiveMode = ServiceBusReceiveMode.PeekLock }, token);
                }
            }
            catch (TaskCanceledException ex)
            {
                return null;
            }
            catch (ServiceBusException ex)
            {
                if (ex.Reason == ServiceBusFailureReason.ServiceTimeout)
                    return null;
                else
                    throw;
            }

            return null;
        }

    }

   
}
