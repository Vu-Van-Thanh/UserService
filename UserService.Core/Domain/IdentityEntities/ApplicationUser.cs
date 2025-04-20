
using Microsoft.AspNetCore.Identity;

namespace UserService.Core.Domain.IdentityEntities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string? FullName { get; set; }    
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? AvatarUrl { get; set; }  
        public bool IsActive { get; set; } = true;

        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}
