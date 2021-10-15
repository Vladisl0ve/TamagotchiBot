using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Telegram.Bots.Extensions.Polling;

namespace Telegram.Bots.Example
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateGlobalLoggerConfiguration();
            Log.Information("Starting host");
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var builder = new HostBuilder()
                  .ConfigureAppConfiguration(x =>
                  {
                      var conf = new ConfigurationBuilder()
                               .SetBasePath(Directory.GetCurrentDirectory())
                               .AddJsonFile("config.json", false, true)
                               .Build();

                      x.AddConfiguration(conf);
                  })
                  .UseSerilog()
                  .ConfigureServices((context, services) =>
                  {
                      services.AddBotClient(context.Configuration["token"]);
                      services.AddPolling<UpdateHandler>();
                  });

            return builder;
        }
        public static void CreateGlobalLoggerConfiguration()
        {
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .WriteTo.Console().CreateLogger();
        }
    }
}