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
using TamagotchiBot.Services.Interfaces;
using TamagotchiBot.Services.Mongo;
using TamagotchiBot.UserExtensions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Quartz;
using TamagotchiBot.Jobs;

namespace Telegram.Bots.Example
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

                      services.Configure<TamagotchiDatabaseSettings>(context.Configuration.GetSection(nameof(TamagotchiDatabaseSettings)));
                      services.AddTransient<ITamagotchiDatabaseSettings>(sp => sp.GetRequiredService<IOptions<TamagotchiDatabaseSettings>>().Value);

                      services.AddTransient<IApplicationServices, ApplicationServices>();
                      services.AddTransient<RandomEventService>();
                      services.Configure<EnvsSettings>(context.Configuration.GetSection(nameof(EnvsSettings)));
                      services.AddTransient<IEnvsSettings>(sp => sp.GetRequiredService<IOptions<EnvsSettings>>().Value);
                      services.AddTransient<UserService>();
                      services.AddTransient<AdsProducersService>();
                      services.AddTransient<PetService>();
                      services.AddTransient<ArchiveUserInfoService>();
                      services.AddTransient<ChatService>();
                      services.AddTransient<SInfoService>();
                      services.AddTransient<AppleGameDataService>();
                      services.AddTransient<TicTacToeGameDataService>();
                      services.AddTransient<HangmanGameDataService>();
                      services.AddTransient<BotControlService>();
                      services.AddTransient<AllUsersDataService>();
                      services.AddTransient<DailyInfoService>();
                      services.AddTransient<BannedUsersService>();
                      services.AddTransient<MetaUserService>();
                      services.AddTransient<ChatsMPService>();
                      services.AddTransient<ReferalInfoService>();
                      services.AddTransient<PaymentService>();
                      services.AddTransient<BonusCodeService>();


                      services.AddLocalization(options => options.ResourcesPath = "Resources");

                      // Quartz setup
                      services.AddQuartz(q =>
                      {
                          var jobKey = new JobKey("ResetAllExpJob");
                          q.AddJob<ResetAllExpJob>(opts => opts.WithIdentity(jobKey));
                          q.AddTrigger(opts => opts
                              .ForJob(jobKey)
                              .WithIdentity("ResetAllExpJob-trigger")
                              //.WithCronSchedule("0 0 1 1 * ?")); // At 01:00 on day 1 of every month
                              //.WithCronSchedule("0 * * * * ?")); // Debug purpose: every minute
                              .WithCronSchedule("0 0 1 1 * ?"));

                          var autoFeedJobKey = new JobKey("AutoFeedJob");
                          q.AddJob<AutoFeedJob>(opts => opts.WithIdentity(autoFeedJobKey));
                          q.AddTrigger(opts => opts
                              .ForJob(autoFeedJobKey)
                              .WithIdentity("AutoFeedJob-trigger")
                              .WithCronSchedule(Constants.CronSchedule.AutoFeedCron));

                          // Daily Reward Job
                          var dailyRewardJobKey = new JobKey("DailyRewardJob");
                          q.AddJob<DailyRewardJob>(opts => opts.WithIdentity(dailyRewardJobKey));
                          q.AddTrigger(opts => opts
                              .ForJob(dailyRewardJobKey)
                              .WithIdentity("DailyRewardJob-trigger")
#if DEBUG_NOTIFY
                              .WithSimpleSchedule(x => x.WithIntervalInSeconds(30).RepeatForever())); // Frequent for debug
#else
                              .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever()));
#endif

                          // Random Event Job
                          var randomEventJobKey = new JobKey("RandomEventJob");
                          q.AddJob<RandomEventJob>(opts => opts.WithIdentity(randomEventJobKey));
                          q.AddTrigger(opts => opts
                              .ForJob(randomEventJobKey)
                              .WithIdentity("RandomEventJob-trigger")
#if DEBUG_NOTIFY
                              .WithSimpleSchedule(x => x.WithIntervalInSeconds(10).RepeatForever()));
#else
                              .WithSimpleSchedule(x => x.WithIntervalInMinutes(10).RepeatForever()));
