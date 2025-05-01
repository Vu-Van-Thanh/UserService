using UserService.Core.Domain.IdentityEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UserService.Infrastructure.DbContext;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UserService.Core.Services;
using UserService.Core.DTO;
using UserService.Infrastructure.Kafka.Handlers;
using UserService.API.Kafka.Producer;
using UserService.Infrastructure.Kafka;
using UserService.Infrastructure.Kafka.Consumers;

namespace UserServiceRegistry
{
    public static class ConfigureServicesExtension
    {
        public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Cấu hình DbContext
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            });


            // 2. Cấu hình Identity
            services.AddScoped<ITokenService, TokenService>();
            services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequiredUniqueChars = 3;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddUserStore<UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, Guid>>()
            .AddRoleStore<RoleStore<ApplicationRole, ApplicationDbContext, Guid>>();

            services.AddScoped<IKafkaHandler<KafkaRequest<UserInfoRequestDTO>>, GetUserHandler>();
            services.AddScoped<IKafkaHandler<KafkaRequest<AccountCreateRequest>>, CreateAccountHandler>();
            services.AddScoped<IEventProducer, UserProducer>();
            services.Configure<KafkaSettings>(configuration.GetSection("Kafka"));
            services.AddHostedService<UserConsumer>();
            // 3. Cấu hình JWT Authentication
            var jwtSettings = configuration.GetSection("JwtSettings");
            var secretKey = Encoding.UTF8.GetBytes(configuration["JwtSettings:Secret"]);
            //var secretKey = Encoding.UTF8.GetBytes("VUVANTHANH2K3_DOANTOTNGHIEP_2025");

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwtSettings["Issuer"],
                    //ValidIssuer= "https://localhost:7198/",
                    ValidAudience = jwtSettings["Audience"],
                    //ValidAudience= "https://localhost:7198/",
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(secretKey)
                };
                /* options.Events = new JwtBearerEvents
                 {
                     OnChallenge = context =>
                     {
                         context.HandleResponse();
                         context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                         context.Response.ContentType = "application/json";
                         var result = System.Text.Json.JsonSerializer.Serialize(new { message = "Unauthorized" });
                         return context.Response.WriteAsync(result);
                     }
                 };*/
                options.Events = new JwtBearerEvents
                {
                   
                    OnMessageReceived = context =>
                    {
                        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                       
                        Console.WriteLine($"🧪 Raw token: {authHeader}");
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var ex = context.Exception;
                        

                        if (ex is SecurityTokenExpiredException expiredException)
                        {
                            Console.WriteLine("⚠️ Token expired at: " + expiredException.Expires);
                        }
                        else if (ex is SecurityTokenInvalidIssuerException)
                        {
                            Console.WriteLine("⚠️ Invalid Issuer!");
                        }
                        else if (ex is SecurityTokenInvalidAudienceException)
                        {
                            Console.WriteLine("⚠️ Invalid Audience!");
                        }
                        else if (ex is SecurityTokenInvalidSignatureException)
                        {
                            Console.WriteLine("⚠️ Invalid Signature!");
                        }
                        else
                        {
                            Console.WriteLine("❌ Token authentication failed!");
                            Console.WriteLine($"Message: {ex.Message}");
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        // Log khi token hợp lệ và đã được xác thực thành công
                        Console.WriteLine("✅ Token validated successfully");
                        return Task.CompletedTask;
                    }
                };
            });

            // 4. Cấu hình CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // 5. Cấu hình Authorization
            //services.AddAuthorization();

            // 6. Cấu hình Swagger
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "User Service API",
                    Version = "v1",
                    Description = "API quản lý người dùng trong hệ thống microservices",
                    Contact = new OpenApiContact
                    {
                        Name = "Hỗ trợ API",
                        Email = "support@example.com",
                        Url = new Uri("https://example.com")
                    }
                });
                options.AddServer(new OpenApiServer
                {
                    Url = "https://localhost:7198"
                });
                // Thêm cấu hình JWT vào Swagger
                var jwtSecuritiySchema = new OpenApiSecurityScheme
                {
                    BearerFormat = "JWT",
                    Description = "Nhập token vào ô bên dưới (không cần Bearer):",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = JwtBearerDefaults.AuthenticationScheme
                    }
                };
                options.AddSecurityDefinition("Bearer", jwtSecuritiySchema);
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        jwtSecuritiySchema,
                        new List<string>()
                    }
                });
            });
            services.AddControllers();

            return services;
        }

    }
}
