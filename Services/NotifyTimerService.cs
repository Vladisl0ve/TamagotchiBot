using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Timers;
using TamagotchiBot.UserExtensions;
using Telegram.Bot;

namespace TamagotchiBot.Services
{
    public class NotifyTimerService
    {
        private Timer _notifyTimer;
        private readonly ITelegramBotClient _botClient;
        private PetService _petService;
        private UserService _userService;
        public NotifyTimerService(ITelegramBotClient telegramBotClient, PetService petService, UserService userService)
        {
            _botClient = telegramBotClient;
            _petService = petService;
            _userService = userService;
        }

        public void SetNotifyTimer(TimeSpan timerSpan)
        {
            TimeSpan timeToWait = timerSpan;
            Log.Information("Timer set to wait for " + timeToWait.TotalSeconds + "s");
            _notifyTimer = new Timer(timeToWait);
            _notifyTimer.Elapsed += OnNotifyTimedEvent;
            _notifyTimer.AutoReset = true;
            _notifyTimer.Enabled = true;
        }

        private async void OnNotifyTimedEvent(object sender, ElapsedEventArgs e)
        {
            try
            {
                var usersToNotify = GetUserIdToNotify();
                foreach (var userId in usersToNotify)
                {
                    var user = _userService.Get(long.Parse(userId));
                    Resources.Resources.Culture = new CultureInfo(user?.Culture ?? "en");
                    await _botClient.SendStickerAsync(userId, Constants.StickersId.PetBored_Cat);
                    await _botClient.SendTextMessageAsync(userId, Resources.Resources.ReminderNotifyText);
                    Log.Information($"Sent reminder to '@{user.Username}'");
                }

            }
            catch (Exception ex)
            {
                Log.Warning(ex.Message);
            }
        }

        private List<string> GetUserIdToNotify()
        {
            List<string> usersToNotify = new();
            var petsDB = _petService.GetAll();

            foreach (var pet in petsDB)
            {
                var spentTime = DateTime.UtcNow - pet.LastUpdateTime;
                if (spentTime > TimeSpan.FromHours(6)) //every 6 hours
                    usersToNotify.Add(pet.UserId.ToString());
            }

            return usersToNotify;
        }
    }
}