#endif

                          // Maintenance Job
                          var maintenanceJobKey = new JobKey("MaintenanceJob");
                          q.AddJob<MaintenanceJob>(opts => opts.WithIdentity(maintenanceJobKey));
                          q.AddTrigger(opts => opts
                              .ForJob(maintenanceJobKey)
                              .WithIdentity("MaintenanceJob-trigger")
                              .WithSimpleSchedule(x => x.WithIntervalInMinutes(1).RepeatForever())); // Check every minute

                          // Changelog Job
                          var changelogJobKey = new JobKey("ChangelogJob");
                          q.AddJob<ChangelogJob>(opts => opts.WithIdentity(changelogJobKey));
                          q.AddTrigger(opts => opts
                              .ForJob(changelogJobKey)
                              .WithIdentity("ChangelogJob-trigger")
                              .WithSimpleSchedule(x => x.WithIntervalInSeconds(30).RepeatForever()));

                          // Duel Timeout Job
                          var duelTimeoutJobKey = new JobKey("DuelTimeoutJob");
                          q.AddJob<DuelTimeoutJob>(opts => opts.WithIdentity(duelTimeoutJobKey));
                          q.AddTrigger(opts => opts
                              .ForJob(duelTimeoutJobKey)
                              .WithIdentity("DuelTimeoutJob-trigger")
                              .WithSimpleSchedule(x => x.WithIntervalInSeconds(100).RepeatForever()));

                          // Dev Notify Job
                          var devNotifyJobKey = new JobKey("DevNotifyJob");
                          q.AddJob<DevNotifyJob>(opts => opts.WithIdentity(devNotifyJobKey));
                          q.AddTrigger(opts => opts
                              .ForJob(devNotifyJobKey)
                              .WithIdentity("DevNotifyJob-trigger")
                              .WithSimpleSchedule(x => x.WithIntervalInSeconds(60).RepeatForever()));

                      });
                      services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
                  })
                  .ConfigureServices(services =>
                  {
                      services.AddTransient<ITelegramBotClient>(sp =>
                      {
                          var sInfoService = sp.GetRequiredService<SInfoService>();
                          var token = sInfoService.GetBotToken();
                          return new TelegramBotClient(token);
                      });
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
            .MinimumLevel.Verbose()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Quartz", LogEventLevel.Warning)
            .WriteTo.File(pathToLog,
                          rollingInterval: RollingInterval.Day,
                          retainedFileCountLimit: null,
                          outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Console(LogEventLevel.Debug)
            .CreateLogger();

            Log.Warning("Path to logs: " + Environment.NewLine + Environment.NewLine + pathToLog);
        }

        private static void CreateDefaultConfig()
        {
            GlobalConfig globalConfig = new GlobalConfig()
            {
                TamagotchiDatabaseSettings = new TamagotchiDatabaseSettings()
                {
                    AllUsersDataCollectionName = "AllUsersData",
                    AppleGameDataCollectionName = "AppleGame",
                    ChatsCollectionName = "Chats",
                    ChatsMPCollectionName = "ChatsMP",
                    BannedUsersCollectionName = "BannedUsers",
                    DailyInfoCollectionName = "DailyInfo",
                    PetsCollectionName = "Pets",
                    ServiceInfoCollectionName = "ServiceInfo",
                    UsersCollectionName = "Users",

                    ConnectionString = "Instead of CS create next env variables: MongoUsername, MongoPass, MongoIP, MongoPort",
                    DatabaseName = "TamagotchiDb",
                    AdsProducersCollectionName = "AdsProducers",
                    MetaUsersCollectionName = "MetaUser",
                    ReferalInfoCollectionName = "ReferalInfo",
                    ArchiveUserInfoCollectionName = "ArchiveUserInfo",
                    TicTacToeGameDataCollectionName = "TicTacToeGameData",
                    HangmanGameDataCollectionName = "HangmanGameData",
                    PaymentCollectionName = "StarPayments",
                    PetsBackupCollectionName = "PetsBackup",
                    UsersBackupCollectionName = "UsersBackup",
                    BonusCodesCollectionName = "BonusCodes"
                },

                EnvsSettings = new EnvsSettings()
                {
                    AlwaysNotifyUsers = new List<string>(),
                    ChatsToDevNotify = new List<string>(),
                    BannedRenamingUsers = new List<string>(),
                    DevNotifyEvery = TimeSpan.FromMinutes(1),
                    DevExtraNotifyEvery = TimeSpan.FromMinutes(5),
                    TriggersEvery = TimeSpan.FromSeconds(20),
                    AwakeWhenAFKFor = TimeSpan.FromMinutes(30),
                    BotstatApiKey = "",
                    OpenAiApiKey = "",
                    TokenBot = ""
                },
            };

            var configJSON = JsonConvert.SerializeObject(globalConfig);

            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "config.json", configJSON);
        }
    }
}