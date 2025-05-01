

using EmployeeService.Infrastructure.Kafka.KafkaEntity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserService.API.Kafka.Producer;
using UserService.Core.Domain.IdentityEntities;
using UserService.Core.DTO;

namespace UserService.Infrastructure.Kafka.Handlers
{
    public class CreateAccountHandler : IKafkaHandler<KafkaRequest<AccountCreateRequest>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEventProducer _eventProducer;


        public CreateAccountHandler(UserManager<ApplicationUser> userManager , IEventProducer eventProducer)
        {
            
            _userManager= userManager;
            _eventProducer = eventProducer;
        }
        public async Task HandleAsync(KafkaRequest<AccountCreateRequest> message)
        {
            AccountCreateRequest request = message.Filter;

            // Tạo email duy nhất từ username
            string email = await GenerateUniqueEmailAsync(request.Username);

            var user = new ApplicationUser
            {
                UserName = request.Username,
                Email = email,
                EmailConfirmed = true // nên xác nhận luôn nếu từ hệ thống nội bộ
            };

            // Tạo user với mật khẩu mặc định là username viết thường (nên đổi nếu có yêu cầu bảo mật)
            var result = await _userManager.CreateAsync(user, request.Username.ToLower());

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                await _eventProducer.PublishAsync("account-created", null, "account-created", new KafkaRequest<AccountCreateResponse>
                {
                    CorrelationId = message.CorrelationId,
                    RequestType = "AccountCreated",
                    Timestamp = DateTime.UtcNow,
                    Filter = new AccountCreateResponse { Email = email, Password = request.Username.ToLower(), EmployeeId = request.EmployeeId, Status = true }   
                });
            }
            else
            {
                await _eventProducer.PublishAsync("account-created", null, "account-created", new KafkaRequest<AccountCreateResponse>
                {
                    CorrelationId = message.CorrelationId,
                    RequestType = "AccountCreated",
                    Timestamp = DateTime.UtcNow,
                    Filter = new AccountCreateResponse { Email = email, Password = request.Username.ToLower(), EmployeeId = request.EmployeeId, Status = false }   
                });
                // Ghi log hoặc ném lỗi nếu tạo user không thành công
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Không thể tạo user: {errors}");

            }
        }

        private async Task<string> GenerateUniqueEmailAsync(string username)
        {
            string domain = "hrm.tv.com";
            string baseEmail = $"{username}@{domain}";
            if (!await _userManager.Users.AnyAsync(u => u.Email == baseEmail))
                return baseEmail;

            // Thử với hậu tố số
            for (int i = 1; i <= 5; i++)
            {
                string emailWithNumber = $"{username}{i}@{domain}";
                if (!await _userManager.Users.AnyAsync(u => u.Email == emailWithNumber))
                    return emailWithNumber;
            }

            // Thử với năm hiện tại
            string emailWithYear = $"{username}{DateTime.Now.Year}@{domain}";
            if (!await _userManager.Users.AnyAsync(u => u.Email == emailWithYear))
                return emailWithYear;

            // Thử với hậu tố random
            for (int i = 0; i < 3; i++)
            {
                string randomSuffix = Guid.NewGuid().ToString("N")[..6]; 
                string emailWithRandom = $"{username}_{randomSuffix}@{domain}";
                if (!await _userManager.Users.AnyAsync(u => u.Email == emailWithRandom))
                    return emailWithRandom;
            }

            // Cuối cùng, dùng GUID đầy đủ
            return $"{username}.{Guid.NewGuid().ToString("N")[..8]}@{domain}";
        }

    }
}
