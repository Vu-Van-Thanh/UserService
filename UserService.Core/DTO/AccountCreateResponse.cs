namespace UserService.Core.DTO
{
    public class AccountCreateResponse
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string EmployeeId { get; set; }  
        public bool Status { get; set; }
    }
}
