// using Confluent.Kafka;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Options;
// using Newtonsoft.Json;
// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
//
// namespace MosaicAddin.Kafka
// {
//     public readonly struct Envelope<T>
//     {
//         public Envelope(T payload, DateTime timestamp, string key)
//         {
//             Payload = payload;
//             Timestamp = timestamp;
//             Key = key;
//         }
//
//         public T Payload { get; }
//         public DateTime Timestamp { get; }
//         public string Key { get; }
//     }
//
//     public interface ITopicSubscriber<T> : IDisposable
//     {
//         void Subscribe(Action<Envelope<T>> onMessage, Action<int, long> onPartitionEof, bool newMessagesOnly);
//     }
//
//
//     public class KafkaTopicSubscriber<T> : ITopicSubscriber<T>, IDisposable
//     {
//         private readonly string topic;
//         private IConsumer<string, string> consumer;
//         private Task ConsumerTask;
//         private bool IsActive { get; set; }
//         private bool hasCaughtUp;
//
//         public KafkaTopicSubscriber(string topic)
//         {
//             this.topic = topic;
//         }
//
//         public void Subscribe(Action<Envelope<T>> onMessage, Action<int, long> onPartitionEof, bool newMessagesOnly)
//         {
//             lock (this)
//             {
//                 consumer = consumer ?? new ConsumerBuilder<string, string>(
//                     new ConsumerConfig
//                     {
//                         BootstrapServers = settings.Value.BootstrapServers,
//                         AutoOffsetReset = newMessagesOnly ? AutoOffsetReset.Latest : AutoOffsetReset.Earliest,
//                         GroupId = $"{Environment.MachineName}|{Environment.UserName.Replace("HETCO\\", "")}|{Guid.NewGuid()}",
//                         EnablePartitionEof = true,
//                     }).Build();
//
//                 if (IsActive) throw new InvalidOperationException($"Already subscribed to topic {topic}");
//                 logger.LogInformation("Subscribing to {consumerTopic}", topic);
//                 ConsumerTask = Task.Factory.StartNew(() =>
//                 {
//                     Consume(onMessage, onPartitionEof);
//                 });
//                 IsActive = true;
//             }
//         }
//
//
//
//         public void Consume(Action<Envelope<T>> onMessage, Action<int, long> onPartitionEof)
//         {
//             int messageCount = 0;
//             var d = DateTime.UtcNow;
//             consumer.Subscribe(topic);
//             while (IsActive)
//             {
//                 var msg = consumer.Consume(settings.Value.PollFrequency);
//                 messageCount++;
//
//                 if (msg == default) continue;
//
//                 if (msg.IsPartitionEOF)
//                 {
//                     logger.LogInformation("Subscriber for {consumerTopic} at partition EOF {offset}.  Count: {messageCount}.  Duration: {duration}ms", msg.Topic, msg.Offset, messageCount, (DateTime.UtcNow - d).TotalMilliseconds);
//                     onPartitionEof(msg.Partition.Value, msg.Offset.Value);
//                     continue;
//                 }
//                 
//                 if (string.Equals("null", msg.Message.Value, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(msg.Message.Value))
//                 {
//                     logger.LogWarning("Disregarding empty/null message.");
//                     return;
//                 }
//
//                 var data = JsonConvert.DeserializeObject<T>(msg.Message.Value);
//                 var key = msg.Message.Key?.ToString();
//                 var timestamp = msg.Message.Timestamp.UtcDateTime;
//
//                 try
//                 {
//                     onMessage(new Envelope<T>(data, timestamp, key));
//                 }
//                 catch(Exception ex)
//                 {
//                     logger.LogError(ex, "Error while processing message for topic {consumerTopic} at offset {offset}", topic, msg.Offset);
//                     throw;
//                 }
//
//                 logger.LogDebug("Message received on topic {consumerTopic} at offset {offset} processed.", msg.Topic, msg.Offset);
//             }
//             consumer.Dispose();
//         }
//
//         public void Dispose()
//         {
//             lock(this)
//             {
//                 if (!IsActive) throw new InvalidOperationException($"Subcriber for topic {topic} already disposed");
//                 logger.LogInformation("Disposing subscriber for topic {consumerTopic}", topic);
//                 IsActive = false;
//                 Task.WaitAll(ConsumerTask);
//                 hasCaughtUp = false;
//             }
//         }
//     }
// }
