using Confluent.Kafka;
using UserService.Infrastructure.Kafka;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace UserService.API.Kafka.Producer
{
    public class UserProducer : IEventProducer
    {
        private readonly IProducer<string, string> _producer;
        private readonly KafkaSettings _kafkaSettings;


        public UserProducer(IOptions<KafkaSettings> kafkaSettings)
        {
            _kafkaSettings = kafkaSettings.Value;
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _kafkaSettings.BootstrapServers
            };

            _producer = new ProducerBuilder<string, string>(producerConfig).Build();
        }

        public async Task PublishAsync<T>(string topic, int? partition, string? key,T @event)
        {
            var message = JsonSerializer.Serialize(@event);
            var kafkaMessage = new Message<string, string>
            {
                Key = key,
                Value = message
            };
            if(partition.HasValue)
            {
                await _producer.ProduceAsync(new TopicPartition(topic, new Partition(partition.Value)), kafkaMessage);
            }
            else
            {
                await _producer.ProduceAsync(topic, kafkaMessage);
            }    
            
        }
    }

}
