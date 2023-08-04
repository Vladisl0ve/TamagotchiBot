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
        private readonly AppleGameDataService _appleGameService;
        private readonly BotControlService _botControlService;
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
                                  AppleGameDataService appleGameDataService,
                                  BotControlService botControlService,
                                  DailyInfoService dailyInfoService)
        {
            _botClient = telegramBotClient;
            _petService = petService;
            _userService = userService;
            _chatService = chatService;
            _sinfoService = sinfoService;
            _allUsersDataService = allUsersDataService;
            _botControlService = botControlService;
            this._appleGameService = appleGameDataService;
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
        public void SetRandomEventNotificationTimer()
        {
            TimeSpan timeToWait = TimeSpan.FromSeconds(90);
            Log.Information("RandomEventNotification timer set to wait for " + timeToWait.TotalSeconds + "s");
            _dailyRewardTimer = new Timer(timeToWait);
            _dailyRewardTimer.Elapsed += OnRandomEventTimedEvent;
            _dailyRewardTimer.AutoReset = true;
            _dailyRewardTimer.Enabled = true;
        }


        private void OnRandomEventTimedEvent(object sender, ElapsedEventArgs e)
        {
            var usersToNotify = UpdateAllRandomEventUsersIds();
            Log.Information($"RandomEventNotification - {usersToNotify.Count} users");
            foreach (var userId in usersToNotify)
            {
                var user = _userService.Get(userId);
                Resources.Resources.Culture = new CultureInfo(user?.Culture ?? "ru");

                DoRandomEvent(user);

                Log.Information($"Sent RandomEventNotification to '@{user?.Username}'");
            }
            if (usersToNotify.Count > 1)
                Log.Information($"RandomEventNotification timer completed - {usersToNotify.Count} users");
        }
        private async void OnDailyRewardTimedEvent(object sender, ElapsedEventArgs e)
        {
            var usersToNotify = UpdateAllDailyRewardUsersIds();
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
        private void LittileThing()
        {
            var counter = 0; 
            var auid = GetAllUsersIds();
            Log.Verbose($"Started Little Things");
            foreach (var userId in auid)
            {
                if (!long.TryParse(userId, out long userIdDB))
                    continue;

                if (_petService.Get(userIdDB) != null)
                    continue;

                _chatService.Remove(userIdDB);
                _petService.Remove(userIdDB);
                _userService.Remove(userIdDB);
                _appleGameService.Delete(userIdDB);
                counter++;
            }

            Log.Verbose($"DB: Removed {counter} users!");
        }
        private async void OnChangelogsTimedEvent(object sender, ElapsedEventArgs e)
        {
            LittileThing();
            var usersToNotify = GetAllUsersIds();
            Log.Information($"Changelog timer - {usersToNotify.Count} users");
            _sinfoService.DisableChangelogsSending();

            int usersSuccess = 0;
            foreach (var userId in usersToNotify)
            {
                var user = _userService.Get(long.Parse(userId));
                Resources.Resources.Culture = new CultureInfo(user?.Culture ?? "ru");

                try
                {
                    await _botClient.SendStickerAsync(userId, Constants.StickersId.ChangelogSticker);
                    await _botClient.SendTextMessageAsync(userId, Resources.Resources.changelog1Text);
                    await Task.Delay(500);

                    usersSuccess++;
                    Log.Information($"Sent changelog to '@{user?.Username ?? "DELETED"}'");
                }
                catch (ApiRequestException ex)
                {
                    if (ex == null)
                    {
                        Log.Error("HZ: ApiRequestException is null 0_o");
                        continue;
                    }

                    if (ex.ErrorCode == 403) //Forbidden by user
                    {
                        if (ex == null)
                        {
                            Log.Error("HZ: ApiRequestException is null 0_o");
                            continue;
                        }

                        Log.Warning($"{ex?.Message} @{user?.Username}, id: {user?.UserId}");

                        //remove all data about user

                        if (user == null)
                            continue;

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

            Log.Information($"Changelogs have been sent - {usersSuccess} success, {usersToNotify.Count - usersSuccess} failed");
        }
        private void OnNotifyTimedEvent(object sender, ElapsedEventArgs e)
        {
            _notifyTimer = new Timer(_triggerNTEvery);

            DateTime nextDevNotifyDB = _sinfoService.GetNextDevNotify();
            if (nextDevNotifyDB < DateTime.UtcNow)
            {
                SendDevNotify();

                _nextDevNotify = DateTime.UtcNow + _notifyDevEvery;
                _sinfoService.UpdateNextDevNotify(_nextDevNotify);
            }
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns>UserId of each updated user</returns>
        private List<long> UpdateAllRandomEventUsersIds()
        {
            List<long> usersToNotify = new();
            var petsDB = _petService.GetAll().Where(p => p.Name != null);

            foreach (var pet in petsDB)
            {
                var user = _userService.Get(pet.UserId);

                if (user.NextRandomEventNotificationTime < DateTime.UtcNow)
                {
                    int minutesToAdd = new Random().Next(-15, 30);

                    //For TEST
                    //_userService.UpdateNextRandomEventNotificationTime(user.UserId, DateTime.UtcNow.AddSeconds(5));

                    _userService.UpdateNextRandomEventNotificationTime(user.UserId, DateTime.UtcNow.AddHours(2).AddMinutes(minutesToAdd));
                    usersToNotify.Add(user.UserId);
                }
            }
            return usersToNotify;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>UserId of each updated user</returns>
        private List<long> UpdateAllDailyRewardUsersIds()
        {
            List<long> usersToNotify = new();
            var petsDB = _petService.GetAll().Where(p => p.Name != null);

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

        private List<string> GetAllUsersIds()
        {
            List<string> usersToNotify = new();
            var usersAUDS = _allUsersDataService.GetAll();

            foreach (var user in usersAUDS)
                usersToNotify.Add(user.UserId.ToString());

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

        private void DoRandomEvent(Models.Mongo.User user)
        {
            Resources.Resources.Culture = new CultureInfo(user?.Culture ?? "ru");
            var random = new Random().Next(10);
            switch (random)
            {
                case 0:
                    RandomEventRaindow(user);
                    break;
                case 1:
                    RandomEventStomachache(user);
                    break;
                case 2:
                    RandomEventStepOnFoot(user);
                    break;
                case 3:
                    RandomEventFriendMet(user);
                    break;
                case 4:
                    RandomEventHotdog(user);
                    break;
                default:
                    RandomEventNotify(user);
                    break;
            }
        }

        private async void SendTextAndSticker(long chatId, string text, string sticker)
        {
            _botControlService.SendStickerAsync(chatId, sticker, toLog: false);
            await Task.Delay(50);
            _botControlService.SendTextMessageAsync(chatId, text, toLog: false);
        }

        #region RandomEvents
        private void RandomEventStomachache(Models.Mongo.User user)
        {
            var petDB = _petService.Get(user.UserId);
            var newSatiety = petDB.Satiety - 15;
            var newHP = petDB.HP - 5;

            _petService.UpdateSatiety(user.UserId, newSatiety);
            _petService.UpdateHP(user.UserId, newHP);

            _petService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);
            SendTextAndSticker(user.UserId, Resources.Resources.RandomEventStomachache, Constants.StickersId.RandomEventStomachache);
        }

        private void RandomEventRaindow(Models.Mongo.User user)
        {
            var petDB = _petService.Get(user.UserId);
            int newJoy = petDB.Joy + 10 ;
            _petService.UpdateJoy(user.UserId, newJoy);

            _petService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);
            SendTextAndSticker(user.UserId, Resources.Resources.RandomEventRainbow, Constants.StickersId.RandomEventRainbow);
        }

        private void RandomEventFriendMet(Models.Mongo.User user)
        {
            var petDB = _petService.Get(user.UserId);
            int newGold = petDB.Gold + 15 ;
            int newJoy = petDB.Joy + 40 ;

            _petService.UpdateGold(user.UserId, newGold);
            _petService.UpdateJoy(user.UserId, newJoy);

            _petService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);
            SendTextAndSticker(user.UserId, Resources.Resources.RandomEventFriendMet, Constants.StickersId.RandomEventFriendMet);
        }

        private void RandomEventHotdog(Models.Mongo.User user)
        {
            var petDB = _petService.Get(user.UserId);
            var newSatiety = petDB.Satiety + 40;
            int newGold = petDB.Gold + 20;

            _petService.UpdateSatiety(user.UserId, newSatiety);
            _petService.UpdateGold(user.UserId, newGold);

            _petService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);
            SendTextAndSticker(user.UserId, Resources.Resources.RandomEventHotdog, Constants.StickersId.RandomEventHotdog);
        }

        private void RandomEventNotify(Models.Mongo.User user)
        {
            int rand = new Random().Next(3);
            var notifyText = new List<string>()
                {
                    Resources.Resources.ReminderNotifyText1,
                    Resources.Resources.ReminderNotifyText2,
                    Resources.Resources.ReminderNotifyText3
                };

            string toSendText = notifyText.ElementAtOrDefault(rand) ?? Resources.Resources.ReminderNotifyText1;

            _petService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);
            SendTextAndSticker(user.UserId, toSendText, Constants.StickersId.PetBored_Cat);
        }

        private void RandomEventStepOnFoot(Models.Mongo.User user)
        {
            var petDB = _petService.Get(user.UserId);
            var newSatiety = petDB.Satiety - 10;
            int newHP = petDB.HP - 1;

            _petService.UpdateSatiety(user.UserId, newSatiety);
            _petService.UpdateHP(user.UserId, newHP);

            _petService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);
            SendTextAndSticker(user.UserId, Resources.Resources.RandomEventStepOnFoot, Constants.StickersId.RandomEventStepOnFoot);
        }

        #endregion

    }
}
