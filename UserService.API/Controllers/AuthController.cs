using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserService.Core.Domain.IdentityEntities;
using UserService.Core.DTO;
using UserService.Core.Services;

namespace UserService.API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ITokenService _tokenService;

        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration, ITokenService tokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _tokenService = tokenService;
        }
        [AllowAnonymous]
        [HttpPost("register")] 
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                IsActive = true,
                RefreshToken = _tokenService.GenerateRefreshToken(),
                RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, model.Role);

            var token = _tokenService.GenerateJwtToken(user);
            return Ok(new { accessToken = token, refresh_token = user.RefreshToken });
        }
        [AllowAnonymous]
        [HttpPost("login")] 
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized("Invalid credentials");

            user.RefreshToken = _tokenService.GenerateRefreshToken();
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            var token = _tokenService.GenerateJwtToken(user);
            return Ok(new { accessToken = token, refresh_token = user.RefreshToken,isSuccess=true,
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    roles = roles,
                    FullName = user.FullName,
                    avartar = user.AvatarUrl
                }
            });
        }

        [HttpPost("tokens")] 
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (request is null || string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest(new { message = "Invalid request" });
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);
            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return Unauthorized(new { message = "Invalid or expired refresh token" });
            }

            var newAccessToken = _tokenService.GenerateJwtToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            return Ok(new
            {
                accessToken = newAccessToken,
                refresh_token = newRefreshToken
            });
        }

        [HttpPost("validate")]
        [AllowAnonymous]
        public IActionResult ValidateJwt([FromBody] string token)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]);
            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                // Xác thực JWT
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "https://localhost:7198/",
                    ValidAudience = "https://localhost:7198/",
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };

                // Thực hiện xác thực token
                tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

                return Ok(new { message = "JWT is valid" }); 
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Invalid JWT", error = ex.Message }); 
            }
        }
        [HttpPost("change-password")] 
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest model)
        {
            var debugClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            Console.WriteLine("===> Claims:");
            foreach (var claim in debugClaims)
            {
                Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
            }
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); 
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return Unauthorized("User not found");

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // ✅ Cập nhật Refresh Token mới
            user.RefreshToken = _tokenService.GenerateRefreshToken();
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            // ✅ Cấp lại Access Token mới
            var newAccessToken = _tokenService.GenerateJwtToken(user);

            return Ok(new
            {
                accessToken = newAccessToken,
                refresh_token = user.RefreshToken,
                message = "Password changed successfully"
            });
        }

        [HttpPut("profile")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateUserProfileRequest model )
        {
            var user = await _userManager.FindByEmailAsync(model.email);
            if (user == null)
                return Unauthorized("User not found");

            // Cập nhật Email và Phone nếu được gửi lên
            if (!string.IsNullOrWhiteSpace(model.email))
                user.Email = model.email;

            if (!string.IsNullOrWhiteSpace(model.phone))
                user.PhoneNumber = model.phone;

            
            if (model.avatar != null && model.avatar.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "AvatarUser");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.avatar.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.avatar.CopyToAsync(stream);
                }

                var relativePath = $"/AvatarUser/{fileName}";
                user.AvatarUrl = relativePath;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new
            {
                message = "Cập nhật thông tin người dùng thành công",
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    phone = user.PhoneNumber,
                    avatar = user.AvatarUrl,
                    fullName = user.FullName
                }
            });
        }


    }
}
