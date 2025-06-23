using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserService.Core.DTO
{
    public class UpdateUserProfileRequest
    {
        public string? email { get; set; }

        public string? phone { get; set; }

        public IFormFile? avatar { get; set; }
    }
}
