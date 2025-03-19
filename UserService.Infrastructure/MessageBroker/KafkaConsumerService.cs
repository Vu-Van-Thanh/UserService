using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using UserService.Core.Domain.IdentityEntities;

namespace MessageBroker
{
    public class KafkaConsumerService
    {
        private readonly string _bootstrapServers;
        private readonly string _topic;
        private readonly string _groupId;
        private readonly UserManager<ApplicationUser> _userManager;

        public KafkaConsumerService(IConfiguration configuration, UserManager<ApplicationUser> userManager)
        {
            _bootstrapServers = configuration["Kafka:BootstrapServers"];
            _topic = configuration["Kafka:Topic"];
            _groupId = "user-service-group";
            _userManager = userManager;
        }

        public async Task StartConsumerAsync()
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = _groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe(_topic);

            Console.WriteLine("User Service đang lắng nghe sự kiện Kafka...");

            while (true)
            {
                try
                {
                    var consumeResult = consumer.Consume(CancellationToken.None);
                    var employeeUpdate = JsonSerializer.Deserialize<EmployeeUpdateEvent>(consumeResult.Message.Value);

                    Console.WriteLine($"📩 Recieved: {consumeResult.Message.Value}");

                    // Xử lý cập nhật thông tin user trong User Service
                    await UpdateUserFromEmployeeAsync(employeeUpdate);

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi xử lý Kafka: {ex.Message}");
                }
            }
        }

        private async Task UpdateUserFromEmployeeAsync(EmployeeUpdateEvent updateEvent)
        {
            var user = await _userManager.FindByIdAsync(updateEvent.UserId.ToString());
            if (user == null)
            {
                Console.WriteLine($"⚠️ Not found User with ID: {updateEvent.UserId}");
                return;
            }

            bool isUpdated = false;

            if (!string.IsNullOrEmpty(updateEvent.Phone) && user.PhoneNumber != updateEvent.Phone)
            {
                user.PhoneNumber = updateEvent.Phone;
                isUpdated = true;
            }

            if (!string.IsNullOrEmpty(updateEvent.Email) && user.Email != updateEvent.Email)
            {
                user.Email = updateEvent.Email;
                isUpdated = true;
            }

            if (isUpdated)
            {
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    Console.WriteLine($"✅ User {user.Id} đã được cập nhật thông tin.");
                }
                else
                {
                    Console.WriteLine($"❌ Lỗi khi cập nhật User {user.Id}: {string.Join(", ", result.Errors)}");
                }
            }
            else
            {
                Console.WriteLine($"🔹 Không có thay đổi cần cập nhật cho User {user.Id}.");
            }
        }
    }

    public class EmployeeUpdateEvent
    {
        public Guid UserId { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

}
