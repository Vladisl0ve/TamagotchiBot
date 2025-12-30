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

        public TelegramBotHostedService(ITelegramBotClient telegramBotClient,
                                        IUpdateHandler updateHandler)
        {
            _client = telegramBotClient;
            _updateHandler = updateHandler;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
#if DEBUG
            Log.Information("DEBUG: Telegram Bot Hosted Service started");
#elif STAGING
            Log.Information("STAGING: Telegram Bot Hosted Service started");
#elif DEBUG_HOTFIX
            Log.Information("DEBUG_HOTFIX: Telegram Bot Hosted Service started");
#elif DEBUG_NOTIFY
            Log.Information("DEBUG_NOTIFY: Telegram Bot Hosted Service started");
#else
            Log.Information("RELEASE: Telegram Bot Hosted Service started");
#endif

            _client.StartReceiving(
                updateHandler: _updateHandler,
                cancellationToken: stoppingToken
                );
            // Keep hosted service alive while receiving messages
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}
