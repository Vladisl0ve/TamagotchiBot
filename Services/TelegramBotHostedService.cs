using Microsoft.Extensions.Hosting;
using Serilog;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace TamagotchiBot.Services
{
    public class TelegramBotHostedService : BackgroundService
    {
        private readonly ITelegramBotClient _client;
        private readonly IUpdateHandler _updateHandler;
        private readonly NotifyTimerService _timerService;

        public TelegramBotHostedService(ITelegramBotClient telegramBotClient,
                                        IUpdateHandler updateHandler,
                                        NotifyTimerService notifyTimerService)
        {
            _client = telegramBotClient;
            _updateHandler = updateHandler;
            _timerService = notifyTimerService;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
#if DEBUG
            Log.Information("DEBUG: Telegram Bot Hosted Service started");
#else
            Log.Information("RELEASE: Telegram Bot Hosted Service started");
#endif
            _timerService.SetNotifyTimer();
            _timerService.SetChangelogsTimer();
            _timerService.SetDailyRewardNotificationTimer();
            _timerService.SetRandomEventNotificationTimer();
            _client.StartReceiving(
                updateHandler: _updateHandler,
                cancellationToken: stoppingToken
                );
            // Keep hosted service alive while receiving messages
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}
