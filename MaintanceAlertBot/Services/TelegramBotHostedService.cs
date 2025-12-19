using Microsoft.Extensions.Hosting;
using Serilog;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace MaintanceAlertBot.Services
{
    public class TelegramBotHostedService : BackgroundService
    {
        private readonly ITelegramBotClient _client;
        private readonly IUpdateHandler _updateHandler;

        public TelegramBotHostedService(ITelegramBotClient telegramBotClient,
                                        IUpdateHandler updateHandler)
        {
            _client = telegramBotClient;
            _updateHandler = updateHandler;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Log.Information("Telegram Bot Hosted Service started");

            _client.StartReceiving(
                updateHandler: _updateHandler,
                cancellationToken: stoppingToken
                );
            // Keep hosted service alive while receiving messages
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}
