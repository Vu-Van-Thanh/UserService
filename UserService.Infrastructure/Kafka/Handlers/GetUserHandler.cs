

using EmployeeService.Infrastructure.Kafka.KafkaEntity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserService.API.Kafka.Producer;
using UserService.Core.Domain.IdentityEntities;
using UserService.Core.DTO;

namespace UserService.Infrastructure.Kafka.Handlers
{
    public class GetUserHandler : IKafkaHandler<KafkaRequest<UserInfoRequestDTO>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEventProducer _eventProducer;


        public GetUserHandler(UserManager<ApplicationUser> userManager , IEventProducer eventProducer)
        {
            
            _userManager= userManager;
            _eventProducer = eventProducer;
        }
        public async Task HandleAsync(KafkaRequest<UserInfoRequestDTO> message)
        {
            if (!Guid.TryParse(message.Filter.AccountId, out Guid accountId))
            {
                // Handle lỗi parse GUID, có thể throw hoặc return
                throw new ArgumentException("Invalid AccountId format");
            }

            var user = await _userManager.Users
                            .FirstOrDefaultAsync(u => u.Id == accountId);
            KafkaResponse<UserInfo> response = new KafkaResponse<UserInfo>            {
                RequestType = message.RequestType,
                Timestamp = DateTime.UtcNow,
                CorrelationId = message.CorrelationId,
                Filter = new UserInfo { 
                    AccountId = user?.Id.ToString() ?? string.Empty, 
                    UserName = user?.UserName ?? string.Empty,
                    Email = user?.Email ?? string.Empty,
                    PhoneNumber = user?.PhoneNumber ?? string.Empty,
                    IsActive = user?.IsActive ?? false
                }
               
            };
            await _eventProducer.PublishAsync("User-Info", null, null, response);

        }
    }
}
