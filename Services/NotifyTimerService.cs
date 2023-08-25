using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Answers;
using TamagotchiBot.Models.Mongo;
using TamagotchiBot.Services.Interfaces;
using TamagotchiBot.UserExtensions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace TamagotchiBot.Services
{
    public class NotifyTimerService
    {
        private Timer _notifyTimer;
        private Timer _changelogsTimer;
        private Timer _MaintainWorkTimer;
        private Timer _dailyRewardTimer;
        private Timer _randomEventRewardTimer;
        private readonly IApplicationServices _appServices;

        private IEnvsSettings _envs;

        private DateTime _nextNotify = DateTime.MaxValue;
        private DateTime _nextDevNotify = DateTime.MaxValue;
        private TimeSpan _notifyEvery = TimeSpan.MaxValue;
        private TimeSpan _notifyDevEvery = TimeSpan.MaxValue;
        private TimeSpan _triggerNTEvery = TimeSpan.MaxValue;
        public NotifyTimerService(ITelegramBotClient telegramBotClient,
                                  IApplicationServices applicationServices,
                                  IEnvsSettings envs)
        {
            _appServices = applicationServices;
            _envs = envs;

            _nextNotify = _appServices.SInfoService.GetNextNotify();
            _nextDevNotify = _appServices.SInfoService.GetNextDevNotify();
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
        public void SetMaintainActions()
        {
            if (!_appServices.SInfoService.GetDoMaintainWorks())
                return;

            TimeSpan timeToWait = TimeSpan.FromSeconds(5);
            Log.Information("MAINTAIN IN SECONDS: " + timeToWait.TotalSeconds);
            _MaintainWorkTimer = new Timer(timeToWait);
            _MaintainWorkTimer.Elapsed += OnMaintainEvent;
            _MaintainWorkTimer.AutoReset = false;
            _MaintainWorkTimer.Enabled = true;
        }
        public void SetChangelogsTimer()
        {
            if (!_appServices.SInfoService.GetDoSendChangelogs())
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
#if DEBUG_NOTIFY
            TimeSpan timeToWait = TimeSpan.FromSeconds(10);
#else

            TimeSpan timeToWait = TimeSpan.FromSeconds(307);
#endif
            Log.Information("DailyRewardNotification timer set to wait for " + timeToWait.TotalSeconds + "s");
            _dailyRewardTimer = new Timer(timeToWait);
            _dailyRewardTimer.Elapsed += OnDailyRewardTimedEvent;
            _dailyRewardTimer.AutoReset = true;
            _dailyRewardTimer.Enabled = true;
        }
        public void SetRandomEventNotificationTimer()
        {
#if DEBUG_NOTIFY
            TimeSpan timeToWait = TimeSpan.FromSeconds(7);
#else
            TimeSpan timeToWait = TimeSpan.FromSeconds(60);
#endif
            Log.Information("RandomEventNotification timer set to wait for " + timeToWait.TotalSeconds + "s");
            _randomEventRewardTimer = new Timer(timeToWait);
            _randomEventRewardTimer.Elapsed += OnRandomEventTimedEvent;
            _randomEventRewardTimer.AutoReset = true;
            _randomEventRewardTimer.Enabled = true;
        }

        private void OnRandomEventTimedEvent(object sender, ElapsedEventArgs e)
        {
            var usersToNotify = UpdateAllRandomEventUsersIds();
            Log.Information($"RandomEventNotification - {usersToNotify.Count} users");
            foreach (var userId in usersToNotify)
            {
                var user = _appServices.UserService.Get(userId);
                Resources.Resources.Culture = new CultureInfo(user?.Culture ?? "ru");

                if (user == null) continue;
                DoRandomEvent(user);

                Log.Information($"Sent RandomEventNotification to {Extensions.GetLogUser(user)}");
            }
            if (usersToNotify.Count > 1)
                Log.Information($"RandomEventNotification timer completed - {usersToNotify.Count} users");
        }
        private void OnDailyRewardTimedEvent(object sender, ElapsedEventArgs e)
        {
            var usersToNotify = UpdateAllDailyRewardUsersIds();
            Log.Information($"DailyRewardNotification timer - {usersToNotify.Count} users");
            foreach (var userId in usersToNotify)
            {
                var user = _appServices.UserService.Get(userId);

                try
                {
                    Resources.Resources.Culture = new CultureInfo(user.Culture);
                    var toSend = new AnswerMessage()
                    {
                        Text = Resources.Resources.rewardNotification,
                        StickerId = GetRandomDailyRewardSticker(),
                    };

                    _appServices.BotControlService.SendAnswerMessageAsync(toSend, userId, false);

                    Log.Information($"Sent DailyRewardNotification to {Extensions.GetLogUser(user)}");
                }
                catch (ApiRequestException ex)
                {
                    if (ex.ErrorCode == 403) //Forbidden by user
                    {
                        Log.Warning($"{ex.Message} {Extensions.GetLogUser(user)}");

                        //remove all data about user
                        _appServices.ChatService.Remove(user.UserId);
                        _appServices.PetService.Remove(user.UserId);
                        _appServices.UserService.Remove(user.UserId);
                        _appServices.AppleGameDataService.Delete(user.UserId);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                }
            }
        }
        private void LittileThing(List<Pet> pets)
        {
            var counter = 0;
            foreach(var pet in pets)
            {
                _appServices.UserService.Remove(pet.UserId);
                _appServices.PetService.Remove(pet.UserId);
                counter++;
            }
            Log.Information($"Removed {counter} users");
        }
        private void OnMaintainEvent(object sender, ElapsedEventArgs e)
        {
            var petsWithoutName = GetAllPetsWithoutName();
            Log.Information($"MAINTAINS - {petsWithoutName.Count} users");
            _appServices.SInfoService.DisableMaintainWorks();

            LittileThing(petsWithoutName);
        }
        private async void OnChangelogsTimedEvent(object sender, ElapsedEventArgs e)
        {
            var usersToNotify = GetAllActiveUsersIds();
            Log.Information($"Changelog timer - {usersToNotify.Count} users");
            _appServices.SInfoService.DisableChangelogsSending();

            Log.Warning($"BD CLEANING ON CHANGELOGS STARTED");

            int usersSuccess = 0;
            int usersDeleted = 0;
            int usersForbidden = 0;
            foreach (var userDB in usersToNotify)
            {
                var petDB = _appServices.PetService.Get(userDB.UserId);

                if (petDB == null)
                {
                    _appServices.ChatService.Remove(userDB.UserId);
                    _appServices.PetService.Remove(userDB.UserId);
                    _appServices.UserService.Remove(userDB.UserId);
                    _appServices.AppleGameDataService.Delete(userDB.UserId);

                    Log.Information($"DELETED {Extensions.GetLogUser(userDB)}");
                    usersDeleted++;
                    continue;
                }

                try
                {
                    Resources.Resources.Culture = new CultureInfo(userDB?.Culture ?? "ru");
                    var toSend = new AnswerMessage()
                    {
                        Text = Resources.Resources.changelog1Text,
                        StickerId = Constants.StickersId.ChangelogSticker
                    };

                    _appServices.BotControlService.SendAnswerMessageAsync(toSend, userDB.UserId, false);

                    await Task.Delay(100);

                    usersSuccess++;
                    Log.Information($"Sent changelog to {Extensions.GetLogUser(userDB)}");
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

                        Log.Warning($"{ex?.Message} {Extensions.GetLogUser(userDB)}");

                        _appServices.ChatService.Remove(userDB.UserId);
                        _appServices.PetService.Remove(userDB.UserId);
                        _appServices.UserService.Remove(userDB.UserId);
                        _appServices.AppleGameDataService.Delete(userDB.UserId);
                        usersForbidden++;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                }
            }

            Log.Warning($"DELETED USERS:   {usersDeleted}");
            Log.Warning($"SUCESS SENT:     {usersSuccess}");
            Log.Warning($"FORBIDDEN USERS: {usersForbidden}");
            Log.Warning($"BD CLEANING IS OVER...");
            Log.Information($"Changelogs have been sent - {usersSuccess} success, {usersToNotify.Count - usersSuccess} failed");
        }
        private void OnNotifyTimedEvent(object sender, ElapsedEventArgs e)
        {
            _notifyTimer = new Timer(_triggerNTEvery);

            DateTime nextDevNotifyDB = _appServices.SInfoService.GetNextDevNotify();
            if (nextDevNotifyDB < DateTime.UtcNow)
            {
                SendDevNotify();

                _nextDevNotify = DateTime.UtcNow + _notifyDevEvery;
                _appServices.SInfoService.UpdateNextDevNotify(_nextDevNotify);
            }
        }

        private void SendDevNotify()
        {
            if (_envs?.ChatsToDevNotify == null)
            {
                Log.Warning("No chats do DEV notify");
                return;
            }

            var chatsToNotify = new List<string>(_envs.ChatsToDevNotify);

            foreach (var chatId in chatsToNotify)
            {
                if (!long.TryParse(chatId, out long parsedChatId))
                    continue;

                try
                {
                    _appServices.BotControlService.SendTextMessageAsync(parsedChatId, $"Tamagotchi is alive! {DateTime.UtcNow:g}UTC", toLog: false);

                    var dailyInfoToday = _appServices.DailyInfoService.GetToday();

                    if (dailyInfoToday == null ||
                        (dailyInfoToday != null && (DateTime.UtcNow - dailyInfoToday.DateInfo) > _envs.DevExtraNotifyEvery))
                    {
                        Log.Information("Sent extra dev notify");
                        _appServices.BotControlService.SendTextMessageAsync(parsedChatId, ToSendExtraDevNotify(), toLog: false);
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
            int playedUsersToday = _appServices.AllUsersDataService.GetAll().Count(p => p.Updated.Date == DateTime.UtcNow.Date);

            var dailyInfoDB = _appServices.DailyInfoService.GetToday() ?? _appServices.DailyInfoService.CreateDefault();
            long messagesSent = _appServices.AllUsersDataService.GetAll().Select(u => u.MessageCounter).Sum();
            long messagesSentToday = messagesSent - (_appServices.DailyInfoService.GetAll().Where(u => u.DateInfo.Date <= DateTime.UtcNow.AddDays(-1).Date).OrderByDescending(i => i.DateInfo).FirstOrDefault()?.MessagesSent ?? 0);
            long callbacksSent = _appServices.AllUsersDataService.GetAll().Select(u => u.CallbacksCounter).Sum();
            long callbacksSentToday = callbacksSent - (_appServices.DailyInfoService.GetAll().Where(u => u.DateInfo.Date <= DateTime.UtcNow.AddDays(-1).Date).OrderByDescending(i => i.DateInfo).FirstOrDefault()?.CallbacksSent ?? 0);

            dailyInfoDB.UsersPlayed = playedUsersToday;
            dailyInfoDB.MessagesSent = messagesSent;
            dailyInfoDB.CallbacksSent = callbacksSent;
            dailyInfoDB.DateInfo = DateTime.UtcNow;
            dailyInfoDB.TodayCallbacks = (int)callbacksSentToday;
            dailyInfoDB.TodayMessages = (int)messagesSentToday;

            _appServices.DailyInfoService.UpdateOrCreate(dailyInfoDB);
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
            var petsDB = _appServices.PetService.GetAll().Where(p => p.Name != null);

            foreach (var pet in petsDB)
            {
                var user = _appServices.UserService.Get(pet.UserId);

                if (pet.NextRandomEventNotificationTime < DateTime.UtcNow)
                {
                    int minutesToAdd = new Random().Next(-15, 30);

#if DEBUG_NOTIFY
                    _appServices.PetService.UpdateNextRandomEventNotificationTime(user.UserId, DateTime.UtcNow.AddSeconds(1));
#else
                    _appServices.PetService.UpdateNextRandomEventNotificationTime(user.UserId, DateTime.UtcNow.AddHours(2).AddMinutes(minutesToAdd));
#endif
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
            var petsDB = _appServices.PetService.GetAll().Where(p => p.Name != null);

            foreach (var pet in petsDB)
            {
                var user = _appServices.UserService.Get(pet.UserId);

#if DEBUG_NOTIFY
                if (user.NextDailyRewardNotificationTime < DateTime.UtcNow && user.GotDailyRewardTime.AddSeconds(1) < DateTime.UtcNow)
                {
                    _appServices.UserService.UpdateNextDailyRewardNotificationTime(user.UserId, DateTime.UtcNow.AddSeconds(1));
                    usersToNotify.Add(user.UserId);
                }
#else

                if (user.NextDailyRewardNotificationTime < DateTime.UtcNow && user.GotDailyRewardTime.AddDays(1) < DateTime.UtcNow)
                {
                    _appServices.UserService.UpdateNextDailyRewardNotificationTime(user.UserId, DateTime.UtcNow.AddDays(1));
                    usersToNotify.Add(user.UserId);
                }
#endif
            }
            return usersToNotify;
        }

        private List<Models.Mongo.User> GetAllActiveUsersIds() => _appServices.UserService.GetAll().ToList();
        private List<Models.Mongo.Pet> GetAllPetsWithoutName() => _appServices.PetService.GetAll().Where(p => p.Name == null).ToList();
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

        #region RandomEvents
        private void RandomEventStomachache(Models.Mongo.User user)
        {
            var petDB = _appServices.PetService.Get(user.UserId);
            var newSatiety = petDB.Satiety - 15;
            var newHP = petDB.HP - 5;

            _appServices.PetService.UpdateSatiety(user.UserId, newSatiety);
            _appServices.PetService.UpdateHP(user.UserId, newHP);

            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            Resources.Resources.Culture = new CultureInfo(user?.Culture ?? "ru");
            var toSend = new AnswerMessage()
            {
                StickerId = Constants.StickersId.RandomEventStomachache,
                Text = Resources.Resources.RandomEventStomachache
            };
            _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        private void RandomEventRaindow(Models.Mongo.User user)
        {
            var petDB = _appServices.PetService.Get(user.UserId);
            if (petDB == null) return;
            int newJoy = petDB.Joy + 10 ;
            _appServices.PetService.UpdateJoy(user.UserId, newJoy);

            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            Resources.Resources.Culture = new CultureInfo(user?.Culture ?? "ru");
            var toSend = new AnswerMessage()
            {
                StickerId = Constants.StickersId.RandomEventRainbow,
                Text = Resources.Resources.RandomEventRainbow
            };
            _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        private void RandomEventFriendMet(Models.Mongo.User user)
        {
            var petDB = _appServices.PetService.Get(user.UserId);
            var userDB = _appServices.UserService.Get(user.UserId);
            int newGold = userDB.Gold + 15 ;
            int newJoy = petDB.Joy + 40 ;

            _appServices.UserService.UpdateGold(user.UserId, newGold);
            _appServices.PetService.UpdateJoy(user.UserId, newJoy);

            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            Resources.Resources.Culture = new CultureInfo(user?.Culture ?? "ru");
            var toSend = new AnswerMessage()
            {
                StickerId = Constants.StickersId.RandomEventFriendMet,
                Text = Resources.Resources.RandomEventFriendMet
            };
            _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        private void RandomEventHotdog(Models.Mongo.User user)
        {
            var userDB = _appServices.UserService.Get(user.UserId);
            var petDB = _appServices.PetService.Get(user.UserId);
            var newSatiety = petDB.Satiety + 40;
            int newGold = userDB.Gold + 20;

            _appServices.PetService.UpdateSatiety(user.UserId, newSatiety);
            _appServices.UserService.UpdateGold(user.UserId, newGold);

            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            Resources.Resources.Culture = new CultureInfo(user?.Culture ?? "ru");
            var toSend = new AnswerMessage()
            {
                StickerId = Constants.StickersId.RandomEventHotdog,
                Text = Resources.Resources.RandomEventHotdog
            };
            _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
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

            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            Resources.Resources.Culture = new CultureInfo(user?.Culture ?? "ru");
            var toSend = new AnswerMessage()
            {
                StickerId = Constants.StickersId.PetBored_Cat,
                Text = toSendText
            };
            _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        private void RandomEventStepOnFoot(Models.Mongo.User user)
        {
            var petDB = _appServices.PetService.Get(user.UserId);
            var newSatiety = petDB.Satiety - 10;
            int newHP = petDB.HP - 1;

            _appServices.PetService.UpdateSatiety(user.UserId, newSatiety);
            _appServices.PetService.UpdateHP(user.UserId, newHP);

            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            Resources.Resources.Culture = new CultureInfo(user?.Culture ?? "ru");
            var toSend = new AnswerMessage()
            {
                StickerId = Constants.StickersId.RandomEventStepOnFoot,
                Text = Resources.Resources.RandomEventStepOnFoot
            };
            _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        #endregion
    }
}
