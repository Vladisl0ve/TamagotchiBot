using Microsoft.Extensions.Hosting;
using Serilog;
using System;
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

        public TelegramBotHostedService(ITelegramBotClient telegramBotClient, IUpdateHandler updateHandler, NotifyTimerService notifyTimerService)
        {
            _client = telegramBotClient;
            _updateHandler = updateHandler;
            _timerService = notifyTimerService;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Log.Information("Telegram Bot Hosted Service started");

#if DEBUG
            _timerService.SetNotifyTimer(TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(6), TimeSpan.FromSeconds(1));
#else
            _timerService.SetNotifyTimer(TimeSpan.FromMinutes(1), TimeSpan.FromHours(6), TimeSpan.FromMinutes(1));
#endif
            _timerService.SetChangelogsTimer();
            _client.StartReceiving(
                updateHandler: _updateHandler,
                cancellationToken: stoppingToken
                );
            // Keep hosted service alive while receiving messages
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }


    }
}
