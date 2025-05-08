

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
            var accountIds = message.Filter.AccountId
                             .Split(',')
                             .Select(id => Guid.TryParse(id.Trim(), out Guid accountId) ? accountId : (Guid?)null)
                             .Where(id => id.HasValue)
                             .Select(id => id.Value)
                             .ToList();

            if (!accountIds.Any())
            {
                var errorResponse = new KafkaResponse<List<UserInfo>>
                {
                    RequestType = message.RequestType,
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = message.CorrelationId,
                    Filter = new List<UserInfo>()  // Có thể để trống hoặc trả về thông báo lỗi ở đây
                };

                // Publish sự kiện lỗi (hoặc thông báo trống)
                await _eventProducer.PublishAsync("User-Info", null, null, errorResponse);
                return;
            }
            var users = await _userManager.Users
                                   .Where(u => accountIds.Contains(u.Id))
                                   .ToListAsync();
            var userInfos = users.Select(user => new UserInfo
            {
                AccountId = user.Id.ToString(),
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                IsActive = user.IsActive
            }).ToList();
            var response = new KafkaResponse<List<UserInfo>>
            {
                RequestType = message.RequestType,
                Timestamp = DateTime.UtcNow,
                CorrelationId = message.CorrelationId,
                Filter = userInfos
            };
            await _eventProducer.PublishAsync("User-Info", null, null, response);

        }
    }
}
