using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Answers;
using TamagotchiBot.Models.Mongo;
using TamagotchiBot.Services.Interfaces;
using TamagotchiBot.UserExtensions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using static TamagotchiBot.UserExtensions.Constants;

namespace TamagotchiBot.Services
{
    public class NotifyTimerService
    {
        private Timer _notifyTimer;
        private Timer _changelogsTimer;
        private Timer _mpDuelsTimer;
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
        public void SetMPDuelsCheckingTimer()
        {
            TimeSpan timeToWait = TimeSpan.FromSeconds(70);
            Log.Information("MP duels timer set to wait for " + timeToWait.TotalSeconds + "s");
            _mpDuelsTimer = new Timer(timeToWait);
            _mpDuelsTimer.Elapsed += OnMPDuelsTimedEvent;
            _mpDuelsTimer.AutoReset = true;
            _mpDuelsTimer.Enabled = true;
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

        private async void OnRandomEventTimedEvent(object sender, ElapsedEventArgs e)
        {
#if !DEBUG_NOTIFY
            if (DateTime.UtcNow.Hour < 4 || DateTime.UtcNow.Hour > 20)
            {
                Log.Information($"RandomEventNotification - Sleep time for [20:00 - 04:00] UTC");
                return;
            }
#endif
            var usersToNotify = UpdateAllRandomEventUsersIds();
            var counter = 0;
            Log.Information($"RandomEventNotification - {usersToNotify.Count} users");
            foreach (var userId in usersToNotify)
            {
                var user = _appServices.UserService.Get(userId);
                Resources.Resources.Culture = new CultureInfo(user?.Culture ?? "ru");

                if (user == null)
                    continue;

                DoRandomEvent(user);

                counter++;
                if (counter % 50 == 0)
                    await Task.Delay(1000);

                Log.Information($"Sent RandomEventNotification to {Extensions.GetLogUser(user)}");
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
                var user = _appServices.UserService.Get(userId);

                try
                {
                    Resources.Resources.Culture = new CultureInfo(user.Culture);
                    var toSend = new AnswerMessage()
                    {
                        Text = Resources.Resources.rewardNotification,
                        StickerId = GetRandomDailyRewardSticker(),
                    };

                    await _appServices.BotControlService.SendAnswerMessageAsync(toSend, userId, false);

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
            foreach (var pet in pets)
            {
                _appServices.UserService.Remove(pet.UserId);
                _appServices.PetService.Remove(pet.UserId);
                counter++;
            }
            Log.Information($"Removed {counter} users");
        }
        private void OnMaintainEvent(object sender, ElapsedEventArgs e)
        {
            Log.Information($"MAINTAINS STARTED");
            _appServices.SInfoService.DisableMaintainWorks();

            //Change exps for users
            var activePets = _appServices.PetService.GetAll();
            int counterUser = 0;
            foreach (var pet in activePets)
            {
                counterUser++;
                var petResult = Pet.Clone(pet);                

                decimal toAddExp = petResult.Level * 100 + petResult.EXP;
                petResult.Level = 1;
                petResult.EXP = 1;

                while (toAddExp > 0)
                {
                    if (toAddExp < Factors.ExpToLvl * petResult.Level)
                    {
                        petResult.EXP += (int)toAddExp;
                        break;
                    }
                    else
                    {
                        toAddExp -= Factors.ExpToLvl * petResult.Level;
                        petResult.Level++;
                    }
                }

                if (petResult.EXP > Factors.ExpToLvl * petResult.Level)
                {
                    petResult.Level += petResult.EXP / Factors.ExpToLvl;
                    petResult.EXP %= Factors.ExpToLvl;
                }
                _appServices.PetService.Update(pet.UserId, petResult);
            }

            Log.Information($"MAINTAINS OVER - updated {counterUser} users");
        }
        private async void OnMPDuelsTimedEvent(object sender, ElapsedEventArgs e)
        {
            var activeDuelMetaUsers = GetAllActiveDuels();
            Log.Information($"Active Duels MP timer started - {activeDuelMetaUsers.Count} users");

            int counterDuelsEnded = 0;
            TimeSpan duelLifeTime;

            duelLifeTime = new Constants.TimesToWait().DuelCDToWait; //5 min life

            foreach (var metaUser in activeDuelMetaUsers)
            {
                if (metaUser.DuelStartTime + duelLifeTime < DateTime.UtcNow)
                {
                    var petDB = _appServices.PetService.Get(metaUser.UserId);
                    var userDB = _appServices.UserService.Get(metaUser.UserId);
                    var userLink = Extensions.GetPersonalLink(metaUser.UserId, userDB?.FirstName ?? "0_o");
                    var petNameEncoded = HttpUtility.HtmlEncode(petDB?.Name ?? "^_^");
                    Resources.Resources.Culture = new CultureInfo(userDB?.Culture ?? "ru");

                    string textToSend = string.Format(Resources.Resources.DuelMPTimeout, userLink, petNameEncoded, Constants.Costs.Duel);
                    await _appServices.BotControlService.EditMessageTextAsync(metaUser.ChatDuelId, metaUser.MsgDuelId, textToSend, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
                    await _appServices.BotControlService.DeleteMessageAsync(metaUser.ChatDuelId, metaUser.MsgCreatorDuelId, false);
                    _appServices.UserService.UpdateGold(metaUser.UserId, userDB.Gold + Constants.Costs.Duel);
                    _appServices.MetaUserService.UpdateChatDuelId(metaUser.UserId, -1);
                    _appServices.MetaUserService.UpdateMsgDuelId(metaUser.UserId, -1);
                    _appServices.MetaUserService.UpdateMsgCreatorDuelId(metaUser.UserId, -1);
                    counterDuelsEnded++;
                }
            }

            Log.Information($"Active Duels MP timer ended - {counterDuelsEnded} duels closed");
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

                if (petDB == null || petDB.Name == null)
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
                        StickerId = Constants.StickersId.ChangelogSticker,
                        ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
                    };

                    await _appServices.BotControlService.SendAnswerMessageAsync(toSend, userDB.UserId, false);

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
                if (!long.TryParse(chatId, out long parsedChatId))
                    continue;

                try
                {
                    await _appServices.BotControlService.SendTextMessageAsync(parsedChatId, $"Tamagotchi is alive! {DateTime.UtcNow:g}UTC", toLog: false);

                    var dailyInfoToday = _appServices.DailyInfoService.GetToday();

                    if (dailyInfoToday == null ||
                        (dailyInfoToday != null && (DateTime.UtcNow - dailyInfoToday.DateInfo) > _envs.DevExtraNotifyEvery))
                    {
                        Log.Information("Sent extra dev notify");
                        await _appServices.BotControlService.SendTextMessageAsync(parsedChatId, ToSendExtraDevNotify(), toLog: false);
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
            var petsDB = _appServices.PetService.GetAll().Where(p => p.Name != null && !p.IsGone);

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

        private List<User> GetAllActiveUsersIds() => _appServices.UserService.GetAll().ToList();
        private List<MetaUser> GetAllActiveDuels() => _appServices.MetaUserService.GetAll().Where(mu => mu.MsgDuelId > 0).ToList();
        private List<Pet> GetAllPetsWithoutName() => _appServices.PetService.GetAll().Where(p => p.Name == null).ToList();
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
                case 5:
                    RandomEventNiceFlower(user);
                    break;
                case 6:
                    RandomEventWatermelon(user);
                    break;
                case 7:
                    RandomEventPlayComputerGames(user);
                    break;
                default:
                    RandomEventNotify(user);
                    break;
            }
        }

        #region RandomEvents
        private async void RandomEventStomachache(Models.Mongo.User user)
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
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        private async void RandomEventRaindow(Models.Mongo.User user)
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
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        private async void RandomEventFriendMet(Models.Mongo.User user)
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
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        private async void RandomEventHotdog(Models.Mongo.User user)
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
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        private async void RandomEventNotify(Models.Mongo.User user)
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
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        private async void RandomEventStepOnFoot(Models.Mongo.User user)
        {
            var petDB = _appServices.PetService.Get(user.UserId);
            var newSatiety = petDB.Satiety - 10;
            int newHP = petDB.HP - 1;

            _appServices.PetService.UpdateSatiety(user.UserId, newSatiety, true);
            _appServices.PetService.UpdateHP(user.UserId, newHP);

            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            Resources.Resources.Culture = new CultureInfo(user?.Culture ?? "ru");
            var toSend = new AnswerMessage()
            {
                StickerId = Constants.StickersId.RandomEventStepOnFoot,
                Text = Resources.Resources.RandomEventStepOnFoot
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }
        private async void RandomEventNiceFlower(Models.Mongo.User user)
        {
            var petDB = _appServices.PetService.Get(user.UserId);
            var newJoy = petDB.Joy + 10;

            _appServices.PetService.UpdateJoy(user.UserId, newJoy, true);

            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            Resources.Resources.Culture = new CultureInfo(user?.Culture ?? "ru");
            var toSend = new AnswerMessage()
            {
                StickerId = Constants.StickersId.RandomEventNiceFlower,
                Text = Resources.Resources.RandomEventNiceFlower
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }
        private async void RandomEventWatermelon(Models.Mongo.User user)
        {
            var petDB = _appServices.PetService.Get(user.UserId);
            var newSatiety = petDB.Satiety + 15;

            _appServices.PetService.UpdateSatiety(user.UserId, newSatiety, true);

            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            Resources.Resources.Culture = new CultureInfo(user?.Culture ?? "ru");
            var toSend = new AnswerMessage()
            {
                StickerId = Constants.StickersId.RandomEventWatermelon,
                Text = Resources.Resources.RandomEventWatermelon
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }
        private async void RandomEventPlayComputerGames(Models.Mongo.User user)
        {
            var petDB = _appServices.PetService.Get(user.UserId);
            var newJoy = petDB.Joy + 30;

            _appServices.PetService.UpdateJoy(user.UserId, newJoy, true);

            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            Resources.Resources.Culture = new CultureInfo(user?.Culture ?? "ru");
            var toSend = new AnswerMessage()
            {
                StickerId = Constants.StickersId.RandomEventPlayComputerGames,
                Text = Resources.Resources.RandomEventPlayComputerGames
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        #endregion
    }
}
