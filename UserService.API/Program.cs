using UserServiceRegistry;
using Consul;
using UserService.API.StartupExtensions;
using UserService.API.Middlewares;

var builder = WebApplication.CreateBuilder(args);


//SerilogConfig.ConfigureLogging(builder.Configuration);
// Thêm Serilog vào Dependency Injection (DI) container
/*builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration)
                 .ReadFrom.Services(services);
});*/

builder.Services.ConfigureServices(builder.Configuration);
var consulConfig = builder.Configuration.GetSection("ConsulConfig").Get<ConsulConfig>();
// Add Consul client
builder.Services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(cfg =>
{
    cfg.Address = new Uri(consulConfig.ConsulAddress);
}));

var app = builder.Build();

//đăng kí consul
var lifetime = app.Lifetime;
var consulClient = app.Services.GetRequiredService<IConsulClient>();
var uri = new Uri($"{consulConfig.ServiceAddress}:{consulConfig.ServicePort}");

var registration = new AgentServiceRegistration()
{
    ID = consulConfig.ServiceId,
    Name = consulConfig.ServiceName,
    Address = uri.Host,
    Port = uri.Port,
    Check = new AgentServiceCheck
    {
        HTTP = $"{uri.Scheme}://{uri.Host}:{uri.Port}/health",
        Interval = TimeSpan.FromSeconds(10),
        Timeout = TimeSpan.FromSeconds(5),
        DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(30)
    }
};

//await consulClient.Agent.ServiceRegister(registration);

// Hủy đăng ký khi app stop
lifetime.ApplicationStopping.Register(() =>
{
    consulClient.Agent.ServiceDeregister(registration.ID).Wait();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Service API v1");
        c.RoutePrefix = "swagger"; 
    });
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseRouting();              // ✅ Phân tích route từ URL
// Debug middleware để log request và header
app.Use(async (context, next) =>
{
    Console.WriteLine("📥 Incoming request:");
    Console.WriteLine($"➡️ Path: {context.Request.Path}");
    Console.WriteLine("🔑 Headers:");

    foreach (var header in context.Request.Headers)
    {
        Console.WriteLine($"{header.Key}: {header.Value}");
    }

    Console.WriteLine($"🧪 User authenticated: {context.User.Identity?.IsAuthenticated}");

    await next(); // Đừng quên gọi middleware tiếp theo
});
app.UseAuthentication();      // ✅ Kiểm tra token (JWT)

app.UseAuthorization();       // ✅ Áp chính sách [Authorize]
app.Use(async (context, next) =>
{
    var user = context.User;
    Console.WriteLine($"👤 Authenticated User: {user?.Identity?.Name}");
    Console.WriteLine($"✅ IsAuthenticated: {user?.Identity?.IsAuthenticated}");

    await next();
});
app.MapControllers();

app.Run();
