using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using MaintanceAlertBot.Database;
using MaintanceAlertBot.Handlers;
using MaintanceAlertBot.Services;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace MaintanceAlertBot
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);

            CreateGlobalLoggerConfiguration();
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "config.json"))
            {
                Log.Warning("No config file found. Attempt to create new one...");
                CreateDefaultConfig();
                Log.Warning("New config has been created");
                Log.Fatal("Insert TokenBot and other necessary parametres to the config.json");
                Log.Warning($"Path to the config is: {AppDomain.CurrentDomain.BaseDirectory}config.json");
                // return; // Don't return, let it try to run or fail gracefully, but user said "Use same approach", Tamagotchi returns.
                return;
            }
            Log.Information("Starting host");
            CreateHostBuilder(args).Build().Run();
        }

        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Log.Fatal(e, "MyHandler caught!");
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return new HostBuilder()
                  .ConfigureAppConfiguration(x =>
                  {
                      var conf = new ConfigurationBuilder()
                               .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                               .AddJsonFile("config.json", false, true)
                               .Build();

                      x.AddConfiguration(conf);
                  })
                  .UseSerilog()
                  .ConfigureServices((context, services) =>
                  {
                      services.AddHostedService<TelegramBotHostedService>();
                      services.AddTransient<IUpdateHandler, UpdateHandler>();
                      services.AddTransient<TamagotchiBot.Services.Mongo.UserService>();

                      services.Configure<TamagotchiBot.Database.TamagotchiDatabaseSettings>(context.Configuration.GetSection(nameof(TamagotchiDatabaseSettings)));
                      services.AddTransient<TamagotchiBot.Database.ITamagotchiDatabaseSettings>(sp => sp.GetRequiredService<IOptions<TamagotchiBot.Database.TamagotchiDatabaseSettings>>().Value);
                      services.AddTransient<TamagotchiBot.Services.Mongo.SInfoService>();

                      services.AddTransient<ITelegramBotClient>(sp =>
                      {
                          var sInfoService = sp.GetRequiredService<TamagotchiBot.Services.Mongo.SInfoService>();
                          var token = sInfoService.GetBotToken();
                          if (string.IsNullOrEmpty(token))
                          {
                              Log.Fatal("Token is null or empty in Database!");
                              throw new ArgumentNullException(nameof(token), "Token not found in ServiceInfo");
                          }
                          return new TelegramBotClient(token);
                      });
                  });
        }
        public static void CreateGlobalLoggerConfiguration()
        {
            string pathToLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                            "Logs",
                                            DateTime.Now.ToString("yyyy"),
                                            DateTime.Now.ToString("MM"),
                                            $"{DateTime.Now:dd}-.txt");

            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .WriteTo.File(pathToLog,
                          rollingInterval: RollingInterval.Day,
                          retainedFileCountLimit: null,
                          outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Console(LogEventLevel.Debug)
            .CreateLogger();

            Log.Warning("Path to logs: " + pathToLog);
        }

        private static void CreateDefaultConfig()
        {
            // Simple anonymous object for config creation
            var globalConfig = new
            {
                TamagotchiDatabaseSettings = new TamagotchiDatabaseSettings()
                {
                    ServiceInfoCollectionName = "ServiceInfo",
                    ConnectionString = "mongodb://localhost:27017",
                    DatabaseName = "TamagotchiDb"
                }
            };

            var configJSON = JsonConvert.SerializeObject(globalConfig, Formatting.Indented);

            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "config.json", configJSON);
        }
    }
}
