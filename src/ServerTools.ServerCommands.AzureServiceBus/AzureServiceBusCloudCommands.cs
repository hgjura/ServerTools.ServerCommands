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
    public class CloudCommands : BaseCloudCommands
    {
        string q_name_reqs;
        string q_name_resp;
        bool isInitialized;
        ServiceBusAdministrationClient admin_client;
        ServiceBusClient client;
        ServiceBusSender reqs_sender;
        ServiceBusReceiver reqs_receiver;
        ServiceBusReceiver reqs_receiver_dlq;
        ServiceBusSender resp_sender;
        ServiceBusReceiver resp_receiver;
        ServiceBusReceiver resp_receiver_dlq;

        #region Single Command functionality
        public override async Task<Message[]> GetCommandsAsync(int timeWindowinMinutes)
        {
            IReadOnlyList<ServiceBusReceivedMessage> messages = await connectionOptions.RetryPolicy.ExecuteAsync(() => reqs_receiver.ReceiveMessagesAsync(maxMessages: 32, maxWaitTime: TimeSpan.FromMinutes(timeWindowinMinutes)));

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
        public override async Task<Message[]> GetCommandsFromDlqAsync(int timeWindowinMinutes)
        {
            ServiceBusReceiver dlq_reqs_receiver = client.CreateReceiver(reqs_receiver.FullyQualifiedNamespace, new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });

            IReadOnlyList<ServiceBusReceivedMessage> messages = await connectionOptions.RetryPolicy.ExecuteAsync(() => dlq_reqs_receiver.ReceiveMessagesAsync(maxMessages: 32, maxWaitTime: TimeSpan.FromMinutes(timeWindowinMinutes)));

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
            => await _getQueueCountAsync(reqs_receiver.FullyQualifiedNamespace);
        public override async Task<long> GetCommandsDlqCountAsync()
            => await _getQueueCountAsync(reqs_receiver_dlq.FullyQualifiedNamespace);

        public override async Task DeleteCommandAsync(object message)
            => await connectionOptions.RetryPolicy.ExecuteAsync(() => reqs_receiver.CompleteMessageAsync((ServiceBusReceivedMessage)message));
        public override async Task DeleteCommandFromDlqAsync(object message)
           => await connectionOptions.RetryPolicy.ExecuteAsync(() => reqs_receiver_dlq.CompleteMessageAsync((ServiceBusReceivedMessage)message));

        public override async Task<bool> PostCommandAsync(Message message)
        {
            dynamic body = JsonConvert.DeserializeObject<ExpandoObject>(message.Text);
            body.Metadata = message.Metadata;

            await connectionOptions.RetryPolicy.ExecuteAsync(() => reqs_sender.SendMessageAsync(new ServiceBusMessage(JsonConvert.SerializeObject(body))));

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

        public override async Task<Message[]> GetResponsesAsync(int timeWindowinMinutes)
        {
            IReadOnlyList<ServiceBusReceivedMessage> messages = await resp_receiver.ReceiveMessagesAsync(maxMessages: 32, maxWaitTime: TimeSpan.FromMinutes(timeWindowinMinutes));

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
        public override async Task<Message[]> GetResponsesFromDlqAsync(int timeWindowinMinutes)
        {

            ServiceBusReceiver dlq_resp_receiver = client.CreateReceiver(resp_receiver.FullyQualifiedNamespace, new ServiceBusReceiverOptions
            {
                SubQueue = SubQueue.DeadLetter
            });


            IReadOnlyList<ServiceBusReceivedMessage> messages = await resp_receiver.ReceiveMessagesAsync(maxMessages: 32, maxWaitTime: TimeSpan.FromMinutes(timeWindowinMinutes));

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
            => await _getQueueCountAsync(resp_receiver.FullyQualifiedNamespace);
        public override async Task<long> GetResponsesDlqCountAsync()
            => await _getQueueCountAsync(resp_receiver_dlq.FullyQualifiedNamespace);

        public override async Task DeleteResponseAsync(object message)
            => await resp_receiver.CompleteMessageAsync((ServiceBusReceivedMessage)message);
        public override async Task DeleteResponseFromDlqAsync(object message)
            => await resp_receiver_dlq.CompleteMessageAsync((ServiceBusReceivedMessage)message);

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

            reqs_sender = client.CreateSender(q_name_reqs);
            reqs_receiver = client.CreateReceiver(q_name_reqs);
            resp_sender = client.CreateSender(q_name_resp);
            resp_receiver = client.CreateReceiver(q_name_resp);

            reqs_receiver_dlq = client.CreateReceiver(q_name_reqs, new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });
            resp_receiver_dlq = client.CreateReceiver(q_name_resp, new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });

            var options1 = new CreateQueueOptions(q_name_reqs)
            {
                //AutoDeleteOnIdle = TimeSpan.FromDays(7),
                DefaultMessageTimeToLive = TimeSpan.FromDays(1),
                DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(1),
                EnableBatchedOperations = true,
                DeadLetteringOnMessageExpiration = true,
                EnablePartitioning = false,
                ForwardDeadLetteredMessagesTo = null,
                ForwardTo = null,
                LockDuration = TimeSpan.FromSeconds(45),
                MaxDeliveryCount = 8,
                MaxSizeInMegabytes = 2048//,
                //RequiresDuplicateDetection = true,
                //RequiresSession = true
            };

            options1.AuthorizationRules.Add(new SharedAccessAuthorizationRule(
                "allClaims",
                new[] { AccessRights.Manage, AccessRights.Send, AccessRights.Listen }));

            if (!(await admin_client.QueueExistsAsync(q_name_reqs)).Value) await admin_client.CreateQueueAsync(options1);

            var options2 = new CreateQueueOptions(q_name_resp)
            {
                //AutoDeleteOnIdle = TimeSpan.FromDays(7),
                DefaultMessageTimeToLive = TimeSpan.FromDays(1),
                DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(1),
                EnableBatchedOperations = true,
                DeadLetteringOnMessageExpiration = true,
                EnablePartitioning = false,
                ForwardDeadLetteredMessagesTo = null,
                ForwardTo = null,
                LockDuration = TimeSpan.FromSeconds(45),
                MaxDeliveryCount = 8,
                MaxSizeInMegabytes = 2048//,
                //RequiresDuplicateDetection = true,
               // RequiresSession = true
            };

            options2.AuthorizationRules.Add(new SharedAccessAuthorizationRule(
                "allClaims",
                new[] { AccessRights.Manage, AccessRights.Send, AccessRights.Listen }));

            await connectionOptions.RetryPolicy.ExecuteAsync(async () =>
            {
                if (!(await admin_client.QueueExistsAsync(q_name_resp)).Value) await admin_client.CreateQueueAsync(options2);

                client.CreateSender(q_name_reqs);
                client.CreateSender(q_name_resp);
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
