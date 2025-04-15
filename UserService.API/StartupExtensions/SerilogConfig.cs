using Microsoft.Extensions.Configuration;
using Serilog;

namespace UserService.API.StartupExtensions
{
    public static class SerilogConfig
    {
        /*public static void ConfigureLogging(IConfiguration configuration)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration) 
                .CreateLogger();
        }*/
    }
}
