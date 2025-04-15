using UserServiceRegistry;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserService.Core.Domain.IdentityEntities;
using UserService.Infrastructure.DbContext;
using Serilog;
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

builder.Services.AddControllers();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Service API v1");
        c.RoutePrefix = "swagger"; 
    });
}

/*app.UseSerilogLoggingMiddleware();
app.UseSerilogRequestLogging();*/

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
