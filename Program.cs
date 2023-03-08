using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using System.IO;
using TamagotchiBot.Database;
using TamagotchiBot.Handlers;
using TamagotchiBot.Services;
using Telegram.Bot;
using Telegram.Bot.Polling;

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
                      services.AddHostedService<TelegramBotHostedService>();
                      services.AddTransient<IUpdateHandler, UpdateHandler>();
                      services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(context.Configuration["TokenBot"]));



                      services.Configure<TamagotchiDatabaseSettings>(context.Configuration.GetSection(nameof(TamagotchiDatabaseSettings)));
                      services.AddSingleton<ITamagotchiDatabaseSettings>(sp => sp.GetRequiredService<IOptions<TamagotchiDatabaseSettings>>().Value);

                      services.AddSingleton<UserService>();
                      services.AddSingleton<PetService>();
                      services.AddSingleton<ChatService>();


                      services.AddLocalization(options => options.ResourcesPath = "Resources");
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