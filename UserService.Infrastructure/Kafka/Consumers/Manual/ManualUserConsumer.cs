

using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace UserService.Infrastructure.Kafka.Consumers.Manual
{
    public class ManualUserConsumer : IEventConsumer
    {
        private readonly IConsumer<string, string> _consumer;
        private readonly IServiceProvider _serviceProvider;
        private readonly KafkaSettings _kafkaSettings;

        public ManualUserConsumer(IConfiguration config, IServiceProvider serviceProvider, IOptions<KafkaSettings> kafkaOptions)
        {
            _kafkaSettings = kafkaOptions.Value;
            ConsumerConfig consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _kafkaSettings?.BootstrapServers,
                GroupId = _kafkaSettings?.GroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
            List<string> allTopics = _kafkaSettings.ConsumeTopicNames
                            .SelectMany(entry => entry.Value)
                            .Distinct()
                            .ToList();
            _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            _consumer.Subscribe(allTopics);
            _serviceProvider = serviceProvider;
        }
        public Task<T> ConsumeAsync<T>(string topic, CancellationToken cancellationToken = default)
        {
            try
            {
                var consumeResult = _consumer.Consume(cancellationToken);

                if (consumeResult == null || string.IsNullOrEmpty(consumeResult.Message.Value))
                {
                    throw new Exception("No message received from Kafka topic.");
                }

                // Deserialize JSON thành T
                T result = JsonSerializer.Deserialize<T>(consumeResult.Message.Value, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })!;

                return Task.FromResult(result);
            }
            catch (ConsumeException ex)
            {
                throw new Exception($"Error consuming message from Kafka: {ex.Error.Reason}", ex);
            }
            catch (JsonException ex)
            {
                throw new Exception("Failed to deserialize Kafka message.", ex);
            }
        }
    }
    
}
