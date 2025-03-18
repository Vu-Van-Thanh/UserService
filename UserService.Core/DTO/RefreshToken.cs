using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserService.Core.DTO
{
    public class RefreshTokenRequest
    {
        public Guid UserId { get; set; }
        public string RefreshToken { get; set; }
    }
}
