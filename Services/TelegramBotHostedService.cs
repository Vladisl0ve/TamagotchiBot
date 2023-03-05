using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Serilog;
using TamagotchiBot.Handlers;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;

namespace TamagotchiBot.Services
{
    public class TelegramBotHostedService : BackgroundService
    {
        private readonly ITelegramBotClient _client;
        private readonly IUpdateHandler _updateHandler;

        public TelegramBotHostedService(ITelegramBotClient telegramBotClient, IUpdateHandler updateHandler)
        {
            _client = telegramBotClient;
            _updateHandler = updateHandler;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Log.Information("Telegram Bot Hosted Service started");

            _client.StartReceiving(_updateHandler, stoppingToken);

            // Keep hosted service alive while receiving messages
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        
    }
}
