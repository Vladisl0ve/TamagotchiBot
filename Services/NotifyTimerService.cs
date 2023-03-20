using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Telegram.Bot;

namespace TamagotchiBot.Services
{
    public class NotifyTimerService
    {
        private System.Timers.Timer _notifyTimer;
        private readonly ITelegramBotClient _botClient;
        public NotifyTimerService(ITelegramBotClient telegramBotClient)
        {
            _botClient = telegramBotClient;
            SetNotifyTimer();
        }

        private void SetNotifyTimer()
        {
            TimeSpan timeToWait = TimeSpan.FromSeconds(1);
            Log.Information("Timer set to wait for " + timeToWait.TotalSeconds + "s");
            _notifyTimer = new System.Timers.Timer(timeToWait);
            _notifyTimer.Elapsed += OnTimedEvent;
            _notifyTimer.AutoReset = true;
            _notifyTimer.Enabled = true;
        }

        private async void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            try
            {
               // await _botClient.SendTextMessageAsync("401250312", $"Test on: {e.SignalTime}");

            }
            catch (Exception ex)
            {
                Log.Warning(ex.Message);
            }
        }
    }
}
