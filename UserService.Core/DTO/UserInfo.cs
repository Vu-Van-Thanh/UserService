namespace UserService.Infrastructure.Kafka.Handlers
{
    public class UserInfo
    {
        public string? AccountId { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Avartar {get;set;}
        public bool IsActive { get; set; }
    }
}