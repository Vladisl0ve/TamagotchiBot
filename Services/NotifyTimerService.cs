using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Timers;
using TamagotchiBot.UserExtensions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace TamagotchiBot.Services
{
    public class NotifyTimerService
    {
        private Timer _notifyTimer;
        private Timer _changelogsTimer;

        private readonly ITelegramBotClient _botClient;
        private PetService _petService;
        private UserService _userService;
        private ChatService _chatService;
        private SInfoService _sinfoService;

        private DateTime _nextNotify = DateTime.MaxValue;
        private DateTime _nextDevNotify = DateTime.MaxValue;
        private TimeSpan _notifyEvery = TimeSpan.MaxValue;
        private TimeSpan _notifyDevEvery = TimeSpan.MaxValue;
        private TimeSpan _triggerNTEvery = TimeSpan.MaxValue;
        public NotifyTimerService(ITelegramBotClient telegramBotClient,
                                  PetService petService,
                                  UserService userService,
                                  ChatService chatService,
                                  SInfoService sinfoService)
        {
            _botClient = telegramBotClient;
            _petService = petService;
            _userService = userService;
            _chatService = chatService;
            _sinfoService = sinfoService;

            _nextNotify = _sinfoService.GetNextNotify();
            _nextDevNotify = _sinfoService.GetNextDevNotify();
        }

        public void SetNotifyTimer(TimeSpan timeToTrigger, TimeSpan timeToNotify, TimeSpan timeToDevNotify)
        {
            _notifyEvery = timeToNotify;
            _notifyDevEvery = timeToDevNotify;
            _triggerNTEvery = timeToTrigger;
            Log.Information("Notify timer set to wait for " + timeToTrigger.TotalSeconds + "s");
            Log.Information("DevNotify timer set to wait for " + timeToDevNotify.TotalSeconds + "s");
            Log.Information($"Next notification: {_nextNotify} UTC || remaining {_nextNotify - DateTime.UtcNow:hh\\:mm\\:ss}");

            _notifyTimer = new Timer(TimeSpan.FromSeconds(3));
            _notifyTimer.Elapsed += OnNotifyTimedEvent;
            _notifyTimer.AutoReset = true;
            _notifyTimer.Enabled = true;
        }

        public void SetChangelogsTimer()
        {
            if (!_sinfoService.GetDoSendChangelogs())
                return;

            TimeSpan timeToWait = TimeSpan.FromSeconds(10);
            Log.Information("Changelogs timer set to wait for " + timeToWait.TotalSeconds + "s");
            _changelogsTimer = new Timer(timeToWait);
            _changelogsTimer.Elapsed += OnChangelogsTimedEvent;
            _changelogsTimer.AutoReset = false;
            _changelogsTimer.Enabled = true;
        }
        private async void OnChangelogsTimedEvent(object sender, ElapsedEventArgs e)
        {
            var usersToNotify = GetAllActiveUsersIds();
            Log.Information($"Changelog timer - {usersToNotify.Count} users");
            _sinfoService.DisableChangelogsSending();
            foreach (var userId in usersToNotify)
            {
                var user = _userService.Get(long.Parse(userId));
                Resources.Resources.Culture = new CultureInfo(user?.Culture ?? "en");

                try
                {
                    await _botClient.SendStickerAsync(userId, Constants.StickersId.ChangelogSticker);
                    await _botClient.SendTextMessageAsync(userId, Resources.Resources.changelog1Text);


                    Log.Information($"Sent changelog to '@{user.Username}'");
                }
                catch (ApiRequestException ex)
                {
                    if (ex.ErrorCode == 403) //Forbidden by user
                    {
                        Log.Warning($"{ex.Message} @{user.Username}, id: {user.UserId}");

                        //remove all data about user
                        _chatService.Remove(user.UserId);
                        _petService.Remove(user.UserId);
                        _userService.Remove(user.UserId);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                }
            }

        }

        private async void OnNotifyTimedEvent(object sender, ElapsedEventArgs e)
        {
            _notifyTimer = new Timer(_triggerNTEvery);

            DateTime nextDevNotifyDB = _sinfoService.GetNextDevNotify();
            if (nextDevNotifyDB < DateTime.UtcNow)
            {
                SendDevNotify();

                _nextDevNotify = DateTime.UtcNow + _notifyDevEvery;
                _sinfoService.UpdateNextDevNotify(_nextDevNotify);
            }

            DateTime nextNotifyDB = _sinfoService.GetNextNotify();

            if (nextNotifyDB > DateTime.UtcNow)
                return;

            var usersToNotify = GetUserIdToNotify();
            Log.Information($"Notify timer - {usersToNotify.Count} users");
            foreach (var userId in usersToNotify)
            {
                var user = _userService.Get(long.Parse(userId));
                Resources.Resources.Culture = new CultureInfo(user?.Culture ?? "en");

                try
                {
                    await _botClient.SendStickerAsync(userId, Constants.StickersId.PetBored_Cat);
                    await _botClient.SendTextMessageAsync(userId, Resources.Resources.ReminderNotifyText);


                    Log.Information($"Sent reminder to '@{user.Username}'");
                }
                catch (ApiRequestException ex)
                {
                    if (ex.ErrorCode == 403) //Forbidden by user
                    {
                        Log.Warning($"{ex.Message} @{user.Username}, id: {user.UserId}");

                        //remove all data about user
                        if (user.UserId != 1297838077) //id of devs
                        {
                            _chatService.Remove(user.UserId);
                            _petService.Remove(user.UserId);
                            _userService.Remove(user.UserId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                }
            }

            _nextNotify = DateTime.UtcNow + _notifyEvery;
            _sinfoService.UpdateNextNotify(_nextNotify);
            Log.Information($"Next notification: {_nextNotify} UTC || remaining {_notifyEvery:c}");
        }

        private async void SendDevNotify()
        {
            var usersToNotify = new List<string>(){ "1297838077" };
            Log.Information($"DevNotify timer - {usersToNotify.Count} users");
            foreach (var userId in usersToNotify)
            {
                var user = _userService.Get(long.Parse(userId));
                Resources.Resources.Culture = new CultureInfo(user?.Culture ?? "en");

                try
                {
                    await _botClient.SendTextMessageAsync(userId, $"Tamagotchi is alive! {DateTime.UtcNow:g}UTC");

                    Log.Information($"Sent dev-reminder to '@{user.Username}'");
                }
                catch (ApiRequestException ex)
                {
                    if (ex.ErrorCode == 403) //Forbidden by user
                    {
                        Log.Warning($"{ex.Message} @{user.Username}, id: {user.UserId}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                }
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
        private List<string> GetAllActiveUsersIds()
        {
            List<string> usersToNotify = new();
            var petsDB = _petService.GetAll();

            foreach (var pet in petsDB)
                usersToNotify.Add(pet.UserId.ToString());

            return usersToNotify;
        }
    }
}
