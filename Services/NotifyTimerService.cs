using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using TamagotchiBot.Database;
using TamagotchiBot.Services.Mongo;
using TamagotchiBot.UserExtensions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace TamagotchiBot.Services
{
    public class NotifyTimerService
    {
        private Timer _notifyTimer;
        private Timer _changelogsTimer;
        private Timer _dailyRewardTimer;

        private readonly ITelegramBotClient _botClient;
        private PetService _petService;
        private UserService _userService;
        private ChatService _chatService;
        private SInfoService _sinfoService;
        private DailyInfoService _dailyInfoService;
        private AllUsersDataService _allUsersDataService;
        private IEnvsSettings _envs;

        private DateTime _nextNotify = DateTime.MaxValue;
        private DateTime _nextDevNotify = DateTime.MaxValue;
        private TimeSpan _notifyEvery = TimeSpan.MaxValue;
        private TimeSpan _notifyDevEvery = TimeSpan.MaxValue;
        private TimeSpan _triggerNTEvery = TimeSpan.MaxValue;
        public NotifyTimerService(ITelegramBotClient telegramBotClient,
                                  IEnvsSettings envs,
                                  PetService petService,
                                  UserService userService,
                                  ChatService chatService,
                                  SInfoService sinfoService,
                                  AllUsersDataService allUsersDataService,
                                  DailyInfoService dailyInfoService)
        {
            _botClient = telegramBotClient;
            _petService = petService;
            _userService = userService;
            _chatService = chatService;
            _sinfoService = sinfoService;
            _allUsersDataService = allUsersDataService;
            _envs = envs;

            _nextNotify = _sinfoService.GetNextNotify();
            _nextDevNotify = _sinfoService.GetNextDevNotify();
            _dailyInfoService = dailyInfoService;
        }

        public void SetNotifyTimer(TimeSpan timeToTrigger = default, TimeSpan timeToNotify = default, TimeSpan timeToDevNotify = default)
        {
            _notifyEvery = timeToNotify == default ? _envs.NotifyEvery : timeToNotify;
            _notifyDevEvery = timeToDevNotify == default ? _envs.DevNotifyEvery : timeToDevNotify;
            _triggerNTEvery = timeToTrigger == default ? _envs.TriggersEvery : timeToTrigger;
            Log.Information("Triggers every " + _triggerNTEvery.TotalSeconds + "s");
            Log.Information("DevNotify timer set to wait for " + _notifyDevEvery.TotalSeconds + "s");
            Log.Information($"Next 'wake up' notification: {_nextNotify} UTC || remaining {_nextNotify - DateTime.UtcNow:hh\\:mm\\:ss}");

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

        public void SetDailyRewardNotificationTimer()
        {
            TimeSpan timeToWait = TimeSpan.FromMinutes(3);
            Log.Information("DailyRewardNotification timer set to wait for " + timeToWait.TotalSeconds + "s");
            _dailyRewardTimer = new Timer(timeToWait);
            _dailyRewardTimer.Elapsed += OnDailyRewardTimedEvent;
            _dailyRewardTimer.AutoReset = true;
            _dailyRewardTimer.Enabled = true;
        }

        private async void OnDailyRewardTimedEvent(object sender, ElapsedEventArgs e)
        {
            var usersToNotify = GetAllDailyRewardUsersIds();
            Log.Information($"DailyRewardNotification timer - {usersToNotify.Count} users");
            foreach (var userId in usersToNotify)
            {
                var user = _userService.Get(userId);
                Resources.Resources.Culture = new CultureInfo(user?.Culture ?? "ru");

                try
                {
                    await _botClient.SendStickerAsync(userId, GetRandomDailyRewardSticker());
                    await _botClient.SendTextMessageAsync(userId, Resources.Resources.rewardNotification);

                    Log.Information($"Sent DailyRewardNotification to '@{user.Username}'");
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
        private async void OnChangelogsTimedEvent(object sender, ElapsedEventArgs e)
        {
            var usersToNotify = GetAllActiveUsersIds();
            Log.Information($"Changelog timer - {usersToNotify.Count} users");
            _sinfoService.DisableChangelogsSending();
            foreach (var userId in usersToNotify)
            {
                var user = _userService.Get(long.Parse(userId));
                Resources.Resources.Culture = new CultureInfo(user?.Culture ?? "ru");

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

            _nextNotify = DateTime.UtcNow + _notifyEvery;
            _sinfoService.UpdateNextNotify(_nextNotify);

            var usersToNotify = GetUserIdToNotify();
            if (usersToNotify != null)
            {
                Log.Information($"Notify timer - {usersToNotify.Count} users");
                foreach (var userId in usersToNotify)
                {
                    var user = _userService.Get(long.Parse(userId));
                    Resources.Resources.Culture = new CultureInfo(user?.Culture ?? "ru");
                    int rand = new Random().Next(3);
                    var notifyText = new List<string>()
                {
                    Resources.Resources.ReminderNotifyText1,
                    Resources.Resources.ReminderNotifyText2,
                    Resources.Resources.ReminderNotifyText3
                };

                    string toSendText = notifyText.ElementAtOrDefault(rand) ?? Resources.Resources.ReminderNotifyText1;
                    try
                    {
                        await Task.Delay(5000); //pause between notification to avoid 429 error
                        await _botClient.SendStickerAsync(userId, Constants.StickersId.PetBored_Cat);
                        await _botClient.SendTextMessageAsync(userId, toSendText);

                        Log.Information($"Sent reminder to '@{user?.Username ?? userId}'");
                    }
                    catch (ApiRequestException ex)
                    {
                        if (ex.ErrorCode == 403) //Forbidden by user
                        {
                            Log.Warning($"{ex.Message} @{user?.Username ?? userId}, id: {userId}");

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

            Log.Information("Notifications completed!");
            Log.Information($"Next notification: {_nextNotify} UTC || remaining {_notifyEvery:c}");
        }
        private async void SendDevNotify()
        {
            if (_envs?.ChatsToDevNotify == null)
            {
                Log.Warning("No chats do DEV notify");
                return;
            }

            var chatsToNotify = new List<string>(_envs.ChatsToDevNotify);

            foreach (var chatId in chatsToNotify)
            {
                try
                {
                    await _botClient.SendTextMessageAsync(chatId, $"Tamagotchi is alive! {DateTime.UtcNow:g}UTC");

                    var dailyInfoToday = _dailyInfoService.GetToday();

                    if (dailyInfoToday == null ||
                        (dailyInfoToday != null && (DateTime.UtcNow - dailyInfoToday.DateInfo) > _envs.DevExtraNotifyEvery))
                    {
                        Log.Information("Sent extra dev notify");
                        await _botClient.SendTextMessageAsync(chatId, ToSendExtraDevNotify());
                    }
                }
                catch (ApiRequestException ex)
                {
                    if (ex.ErrorCode == 403) //Forbidden by user
                    {
                        Log.Warning($"{ex.Message}, id: {chatId}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                }
            }

        }
        private string ToSendExtraDevNotify()
        {
            int playedUsersToday = _allUsersDataService.GetAll().Count(p => p.Updated.Date == DateTime.UtcNow.Date);

            var dailyInfoDB = _dailyInfoService.GetToday() ?? _dailyInfoService.CreateDefault();
            long messagesSent = _allUsersDataService.GetAll().Select(u => u.MessageCounter).Sum();
            long messagesSentToday = messagesSent - (_dailyInfoService.GetAll().Where(u => u.DateInfo.Date <= DateTime.UtcNow.AddDays(-1).Date).OrderByDescending(i => i.DateInfo).FirstOrDefault()?.MessagesSent ?? 0);
            long callbacksSent = _allUsersDataService.GetAll().Select(u => u.CallbacksCounter).Sum();
            long callbacksSentToday = callbacksSent - (_dailyInfoService.GetAll().Where(u => u.DateInfo.Date <= DateTime.UtcNow.AddDays(-1).Date).OrderByDescending(i => i.DateInfo).FirstOrDefault()?.CallbacksSent ?? 0);

            dailyInfoDB.UsersPlayed = playedUsersToday;
            dailyInfoDB.MessagesSent = messagesSent;
            dailyInfoDB.CallbacksSent = callbacksSent;
            dailyInfoDB.DateInfo = DateTime.UtcNow;
            dailyInfoDB.TodayCallbacks = (int)callbacksSentToday;
            dailyInfoDB.TodayMessages = (int)messagesSentToday;

            _dailyInfoService.UpdateOrCreate(dailyInfoDB);
            string text = $"{dailyInfoDB.DateInfo:G}" + Environment.NewLine
                        + $"Played   users  : {playedUsersToday}" + Environment.NewLine
                        + $"Messages today: {messagesSentToday}" + Environment.NewLine
                        + $"Callbacks today: {callbacksSentToday}" + Environment.NewLine
                        + $"------------------------" + Environment.NewLine
                        + $"Messages sent: {messagesSent}" + Environment.NewLine
                        + $"Callbacks sent  : {callbacksSent}";

            return text;
        }
        private List<string> GetUserIdToNotify()
        {
            List<string> usersToNotify = new();
            var petsDB = _petService.GetAll();

            foreach (var pet in petsDB)
            {
                var spentTime = DateTime.UtcNow - pet.LastUpdateTime;
                if (spentTime > _envs.AwakeWhenAFKFor)
                    usersToNotify.Add(pet.UserId.ToString());
            }

            if (_envs.AlwaysNotifyUsers == null)
            {
                Log.Warning("No always notify persons");
                return null;
            }

            foreach (var userAlwaysNotify in _envs.AlwaysNotifyUsers)
                if (!usersToNotify.Contains(userAlwaysNotify))
                    usersToNotify.Add(userAlwaysNotify);

            return usersToNotify;
        }
        private List<long> GetAllDailyRewardUsersIds()
        {
            List<long> usersToNotify = new();
            var petsDB = _petService.GetAll();

            foreach (var pet in petsDB)
            {
                var user = _userService.Get(pet.UserId);

                if (user.NextDailyRewardNotificationTime < DateTime.UtcNow && pet.GotDailyRewardTime.AddDays(1) < DateTime.UtcNow)
                {
                    _userService.UpdateNextDailyRewardNotificationTime(user.UserId, DateTime.UtcNow.AddDays(1));
                    usersToNotify.Add(user.UserId);
                }
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

        private string GetRandomDailyRewardSticker()
        {
            var random = new Random().Next(0, 6);

            return random switch
            {
                1 => Constants.StickersId.DailyRewardNotificationSticker_1,
                2 => Constants.StickersId.DailyRewardNotificationSticker_2,
                3 => Constants.StickersId.DailyRewardNotificationSticker_3,
                4 => Constants.StickersId.DailyRewardNotificationSticker_4,
                5 => Constants.StickersId.DailyRewardNotificationSticker_5,
                _ => Constants.StickersId.DailyRewardNotificationSticker_3,
            };
        }
    }
}
