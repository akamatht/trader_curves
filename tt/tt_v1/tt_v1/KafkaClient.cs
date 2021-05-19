using System;
using System.Threading.Tasks;
using System.Reflection;
using System.Text;
using log4net;
using Confluent.Kafka;

namespace Subscriber
{
    public class KafkaClient
    {
        static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        string _topic;
        ProducerConfig _producerConfig;
        IProducer<string, string> _producer;

        public KafkaClient(string bootstrapServers, string topic)
        {
            ConfigureProducer(bootstrapServers);

            _topic = topic;
        }

        void ConfigureProducer(string bootstrapServers)
        {
            _producerConfig = new ProducerConfig { BootstrapServers = bootstrapServers };
        }

        public void Run()
        {
            _log.Info("Kafka Producer builder");

            try
            {
                _producer = new ProducerBuilder<string, string>(_producerConfig).Build();
            }
            catch (Exception e)
            {
                _log.Error($"Kafka Producer builder failed ", e);
                throw;
            }
        }

        public void Publish(string key, string payload)
        {
            try
            {
                _producer.Produce(_topic, new Message<string, string> { Key=key ,Value = payload }, handler);
            }
            catch (ProduceException<Null, string> e)
            {
                _log.Error($"Kafka Producer delivery failed: {e.Error.Reason} ");
            }
            catch (Exception e)
            {
                _log.Error($"EXCEPTION: Kafka Producer ", e);
                throw;
            }
        }

        public static void handler(DeliveryReport<string, string> dr)
        {
            if (dr.Error.IsError)
            {
                _log.Error($"Kafka Producer delivery error: {dr.Error.Reason}");
            }
            //else
            //{
            //    _log.Debug($"Delivered to '{dr.TopicPartitionOffset}' : {dr.Message.Value}");
            //}
        }

        public void Stop()
        {
            _log.Info("Stopping KafkaClient");

            Dispose();
        }

        void Dispose()
        {
            _log.Info("Flushing and Disposing Kafka Producer ");

            try
            {
                _producer.Flush(TimeSpan.FromSeconds(10));
                _producer.Dispose();
            }
            catch (Exception e)
            {
                _log.Error("EXCEPTION: Flushing and Disposing Kafka Producer", e);
                throw;
            }
        }
    }
}
