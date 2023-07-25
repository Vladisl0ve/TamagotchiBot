using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using TamagotchiBot.Database;
using TamagotchiBot.Handlers;
using TamagotchiBot.Services;
using TamagotchiBot.Services.Mongo;
using TamagotchiBot.UserExtensions;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace Telegram.Bots.Example
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateGlobalLoggerConfiguration();
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "config.json"))
            {
                Log.Warning("No config file found. Attempt to create new one...");
                CreateDefaultConfig();
                Log.Warning("New config has been created");
                Log.Fatal("Insert TokenBot and other necessary parametres to the config.json");
                Log.Warning($"Path to the config is: {AppDomain.CurrentDomain.BaseDirectory}config.json");
                return;
            }
            Log.Information("Starting host");
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var builder = new HostBuilder()
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
                      services.AddSingleton<ITelegramBotClient>(_ => new TelegramBotClient(context.Configuration["TokenBot"]));

                      services.Configure<TamagotchiDatabaseSettings>(context.Configuration.GetSection(nameof(TamagotchiDatabaseSettings)));
                      services.AddSingleton<ITamagotchiDatabaseSettings>(sp => sp.GetRequiredService<IOptions<TamagotchiDatabaseSettings>>().Value);

                      services.AddSingleton<NotifyTimerService>();
                      services.Configure<EnvsSettings>(context.Configuration.GetSection(nameof(EnvsSettings)));
                      services.AddSingleton<IEnvsSettings>(sp => sp.GetRequiredService<IOptions<EnvsSettings>>().Value);
                      services.AddSingleton<UserService>();
                      services.AddSingleton<PetService>();
                      services.AddSingleton<ChatService>();
                      services.AddSingleton<SInfoService>();
                      services.AddSingleton<AppleGameDataService>();
                      services.AddSingleton<BotControlService>();
                      services.AddSingleton<AllUsersDataService>();
                      services.AddSingleton<DailyInfoService>();
                      services.AddSingleton<BannedUsersService>();


                      services.AddLocalization(options => options.ResourcesPath = "Resources");
                  });

            return builder;
        }
        public static void CreateGlobalLoggerConfiguration()
        {
            string pathToLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                            "Logs",
                                            DateTime.Now.ToString("yyyy"),
                                            DateTime.Now.ToString("MM"),
                                            $"{DateTime.Now:dd}-.txt");

            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .WriteTo.File(pathToLog,
                          rollingInterval: RollingInterval.Day,
                          retainedFileCountLimit: null,
                          outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Console()
            .CreateLogger();

            Log.Warning("Path to logs: " + pathToLog);
        }

        private static void CreateDefaultConfig()
        {
            GlobalConfig globalConfig = new GlobalConfig()
            {
                TokenBot = "",

                TamagotchiDatabaseSettings = new TamagotchiDatabaseSettings()
                {
                    AllUsersDataCollectionName = "AllUsersData",
                    AppleGameDataCollectionName = "AppleGame",
                    ChatsCollectionName = "Chats",
                    BannedUsersCollectionName = "BannedUsers",
                    DailyInfoCollectionName = "DailyInfo",
                    PetsCollectionName = "Pets",
                    ServiceInfoCollectionName = "ServiceInfo",
                    UsersCollectionName = "Users",

                    ConnectionString = "",
                    DatabaseName = "TamagotchiDb"
                },

                EnvsSettings = new EnvsSettings()
                {
                    AlwaysNotifyUsers  = new List<string>(),
                    ChatsToDevNotify = new List<string>(),
                    BannedRenamingUsers = new List<string>(),
                    NotifyEvery = TimeSpan.FromHours(1),
                    DevNotifyEvery = TimeSpan.FromMinutes(1),
                    DevExtraNotifyEvery = TimeSpan.FromMinutes(5),
                    TriggersEvery  = TimeSpan.FromSeconds(20),
                    AwakeWhenAFKFor = TimeSpan.FromMinutes(30)
                },
            };

            var configJSON = JsonConvert.SerializeObject(globalConfig);

            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "config.json", configJSON);
        }
    }
}