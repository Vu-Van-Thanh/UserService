
using Microsoft.AspNetCore.Identity;

namespace UserService.Core.Domain.IdentityEntities
{
    public class ApplicationRole : IdentityRole<Guid>
    {
        public string? Description { get; set; }
    }
}
