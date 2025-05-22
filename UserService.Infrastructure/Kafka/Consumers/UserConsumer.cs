using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using UserService.Core.DTO;
using UserService.Infrastructure.Kafka.Handlers;

namespace UserService.Infrastructure.Kafka.Consumers
{
    public class UserConsumer : BackgroundService
    {
        private readonly IConsumer<string, string> _consumer;
        private readonly IServiceProvider _serviceProvider;
        private readonly KafkaSettings _kafkaSettings;

        public UserConsumer(IConfiguration config, IServiceProvider serviceProvider, IOptions<KafkaSettings> kafkaOptions)
        {
            _kafkaSettings = kafkaOptions.Value;

            try
            {
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

                // Khởi tạo Kafka consumer
                _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
                _consumer.Subscribe(allTopics);
            }
            catch (Exception ex)
            {
                // Log lỗi nếu không thể khởi tạo hoặc subscribe Kafka consumer
                Console.WriteLine($"Lỗi khi khởi tạo Kafka consumer: {ex.Message}");
                throw; // Ném lại exception nếu không thể tiếp tục khởi tạo
            }

            _serviceProvider = serviceProvider;
            _serviceProvider = serviceProvider;
        }
        /*protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = _consumer.Consume(stoppingToken);
                var topic = result.Topic;
                var message = result.Message.Value;
                using var scope = _serviceProvider.CreateScope();

                switch (topic)
                {
                    
                    case "get-all-user":
                        Console.WriteLine("Receive : {0}", message);
                        var filterHandler = scope.ServiceProvider.GetRequiredService<IKafkaHandler<KafkaRequest<UserInfoRequestDTO>>>();
                        var filterData = JsonSerializer.Deserialize<KafkaRequest<UserInfoRequestDTO>>(message);
                        await filterHandler.HandleAsync(filterData);
                        break;
                    case "account-create":
                        Console.WriteLine("Receive : {0}", message);
                        var accountHandler = scope.ServiceProvider.GetRequiredService<IKafkaHandler<KafkaRequest<AccountCreateRequest>>>();
                        var accountData = JsonSerializer.Deserialize<KafkaRequest<AccountCreateRequest>>(message);
                        await accountHandler.HandleAsync(accountData);
                        break;
                }
            }
        }*/
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Tách logic consume ra 1 thread riêng
            Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = _consumer.Consume(stoppingToken);
                        var topic = result.Topic;
                        var message = result.Message.Value;

                        using var scope = _serviceProvider.CreateScope();

                        switch (topic)
                        {
                            case "get-all-user":
                                Console.WriteLine("Receive : {0}", message);
                                var filterHandler = scope.ServiceProvider.GetRequiredService<IKafkaHandler<KafkaRequest<UserInfoRequestDTO>>>();
                                var filterData = JsonSerializer.Deserialize<KafkaRequest<UserInfoRequestDTO>>(message);
                                await filterHandler.HandleAsync(filterData);
                                break;

                            case "account-create":
                                Console.WriteLine("Receive : {0}", message);
                                var accountHandler = scope.ServiceProvider.GetRequiredService<IKafkaHandler<KafkaRequest<AccountCreateRequest>>>();
                                var accountData = JsonSerializer.Deserialize<KafkaRequest<AccountCreateRequest>>(message);
                                await accountHandler.HandleAsync(accountData);
                                break;
                        }
                    }
                    catch (ConsumeException ex)
                    {
                        Console.WriteLine($"Kafka consume error: {ex.Error.Reason}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"General error in UserConsumer: {ex.Message}");
                    }
                }
            }, stoppingToken);

            return Task.CompletedTask;
        }


        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _consumer.Close();    // Dừng Kafka consumer một cách "gracefully"
            _consumer.Dispose();  // Giải phóng tài nguyên
            return base.StopAsync(cancellationToken); // Gọi base nếu có thêm xử lý mặc định
        }

    }
}
