using UserServiceRegistry;
using Consul;
using UserService.API.StartupExtensions;

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
builder.Services.AddControllers();

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

// Đăng ký khi app start
await consulClient.Agent.ServiceRegister(registration);

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

app.UseCors("MyCorsPolicy");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
