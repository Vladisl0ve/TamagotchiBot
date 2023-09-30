using MongoDB.Driver.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using TamagotchiBot.Models;
using TamagotchiBot.Models.Answers;
using TamagotchiBot.Models.Mongo;
using TamagotchiBot.Services.Interfaces;
using TamagotchiBot.UserExtensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static TamagotchiBot.Resources.Resources;
using static TamagotchiBot.UserExtensions.Constants;
using Extensions = TamagotchiBot.UserExtensions.Extensions;
using User = TamagotchiBot.Models.Mongo.User;

namespace TamagotchiBot.Controllers
{
    public class MenuController
    {
        private readonly IApplicationServices _appServices;
        private readonly Message _message = null;
        private readonly CallbackQuery _callback = null;
        private readonly long _userId;
        private readonly string _userInfo;

        private MenuController(IApplicationServices services, Message message = null, CallbackQuery callback = null)
        {
            _callback = callback;
            _message = message;
            _userId = callback?.From.Id ?? message.From.Id;
            _appServices = services;
            _userInfo = Extensions.GetLogUser(_appServices.UserService.Get(_userId));

            UpdateOrCreateAUD(message?.From ?? callback.From, message, callback);
        }
        public MenuController(IApplicationServices services, CallbackQuery callback) : this(services, null, callback)
        {
        }

        public MenuController(IApplicationServices services, Message message) : this(services, message, null)
        {
        }

        private void UpdateOrCreateAUD(Telegram.Bot.Types.User user, Message message = null, CallbackQuery callback = null)
        {
            if (user == null)
            {
                Log.Warning("User is null, can not add to AUD");
                return;
            }

            var aud = _appServices.AllUsersDataService.Get(user.Id);
            if (aud == null) //if new
            {
                _appServices.AllUsersDataService.Create(new AllUsersData()
                {
                    UserId = user.Id,
                    ChatId = user.Id,
                    Created = DateTime.UtcNow,
                    Culture = Culture.ToString(),
                    Username = user.Username,
                    Updated = DateTime.UtcNow,
                    LastMessage = message?.Text ?? string.Empty,
                    MessageCounter = message == null ? 0 : 1,
                    CallbacksCounter = callback == null ? 0 : 1,

                });
                return;
            }

            aud.Updated = DateTime.UtcNow;
            aud.Username = user.Username;
            aud.Culture = _appServices.UserService.Get(user.Id)?.Culture ?? "ru";
            aud.LastMessage = message?.Text ?? string.Empty;
            aud.MessageCounter = message == null ? aud.MessageCounter : aud.MessageCounter + 1;
            aud.CallbacksCounter = callback == null ? aud.CallbacksCounter : aud.CallbacksCounter + 1;
            _appServices.AllUsersDataService.Update(aud);
        }

        public AnswerMessage ProcessMessage(string customText = null)
        {
            return CommandHandler(customText);
        }

        private AnswerMessage CommandHandler(string customText = null)
        {
            string textReceived = customText ?? _message.Text;
            if (textReceived == null)
                return null;

            textReceived = textReceived.ToLower();

            var petDB = _appServices.PetService.Get(_userId);
            var userDB = _appServices.UserService.Get(_userId);

            if (textReceived == "/language")
                ChangeLanguage();
            if (textReceived == "/help")
                ShowHelpInfo();

            if (textReceived == "/pet")
                ShowPetInfo(petDB);
            if (textReceived == "/bathroom")
                GoToBathroom(petDB);
            if (textReceived == "/kitchen")
                GoToKitchen(petDB);
            if (textReceived == "/gameroom")
                GoToGameroom(petDB);
            if (textReceived == "/hospital")
                GoToHospital(petDB);
            if (textReceived == "/ranks")
                ShowRankingInfo();
            if (textReceived == "/sleep")
                GoToSleep(petDB);
            if (textReceived == "/test")
            {
                Log.Debug($"Called /test for {_userInfo}");
                var toSend = new AnswerMessage()
                {
                    Text = DevelopWarning,
                    StickerId = StickersId.DevelopWarningSticker,
                    ReplyMarkup = null,
                    InlineKeyboardMarkup = null
                };
                _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
            }
            if (textReceived == "/menu")
                ShowMenuInfo();
            if (textReceived == "/rename")
                RenamePet(petDB);
            if (textReceived == "/work")
                ShowWorkInfo(petDB);
            if (textReceived == "/reward")
                ShowRewardInfo(userDB);
            if (textReceived == "/referal")
                ShowReferalInfo();
#if DEBUG
            if (textReceived == "/restart")
                RestartPet(petDB);
            if (textReceived == "/kill")
                TestKillPet(petDB);
#endif
            return null;
        }

        public void CallbackHandler()
        {
            var userDb = _appServices.UserService.Get(_userId);
            var petDb = _appServices.PetService.Get(_userId);

            if (userDb == null || petDb == null)
                return;

            if (_callback.Data == new CallbackButtons.PetCommand().PetCommandInlineBasicInfo.CallbackData)
            {
                ShowBasicInfoInline(petDb);
                return;
            }

            if (_callback.Data == new CallbackButtons.PetCommand().PetCommandInlineExtraInfo.CallbackData)
            {
                ShowExtraInfoInline(petDb);
                return;
            }

            if (_callback.Data == new CallbackButtons.KitchenCommand().KitchenCommandInlineBread.CallbackData)
            {
                FeedWithBreadInline(petDb);
                return;
            }

            if (_callback.Data == new CallbackButtons.KitchenCommand().KitchenCommandInlineRedApple.CallbackData)
            {
                FeedWithAppleInline(petDb);
                return;
            }

            if (_callback.Data == new CallbackButtons.KitchenCommand().KitchenCommandInlineChocolate.CallbackData)
            {
                FeedWithChocolateInline(petDb);
                return;
            }

            if (_callback.Data == new CallbackButtons.KitchenCommand().KitchenCommandInlineLollipop.CallbackData)
            {
                FeedWithLollipopInline(petDb);
                return;
            }

            if (_callback.Data == new CallbackButtons.SleepCommand().SleepCommandInlinePutToSleep(default).CallbackData)
            {
                PutToSleepInline(petDb);
                return;
            }

            if (_callback.Data == new CallbackButtons.WorkCommand().WorkCommandInlineWorkOnPC.CallbackData)
            {
                WorkOnPCInline(petDb);
                return;
            }

            if (_callback.Data == new CallbackButtons.WorkCommand().WorkCommandInlineShowTime(default).CallbackData)
            {
                WorkOnPCInline(petDb);
                return;
            }

            if (_callback.Data == new CallbackButtons.RewardsCommand().RewardCommandInlineDailyReward.CallbackData)
            {
                GetDailyRewardInline(userDb);
                return;
            }

            if (_callback.Data == new CallbackButtons.RewardsCommand().RewardCommandDailyRewardInlineShowTime(default).CallbackData)
            {
                GetDailyRewardInline(userDb);
                return;
            }

            if (_callback.Data == "gameroomCommandInlineCard")
            {
                PlayCardInline(petDb);
                return;
            }

            if (_callback.Data == new CallbackButtons.GameroomCommand().GameroomCommandInlineDice.CallbackData)
            {
                PlayDiceInline(petDb);
                return;
            }

            if (_callback.Data == new CallbackButtons.HospitalCommand().HospitalCommandCurePills.CallbackData)
            {
                CureWithPill(petDb);
                return;
            }

            if (_callback.Data == new CallbackButtons.BathroomCommand().BathroomCommandBrushTeeth.CallbackData)
            {
                TeethInline(petDb);
                return;
            }

            if (_callback.Data == new CallbackButtons.BathroomCommand().BathroomCommandTakeShower.CallbackData)
            {
                TakeShowerInline(petDb);
                return;
            }

            if (_callback.Data == new CallbackButtons.RanksCommand().RanksCommandInlineGold.CallbackData)
            {
                ShowRanksGold();
                return;
            }

            if (_callback.Data == new CallbackButtons.RanksCommand().RanksCommandInlineLevel.CallbackData)
            {
                ShowRanksLevel();
                return;
            }

            if (_callback.Data == new CallbackButtons.RanksCommand().RanksCommandInlineApples.CallbackData)
            {
                ShowRanksApples();
                return;
            }
        }

        #region Message Answers
        private void ShowWorkInfo(Pet petDB)
        {
            Log.Debug($"Called /ShowWorkInfo for {_userInfo}");

            var accessCheck = CheckStatusIsInactiveOrNull(petDB, IsGoToWorkCommand: true);
            if (accessCheck != null)
            {
                Log.Debug($"Pet is busy for {_userInfo}");
                _appServices.BotControlService.SendAnswerMessageAsync(accessCheck, _userId, false);
                return;
            }

            string toSendText = string.Empty;

            List<CallbackModel> inlineParts;
            InlineKeyboardMarkup toSendInline = default;

            if (petDB.CurrentStatus == (int)CurrentStatus.WorkingOnPC)
            {
                TimeSpan remainsTime = new TimesToWait().WorkOnPCToWait - (DateTime.UtcNow - petDB.StartWorkingTime);

                //if _callback handled when pet is working
                if (remainsTime > TimeSpan.Zero)
                {
                    Log.Debug($"Pet is working for {_userInfo}");
                    _appServices.BotControlService.SendAnswerMessageAsync(GetRemainedTimeWork(remainsTime), _userId, false);
                    return;
                }
            }
            else
            {
                toSendText = string.Format(workCommand, new TimesToWait().WorkOnPCToWait.TotalMinutes, Rewards.WorkOnPCGoldReward, petDB.Fatigue);

                inlineParts = new InlineItems().InlineWork;
                toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

                var aud = _appServices.AllUsersDataService.Get(_userId);
                aud.WorkCommandCounter++;
                _appServices.AllUsersDataService.Update(aud);
            }

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.PetWork_Cat,
                InlineKeyboardMarkup = toSendInline
            };

            _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private void ShowRewardInfo(User userDB)
        {
            Log.Debug($"Called /ShowRewardInfo for {_userInfo}");

            string toSendText = string.Empty;

            List<CallbackModel> inlineParts;
            InlineKeyboardMarkup toSendInline = default;

            if (userDB.GotDailyRewardTime.AddHours(new TimesToWait().DailyRewardToWait.TotalHours) > DateTime.UtcNow)
            {
                TimeSpan remainsTime = new TimesToWait().DailyRewardToWait - (DateTime.UtcNow - userDB.GotDailyRewardTime);

                if (remainsTime > TimeSpan.Zero)
                {
                    _appServices.BotControlService.SendAnswerMessageAsync(GetRemainedTimeDailyReward(remainsTime), _userId, false);
                    return;
                }
            }
            else
            {
                toSendText = string.Format(rewardCommand);

                inlineParts = new InlineItems().InlineRewards;
                toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

                var aud = _appServices.AllUsersDataService.Get(_userId);
                aud.RewardCommandCounter++;
                _appServices.AllUsersDataService.Update(aud);
            }

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.DailyRewardSticker,
                InlineKeyboardMarkup = toSendInline
            };

            _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private void ChangeLanguage()
        {
            _appServices.UserService.UpdateLanguage(_userId, null);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.LanguageCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = Resources.Resources.ChangeLanguage,
                StickerId = StickersId.ChangeLanguageSticker,
                ReplyMarkup = LanguagesMarkup,
                InlineKeyboardMarkup = null
            };
            Log.Debug($"Called /ChangeLanugage for {_userInfo}");
            _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private void ShowPetInfo(Pet petDB)
        {
            var encodedPetName = HttpUtility.HtmlEncode(petDB.Name);
            encodedPetName = "<b>" + encodedPetName + "</b>";

            string toSendText = string.Format(petCommand,
                                              encodedPetName,
                                              petDB.HP,
                                              petDB.EXP,
                                              petDB.Level,
                                              petDB.Satiety,
                                              petDB.Fatigue,
                                              Extensions.GetCurrentStatus(petDB.CurrentStatus),
                                              petDB.Joy,
                                              _appServices.UserService.Get(_userId).Gold,
                                              petDB.Hygiene);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.PetCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.PetInfo_Cat,
                ReplyMarkup = null,
                InlineKeyboardMarkup = Extensions.InlineKeyboardOptimizer(new InlineItems().InlinePet),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
            };
            Log.Debug($"Called /ShowPetInfo for {_userInfo}");
            _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private AnswerMessage CheckStatusIsInactiveOrNull(Pet petDB, bool IsGoToSleepCommand = false, bool IsGoToWorkCommand = false)
        {
            if (petDB.CurrentStatus == (int)CurrentStatus.Sleeping && !IsGoToSleepCommand)
            {
                string denyText = string.Format(denyAccessSleeping);
                return new AnswerMessage()
                {
                    Text = denyText,
                    StickerId = StickersId.PetBusy_Cat
                };
            }

            if (petDB.CurrentStatus == (int)CurrentStatus.WorkingOnPC && !IsGoToWorkCommand)
            {
                string denyText = string.Format(denyAccessWorking);
                return new AnswerMessage()
                {
                    Text = denyText,
                    StickerId = StickersId.PetBusy_Cat
                };
            }

            return null;
        }
        private void GoToBathroom(Pet petDB)
        {
            Log.Debug($"Called /GoToBathroom for {_userInfo}");
            var accessCheck = CheckStatusIsInactiveOrNull(petDB);
            if (accessCheck != null)
            {
                Log.Debug($"Pet is busy for {_userInfo}");
                _appServices.BotControlService.SendAnswerMessageAsync(accessCheck, _userId, false);
                return;
            }

            string toSendText = string.Format(bathroomCommand, petDB.Hygiene);

            List<CallbackModel> inlineParts = new InlineItems().InlineHygiene;
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.BathroomCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.PetBathroom_Cat,
                InlineKeyboardMarkup = toSendInline
            };
            _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private void GoToKitchen(Pet petDB)
        {
            Log.Debug($"Called /GoToKitchen for {_userInfo}");
            var accessCheck = CheckStatusIsInactiveOrNull(petDB);
            if (accessCheck != null)
            {
                Log.Debug($"Pet is busy for {_userInfo}");
                _appServices.BotControlService.SendAnswerMessageAsync(accessCheck, _userId, false);
                return;
            }

            string toSendText = string.Format(kitchenCommand, petDB.Satiety, _appServices.UserService.Get(_userId).Gold);

            List<CallbackModel> inlineParts = new InlineItems().InlineFood;
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.KitchenCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.PetKitchen_Cat,
                InlineKeyboardMarkup = toSendInline
            };
            _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private void GoToGameroom(Pet petDB)
        {
            Log.Debug($"Called /GoToGameroom for {_userInfo}");
            var accessCheck = CheckStatusIsInactiveOrNull(petDB);
            if (accessCheck != null)
            {
                Log.Debug($"Pet is busy for {_userInfo}");
                _appServices.BotControlService.SendAnswerMessageAsync(accessCheck, _userId, false);
                return;
            }

            string toSendText = string.Format(gameroomCommand,
                                              petDB.Fatigue,
                                              petDB.Joy,
                                              _appServices.UserService.Get(_userId).Gold,
                                              Factors.CardGameJoyFactor,
                                              Costs.AppleGame,
                                              Factors.DiceGameJoyFactor,
                                              Costs.DiceGame);

            List<CallbackModel> inlineParts = new InlineItems().InlineGames;
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.GameroomCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.PetGameroom_Cat,
                InlineKeyboardMarkup = toSendInline
            };
            _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private void GoToHospital(Pet petDB)
        {
            Log.Debug($"Called /GoToHospital for {_userInfo}");
            var accessCheck = CheckStatusIsInactiveOrNull(petDB);
            if (accessCheck != null)
            {
                Log.Debug($"Pet is busy for {_userInfo}");
                _appServices.BotControlService.SendAnswerMessageAsync(accessCheck, _userId, false);
                return;
            }

            string commandHospital =  petDB.HP switch
            {
                >= 80 => hospitalCommandHighHp,
                >20 and < 80 => hospitalCommandMidHp,
                _ => hospitalCommandLowHp
            };

            string stickerHospital =  petDB.HP switch
            {
                >= 80 => StickersId.PetHospitalHighHP_Cat,
                >20 and < 80 => StickersId.PetHospitalMidHP_Cat,
                _ => StickersId.PetHospitalLowHP_Cat
            };

            string toSendText = string.Format(commandHospital, petDB.HP);

            List<CallbackModel> inlineParts = new InlineItems().InlineHospital;
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.HospitalCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = stickerHospital,
                InlineKeyboardMarkup = toSendInline
            };

            _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private void ShowRankingInfo()
        {
            Log.Debug($"Called /ShowRankingInfo for {_userInfo}");

            var anwserRating = GetRanksByLevel();

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.RanksCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = anwserRating,
                StickerId = StickersId.PetRanks_Cat,
                InlineKeyboardMarkup = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineRanks, 3),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
            };

            _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private void GoToSleep(Pet petDB)
        {
            Log.Debug($"Called /GoToSleep for {_userInfo}");

            var accessCheck = CheckStatusIsInactiveOrNull(petDB, true);
            if (accessCheck != null)
            {
                Log.Debug($"Pet is busy for {_userInfo}");
                _appServices.BotControlService.SendAnswerMessageAsync(accessCheck, _userId, false);
                return;
            }

            string toSendText = string.Format(sleepCommand,
                                              petDB.Name,
                                              petDB.Fatigue,
                                              Extensions.GetCurrentStatus(petDB.CurrentStatus));

            InlineKeyboardMarkup toSendInline;
            if (petDB.CurrentStatus == (int)CurrentStatus.Sleeping)
            {
                var ticksToWait = (petDB.ToWakeUpTime - DateTime.UtcNow).Ticks;
                string timeToWaitStr = string.Format(sleepCommandInlineShowTime, new DateTime().AddTicks(ticksToWait).ToString("HH:mm:ss"));

                toSendInline = Extensions.InlineKeyboardOptimizer(
                    new List<CallbackModel>()
                    {
                        new CallbackModel()
                        {
                            Text = timeToWaitStr,
                            CallbackData = "sleepCommandInlinePutToSleep"
                        }
                    });
            }
            else
                toSendInline = Extensions.InlineKeyboardOptimizer(
                    new List<CallbackModel>()
                    {
                        new CallbackModel()
                        {
                            Text = sleepCommandInlinePutToSleep,
                            CallbackData = new CallbackButtons.SleepCommand().SleepCommandInlinePutToSleep(default).CallbackData
                        }
                    });

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.SleepCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.PetSleep_Cat,
                InlineKeyboardMarkup = toSendInline
            };

            _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private void ShowHelpInfo()
        {
            string toSendText = string.Format(helpCommand);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.HelpCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.HelpCommandSticker
            };
            Log.Debug($"Called /ShowHelpInfo for {_userInfo}");
            _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task ShowReferalInfo()
        {
            Log.Debug($"Called /ShowReferalInfo for {_userInfo}");
            var botUsername = (await _appServices.SInfoService.GetBotUserInfo()).Username;

            var refAmounts = _appServices.ReferalInfoService.GetDoneRefsAmount(_userId);
            var goldByRef = Rewards.ReferalAdded * refAmounts;
            var refLink = Extensions.GetReferalLink(_userId, botUsername);
            string toSendText = string.Format(referalCommand, refAmounts, goldByRef, refLink);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.MenuCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.ReferalCommandSticker,
                InlineKeyboardMarkup = new InlineKeyboardMarkup(new List<InlineKeyboardButton>()
                {
                    new InlineKeyboardButton(new CallbackButtons.ReferalCommand().ToAddToNewGroupReferalCommand.Text)
                    {
                        Url = $"http://t.me/{botUsername}?startgroup=start"
                    },
                    new InlineKeyboardButton(new CallbackButtons.ReferalCommand().ToShareReferalCommand.Text)
                    {
                        Url = $"https://t.me/share/url?url={Extensions.GetReferalLink(_userId, botUsername)}"
                    }
                }),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
            };
            _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private void ShowMenuInfo()
        {
            Log.Debug($"Called /ShowMenuInfo for {_userInfo}");

            string toSendText = string.Format(menuCommand);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.MenuCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.MenuCommandSticker
            };
            _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private void RenamePet(Pet petDB)
        {
            Log.Debug($"Called /RenamePet for {_userInfo}");

            if (_appServices.BannedUsersService.GetAll().Any(bs => bs.UserId == _userId && bs.IsRenameBanned))
            {
                Log.Debug($"Banned for renaming for {_userInfo}");

                string toSendTextBan = string.Format(renameBannedCommand);

                var audF = _appServices.AllUsersDataService.Get(_userId);
                audF.RenameCommandCounter++;
                _appServices.AllUsersDataService.Update(audF);

                var toSend = new AnswerMessage() { Text = toSendTextBan, StickerId = StickersId.BannedSticker };
                _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
            }

            string toSendText = string.Format(renameCommand);

            _appServices.MetaUserService.UpdateIsPetNameAskedOnRename(_userId, true);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.RenameCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSendFinal = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.RenamePetSticker
            };

            _appServices.BotControlService.SendAnswerMessageAsync(toSendFinal, _userId, false);
        }
        private void TestKillPet(Pet petDB)
        {
            string toSendText = string.Format("HP is zero (0) for {0}", petDB.Name);

            _appServices.PetService.UpdateHP(_userId, 0);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.BannedSticker
            };

            _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId);
        }
        private void RestartPet(Pet petDB)
        {
            string toSendText = string.Format(restartCommand, petDB.Name);

            _appServices.PetService.Remove(_userId);
            _appServices.UserService.Remove(_userId);
            _appServices.ChatService.Remove(_userId);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.DroppedPetSticker
            };

            _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId);
        }
        #endregion

        #region Inline Answers
        private void ShowBasicInfoInline(Pet petDB)
        {
            var encodedPetName = HttpUtility.HtmlEncode(petDB.Name);
            encodedPetName = "<b>" + encodedPetName + "</b>";
            var userDB = _appServices.UserService.Get(_userId);
            Culture = new System.Globalization.CultureInfo(userDB.Culture);
            string toSendText = string.Format(petCommand,
                                              encodedPetName,
                                              petDB.HP,
                                              petDB.EXP,
                                              petDB.Level,
                                              petDB.Satiety,
                                              petDB.Fatigue,
                                              Extensions.GetCurrentStatus(petDB.CurrentStatus),
                                              petDB.Joy,
                                              userDB.Gold,
                                              petDB.Hygiene);

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
            {
                new CallbackButtons.PetCommand().PetCommandInlineExtraInfo
            });

            Log.Debug($"Callbacked ShowBasicInfoInline for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline, Telegram.Bot.Types.Enums.ParseMode.Html),
                                                              false);
        }
        private void ShowExtraInfoInline(Pet petDB)
        {
            var encodedPetName = HttpUtility.HtmlEncode(petDB.Name);
            encodedPetName = "<b>" + encodedPetName + "</b>";
            string toSendText = string.Format(petCommandMoreInfo1, encodedPetName, petDB.BirthDateTime, _appServices.ReferalInfoService.GetDoneRefsAmount(_userId));
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
            {
                new CallbackButtons.PetCommand().PetCommandInlineBasicInfo
            });

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.ExtraInfoShowedTimesCounter++;
            _appServices.AllUsersDataService.Update(aud);

            Log.Debug($"Callbacked ShowExtraInfoInline for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline, Telegram.Bot.Types.Enums.ParseMode.Html),
                                                              false);
        }

        private void PetIsNotHungry()
        {
            Log.Debug($"Sent alert PetIsNotHungry for {_userInfo}");

            string answerLocal = string.Format(tooManyStarvingCommand);
            _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, answerLocal, true);
        }
        private void NotEnoughGold()
        {
            Log.Debug($"Sent alert NotEnoughGold for {_userInfo}");

            string anwserLocal = string.Format(goldNotEnough);
            _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwserLocal, true);
        }

        private void PetIsTooTired()
        {
            Log.Debug($"Sent alert PetIsTooTired for {_userInfo}");

            string anwserLocal = string.Format(tooTiredText);
            _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwserLocal, true);
        }

        private void PetIsFullOfJoy()
        {
            Log.Debug($"Sent alert PetIsFullOfJoy for {_userInfo}");

            string anwserLocal = string.Format(PetIsFullOfJoyText);
            _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwserLocal, true);
        }

        private void TakeShowerInline(Pet petDB)
        {
            if (petDB.Hygiene >= 100)
            {
                SendAlertToUser(PetIsCleanEnoughAlert, true);
                return;
            }

            var newHygiene = petDB.Hygiene + HygieneFactors.ShowerFactor;
            newHygiene = newHygiene > 100 ? 100 : newHygiene;

            _appServices.PetService.UpdateHygiene(_userId, newHygiene);

            string anwser = string.Format(PetHygieneAnwserCallback, HygieneFactors.ShowerFactor);
            SendAlertToUser(anwser);

            string toSendText = string.Format(bathroomCommand, newHygiene);
            List<CallbackModel> inlineParts = new InlineItems().InlineHygiene;
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            Log.Debug($"Callbacked TakeShowerInline for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }
        private void TeethInline(Pet petDB)
        {
            if (petDB.Hygiene >= 100)
            {
                SendAlertToUser(PetIsCleanEnoughAlert, true);
                return;
            }

            var newHygiene = petDB.Hygiene + HygieneFactors.TeethFactor;
            newHygiene = newHygiene > 100 ? 100 : newHygiene;

            _appServices.PetService.UpdateHygiene(_userId, newHygiene);

            string anwser = string.Format(PetHygieneAnwserCallback, HygieneFactors.TeethFactor);
            SendAlertToUser(anwser);

            string toSendText = string.Format(bathroomCommand, newHygiene);
            List<CallbackModel> inlineParts = new InlineItems().InlineHygiene;
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            Log.Debug($"Callbacked TeethInline for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }

        private bool ToContinueFeedingPet(Pet petDB, User userDB, int foodPrice)
        {
            if (petDB.Satiety >= 100)
            {
                PetIsNotHungry();
                return false;
            }

            var newGold = userDB.Gold - foodPrice;
            if (newGold < 0)
            {
                NotEnoughGold();
                return false;
            }

            return true;
        }
        private void FeedWithBreadInline(Pet petDB)
        {
            var userDB = _appServices.UserService.Get(_userId);

            if (!ToContinueFeedingPet(petDB, userDB, Costs.Bread))
                return;

            var newGold = userDB.Gold - Costs.Bread;
            var newSatiety = Math.Round(petDB.Satiety + FoodFactors.BreadHungerFactor, 2);
            newSatiety = newSatiety > 100 ? 100 : newSatiety;

            _appServices.UserService.UpdateGold(_userId, newGold);
            _appServices.PetService.UpdateSatiety(_userId, newSatiety);
            var aud = _appServices.AllUsersDataService.Get(_userId);

            aud.BreadEatenCounter++;
            aud.GoldSpentCounter += Costs.Bread;
            _appServices.AllUsersDataService.Update(aud);

            string anwser = string.Format(PetFeedingAnwserCallback, (int)FoodFactors.BreadHungerFactor);
            SendAlertToUser(anwser);

            string toSendText = string.Format(kitchenCommand, newSatiety, newGold);
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineFood, 3);

            Log.Debug($"Callbacked FeedWithBreadInline for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }
        private void FeedWithAppleInline(Pet petDB)
        {
            var userDB = _appServices.UserService.Get(_userId);

            if (!ToContinueFeedingPet(petDB, userDB, Costs.Apple))
                return;

            var newGold = userDB.Gold - Constants.Costs.Apple;
            var newSatiety = Math.Round(petDB.Satiety + FoodFactors.RedAppleHungerFactor, 2);
            newSatiety = newSatiety > 100 ? 100 : newSatiety;

            _appServices.UserService.UpdateGold(_userId, newGold);
            _appServices.PetService.UpdateSatiety(_userId, newSatiety);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.AppleEatenCounter++;
            aud.GoldSpentCounter += Costs.Apple;
            _appServices.AllUsersDataService.Update(aud);

            SendAlertToUser(string.Format(PetFeedingAnwserCallback, (int)FoodFactors.RedAppleHungerFactor));

            string toSendText = string.Format(kitchenCommand, newSatiety, newGold);
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineFood, 3);

            Log.Debug($"Callbacked FeedWithAppleInline for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }
        private void FeedWithChocolateInline(Pet petDB)
        {
            var userDB = _appServices.UserService.Get(_userId);

            if (!ToContinueFeedingPet(petDB, userDB, Costs.Chocolate))
                return;

            var newGold = userDB.Gold - Constants.Costs.Chocolate;
            var newSatiety = Math.Round(petDB.Satiety + FoodFactors.ChocolateHungerFactor, 2);
            newSatiety = newSatiety > 100 ? 100 : newSatiety;

            _appServices.UserService.UpdateGold(_userId, newGold);
            _appServices.PetService.UpdateSatiety(_userId, newSatiety);
            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.ChocolateEatenCounter++;
            aud.GoldSpentCounter += Constants.Costs.Chocolate;
            _appServices.AllUsersDataService.Update(aud);

            string anwser = string.Format(PetFeedingAnwserCallback, (int)FoodFactors.ChocolateHungerFactor);
            SendAlertToUser(anwser);

            string toSendText = string.Format(kitchenCommand, newSatiety, newGold);
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineFood, 3);

            Log.Debug($"Callbacked FeedWithChocolateInline for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }
        private void FeedWithLollipopInline(Pet petDB)
        {
            var userDB = _appServices.UserService.Get(_userId);

            if (!ToContinueFeedingPet(petDB, userDB, Costs.Lollipop))
                return;

            var newGold = userDB.Gold - Constants.Costs.Lollipop;
            var newSatiety = Math.Round(petDB.Satiety + FoodFactors.LollipopHungerFactor, 2);
            newSatiety = newSatiety > 100 ? 100 : newSatiety;

            _appServices.UserService.UpdateGold(_userId, newGold);
            _appServices.PetService.UpdateSatiety(_userId, newSatiety);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.LollypopEatenCounter++;
            aud.GoldSpentCounter += Costs.Lollipop;
            _appServices.AllUsersDataService.Update(aud);

            string anwser = string.Format(PetFeedingAnwserCallback, (int)FoodFactors.LollipopHungerFactor);
            SendAlertToUser(anwser);

            string toSendText = string.Format(kitchenCommand, newSatiety, newGold);
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineFood, 3);

            Log.Debug($"Callbacked FeedWithLollipopInline for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }

        private void PutToSleepInline(Pet petDB)
        {
            Pet petResult = new Pet().Clone(petDB);

            if (CheckStatusIsInactive(petResult, true))
                return;

            if (petResult.CurrentStatus == (int)CurrentStatus.Sleeping)
            {
                UpdateSleepingInline(petResult);
                return;
            }

            if (petResult.Fatigue < Limits.ToRestMinLimitOfFatigue)
            {
                DeclineSleepingInline(petResult);
                return;
            }

            StartSleepingInline(petResult);
        }
        private void StartSleepingInline(Pet petDB)
        {
            petDB.CurrentStatus = (int)CurrentStatus.Sleeping;
            petDB.StartSleepingTime = DateTime.UtcNow;
            petDB.ToWakeUpTime = DateTime.UtcNow + new TimesToWait().SleepToWait;
            _appServices.PetService.Update(petDB.UserId, petDB);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.SleepenTimesCounter++;
            _appServices.AllUsersDataService.Update(aud);


            var timeToWait = petDB.ToWakeUpTime - DateTime.UtcNow;
            string toSendText = string.Format(sleepCommand, petDB.Name, petDB.Fatigue, Extensions.GetCurrentStatus(petDB.CurrentStatus));
            InlineKeyboardMarkup toSendInline =
                Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
                {
                    new CallbackButtons.SleepCommand().SleepCommandInlinePutToSleep(timeToWait)
                });

            Log.Debug($"Callbacked StartSleepingInline for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }
        private void DeclineSleepingInline(Pet petDB)
        {
            _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, PetSleepingDoesntWantYetAnwserCallback);
            string sendTxt = string.Format(sleepCommand, petDB.Name, petDB.Fatigue, Extensions.GetCurrentStatus(petDB.CurrentStatus));

            InlineKeyboardMarkup toSendInlineWhileActive =
                Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
                {
                    new CallbackModel()
                    {
                        Text = sleepCommandInlinePutToSleep,
                        CallbackData = "sleepCommandInlinePutToSleep"
                    }
                });

            Log.Debug($"Callbacked DeclineSleepingInline for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(sendTxt, toSendInlineWhileActive),
                                                              false);
        }
        private void UpdateSleepingInline(Pet petDB)
        {
            _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, PetSleepingAlreadyAnwserCallback);

            string toSendText = string.Format(sleepCommand, petDB.Name, petDB.Fatigue, Extensions.GetCurrentStatus(petDB.CurrentStatus));

            var timeToWait = petDB.ToWakeUpTime - DateTime.UtcNow;
            timeToWait = timeToWait < TimeSpan.Zero ? default : timeToWait;

            InlineKeyboardMarkup toSendInline =
                Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
                {
                    new CallbackButtons.SleepCommand().SleepCommandInlinePutToSleep(timeToWait)
                });

            Log.Debug($"Callbacked UpdateSleepingInline for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }

        private void PlayCardInline(Pet petDB)
        {
            var userDB = _appServices.UserService.Get(_userId);

            var newGold = userDB.Gold - Costs.AppleGame;
            if (newGold < 0)
            {
                NotEnoughGold();
                return;
            }

            var newFatigue = petDB.Fatigue + Factors.CardGameFatigueFactor;
            if (newFatigue > 100)
            {
                PetIsTooTired();
                return;
            }

            if (petDB.Joy >= 100)
            {
                PetIsFullOfJoy();
                return;
            }
            var newJoy = petDB.Joy + Factors.CardGameJoyFactor;
            newJoy = newJoy > 100 ? 100 : newJoy;

            _appServices.UserService.UpdateGold(_userId, newGold);
            _appServices.PetService.UpdateFatigue(_userId, newFatigue);
            _appServices.PetService.UpdateJoy(_userId, newJoy);

            string anwser = string.Format(PetPlayingAnwserCallback, Factors.CardGameFatigueFactor);
            SendAlertToUser(anwser);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.CardsPlayedCounter++;
            _appServices.AllUsersDataService.Update(aud);

            string toSendText = string.Format(gameroomCommand,
                                              newFatigue,
                                              newJoy,
                                              newGold,
                                              Factors.CardGameJoyFactor,
                                              Factors.CardGameJoyFactor,
                                              Costs.AppleGame,
                                              Factors.DiceGameJoyFactor,
                                              Costs.DiceGame);
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineGames, 3);

            Log.Debug($"Callbacked PlayCardInline for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }
        private void PlayDiceInline(Pet petDB)
        {
            var userDB = _appServices.UserService.Get(_userId);

            var newGold = userDB.Gold - Costs.DiceGame;
            if (newGold < 0)
            {
                NotEnoughGold();
                return;
            }

            var newFatigue = petDB.Fatigue + Factors.DiceGameFatigueFactor;
            if (newFatigue > 100)
            {
                PetIsTooTired();
                return;
            }

            if (petDB.Joy >= 100)
            {
                PetIsFullOfJoy();
                return;
            }
            var newJoy = petDB.Joy + Factors.DiceGameJoyFactor;
            newJoy = newJoy > 100 ? 100 : newJoy;

            _appServices.UserService.UpdateGold(_userId, newGold);
            _appServices.PetService.UpdateFatigue(_userId, newFatigue);
            _appServices.PetService.UpdateJoy(_userId, newJoy);
            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.DicePlayedCounter++;
            aud.GoldSpentCounter += Costs.DiceGame;
            _appServices.AllUsersDataService.Update(aud);

            string anwser = string.Format(PetPlayingAnwserCallback, Factors.DiceGameJoyFactor);
            _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwser);

            string toSendText = string.Format(gameroomCommand,
                                              newFatigue,
                                              newJoy,
                                              newGold,
                                              Factors.CardGameJoyFactor,
                                              Costs.AppleGame,
                                              Factors.DiceGameJoyFactor,
                                              Costs.DiceGame);
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineGames, 3);

            Log.Debug($"Callbacked PlayDiceInline for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }
        private void WorkOnPCInline(Pet petDB)
        {
            InlineKeyboardMarkup toSendInline;
            string toSendText;

            if (petDB.CurrentStatus == (int)CurrentStatus.Active)
            {
                if (_callback.Data == new CallbackButtons.WorkCommand().WorkCommandInlineShowTime(default).CallbackData)
                {
                    UpdateWorkOnPCButtonToDefault();
                    return;
                }

                var newFatigue = petDB.Fatigue + Factors.WorkOnPCFatigueFactor;
                if (newFatigue > 100)
                {
                    PetIsTooTired();
                    return;
                }

                _appServices.PetService.UpdateFatigue(_userId, newFatigue);
                _appServices.UserService.UpdateGold(_userId, _appServices.UserService.Get(_userId).Gold += Rewards.WorkOnPCGoldReward);
                _appServices.PetService.UpdateCurrentStatus(_userId, (int)CurrentStatus.WorkingOnPC);

                var aud = _appServices.AllUsersDataService.Get(_userId);
                aud.GoldEarnedCounter += Rewards.WorkOnPCGoldReward;
                aud.WorkOnPCCounter++;
                _appServices.AllUsersDataService.Update(aud);

                var startWorkingTime = DateTime.UtcNow;
                _appServices.PetService.UpdateStartWorkingTime(_userId, startWorkingTime);

                string anwser = string.Format(PetWorkingAnswerCallback, Factors.WorkOnPCFatigueFactor, Rewards.WorkOnPCGoldReward);
                SendAlertToUser(anwser, true);

                toSendText = string.Format(workCommandPCWorking);

                TimeSpan remainsTime = new TimesToWait().WorkOnPCToWait - (DateTime.UtcNow - startWorkingTime);

                toSendInline = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
                {
                    new CallbackButtons.WorkCommand().WorkCommandInlineShowTime(remainsTime)
                });

                Log.Debug($"Callbacked WorkOnPCInline (default) for {_userInfo}");
                _appServices.BotControlService.SendAnswerCallback(_userId,
                                                                  _callback?.Message?.MessageId ?? 0,
                                                                  new AnswerCallback(toSendText, toSendInline),
                                                                  false);
            }
            else if (petDB.CurrentStatus == (int)CurrentStatus.WorkingOnPC)
            {
                TimeSpan remainsTime = new TimesToWait().WorkOnPCToWait - (DateTime.UtcNow - petDB.StartWorkingTime);

                //if _callback handled when time of work is over
                if (remainsTime <= TimeSpan.Zero)
                {
                    petDB.CurrentStatus = (int)CurrentStatus.Active;
                    _appServices.PetService.UpdateCurrentStatus(_userId, petDB.CurrentStatus);

                    string toSendTextIfTimeOver = string.Format(workCommand, new TimesToWait().WorkOnPCToWait.TotalMinutes, Rewards.WorkOnPCGoldReward);
                    List<CallbackModel> inlineParts = new InlineItems().InlineWork;
                    InlineKeyboardMarkup toSendInlineIfTimeOver = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

                    Log.Debug($"Callbacked WorkOnPCInline (work is over) for {_userInfo}");
                    _appServices.BotControlService.SendAnswerCallback(_userId,
                                                                      _callback?.Message?.MessageId ?? 0,
                                                                      new AnswerCallback(toSendTextIfTimeOver, toSendInlineIfTimeOver),
                                                                      false);
                    return;
                }

                Log.Debug($"Callbacked WorkOnPCInline (still working) for {_userInfo}");

                //if _callback handled when pet is still working
                _appServices.BotControlService.SendAnswerCallback(_userId,
                                                                  _callback?.Message?.MessageId ?? 0,
                                                                  ShowRemainedTimeWorkCallback(remainsTime),
                                                                  false);
            }
        }

        private void GetDailyRewardInline(User userDB)
        {
            var dateTimeWhenOver = userDB.GotDailyRewardTime.Add(new TimesToWait().DailyRewardToWait);
            if (dateTimeWhenOver > DateTime.UtcNow)
            {
                Log.Debug($"Callbacked GetDailyRewardInline (still waiting) for {_userInfo}");
                _appServices.BotControlService.SendAnswerCallback(_userId,
                                                                  _callback?.Message?.MessageId ?? 0,
                                                                  ShowRemainedTimeDailyRewardCallback(dateTimeWhenOver - DateTime.UtcNow, true),
                                                                  false);
                return;
            }

            var newGold = userDB.Gold + Constants.Rewards.DailyGoldReward;

            _appServices.UserService.UpdateGold(_userId, newGold);
            _appServices.UserService.UpdateDailyRewardTime(_userId, DateTime.UtcNow);
            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.GoldEarnedCounter += Constants.Rewards.DailyGoldReward;
            _appServices.AllUsersDataService.Update(aud);

            string anwser = string.Format(DailyRewardAnwserCallback, Rewards.DailyGoldReward);
            SendAlertToUser(anwser, true);

            Log.Debug($"Callbacked GetDailyRewardInline (default) for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              ShowRemainedTimeDailyRewardCallback(new TimeSpan(23, 59, 59), false),
                                                              false);
            return;
        }

        private AnswerMessage GetRemainedTimeWork(TimeSpan remainedTime)
        {
            InlineKeyboardMarkup toSendInline;
            string toSendText;

            toSendText = string.Format(workCommandPCWorking);

            toSendInline = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
            {
                new CallbackButtons.WorkCommand().WorkCommandInlineShowTime(remainedTime)
            });

            return new AnswerMessage() { InlineKeyboardMarkup = toSendInline, Text = toSendText, StickerId = StickersId.PetWork_Cat };
        }
        private AnswerCallback ShowRemainedTimeWorkCallback(TimeSpan remainedTime = default)
        {
            InlineKeyboardMarkup toSendInline;
            string toSendText;

            if (remainedTime == default)
                remainedTime = new TimeSpan(0);

            string anwser = string.Format(workCommandPCWorking);
            _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwser);

            toSendText = string.Format(workCommandPCWorking);
            toSendInline = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
            {
                new CallbackButtons.WorkCommand().WorkCommandInlineShowTime(remainedTime)
            });

            return new AnswerCallback(toSendText, toSendInline);
        }

        private AnswerMessage GetRemainedTimeDailyReward(TimeSpan remainedTime)
        {
            InlineKeyboardMarkup toSendInline;
            string toSendText;

            toSendText = string.Format(rewardCommandDailyRewardGotten);
            string inlineStr = string.Format(rewardCommandDailyRewardInlineShowTime, new DateTime(remainedTime.Ticks).ToString("HH:mm:ss"));

            toSendInline = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
                    {
                        new CallbackModel()
                        {
                            Text = inlineStr,
                            CallbackData = "rewardCommandDailyRewardInlineShowTime"
                        }
                    });

            return new AnswerMessage() { InlineKeyboardMarkup = toSendInline, Text = toSendText, StickerId = StickersId.DailyRewardSticker };
        }
        private AnswerCallback ShowRemainedTimeDailyRewardCallback(TimeSpan remainedTime = default, bool isAlert = false)
        {
            InlineKeyboardMarkup toSendInline;
            string toSendText;

            if (remainedTime == default)
                remainedTime = new TimeSpan(0);

            if (isAlert)
            {
                string anwser = string.Format(rewardCommandDailyRewardGotten);
                SendAlertToUser(anwser, isAlert);
            }

            toSendText = string.Format(rewardCommandDailyRewardGotten);

            toSendInline = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
            {
                new CallbackButtons.RewardsCommand().RewardCommandDailyRewardInlineShowTime(remainedTime)
            });

            return new AnswerCallback(toSendText, toSendInline);
        }

        private void CureWithPill(Pet petDB)
        {
            var newHP = petDB.HP + Factors.PillHPFactor;
            if (newHP > 100)
                newHP = 100;

            var newJoy = petDB.Joy + Factors.PillJoyFactor;
            if (newJoy < 0)
                newJoy = 0;

            _appServices.PetService.UpdateHP(_userId, newHP);
            _appServices.PetService.UpdateJoy(_userId, newJoy);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.PillEatenCounter++;
            _appServices.AllUsersDataService.Update(aud);

            string anwser = string.Format(PetCuringAnwserCallback, Factors.PillHPFactor, Factors.PillJoyFactor);
            SendAlertToUser(anwser, true);

            string commandHospital =  newHP switch
            {
                >= 80 => hospitalCommandHighHp,
                >20 and < 80 => hospitalCommandMidHp,
                _ => hospitalCommandLowHp
            };

            string toSendText = string.Format(commandHospital, newHP);
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineHospital);

            Log.Debug($"Callbacked CureWithPill for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }

        private void ShowRanksGold()
        {
            string toSendText = GetRanksByGold();
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineRanks);

            Log.Debug($"Callbacked ShowRanksGold for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline, Telegram.Bot.Types.Enums.ParseMode.Html),
                                                              false);
        }
        private void ShowRanksApples()
        {
            string toSendText = GetRanksByApples();
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineRanks);

            Log.Debug($"Callbacked ShowRanksApples for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline, Telegram.Bot.Types.Enums.ParseMode.Html),
                                                              false);
        }
        private void ShowRanksLevel()
        {
            string toSendText = GetRanksByLevel();
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineRanks);

            Log.Debug($"Callbacked ShowRanksLevel for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline, Telegram.Bot.Types.Enums.ParseMode.Html),
                                                              false);
        }

        #endregion

        private string GetRanksByLevel()
        {
            var topPets = _appServices.PetService.GetAll()
                .OrderByDescending(p => p.Level)
                .ThenByDescending(p => p.LastUpdateTime)
                .Take(10); //First 10 top-level pets

            string anwserRating = "";
            var currentUser = _appServices.UserService.Get(_userId);
            var currentPet = _appServices.PetService.Get(_userId);

            int counter = 1;
            foreach (var petDB in topPets)
            {
                var userDB = _appServices.UserService.Get(petDB.UserId);
                string name = petDB.Name ?? userDB.Username ?? userDB.FirstName + userDB.LastName;

                if (counter == 1)
                {
                    anwserRating += ranksCommand + "\n\n";
                    if (currentUser.UserId == userDB.UserId)
                        anwserRating += "<b>" + "🌟 " + petDB.Level + " 🐱 " + HttpUtility.HtmlEncode(name) + "</b>";
                    else
                        anwserRating += "🌟 " + petDB.Level + " 🐱 " + HttpUtility.HtmlEncode(name);
                    anwserRating += "\n⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯";
                    counter++;
                }
                else
                {
                    anwserRating += "\n";

                    if (currentUser.UserId == userDB.UserId)
                        anwserRating += "<b>" + counter + ". " + petDB.Level + " 🐱 " + HttpUtility.HtmlEncode(name) + "</b>";
                    else
                        anwserRating += counter + ". " + petDB.Level + " 🐱 " + HttpUtility.HtmlEncode(name);
                    counter++;
                }
            }

            if (!topPets.Any(a => a.UserId == currentUser.UserId))
            {
                string name = currentPet.Name ?? currentUser.Username ?? currentUser.FirstName + currentUser.LastName;

                anwserRating += "\n______________________________";
                anwserRating += "\n <b>" + _appServices.PetService.GetAll()
                .OrderByDescending(p => p.Level)
                .ThenByDescending(p => p.LastUpdateTime)
                .ToList()
                .FindIndex(a => a.UserId == currentUser.UserId) + ". " + currentPet.Level + " 🐱 " + HttpUtility.HtmlEncode(name) + "</b>";
            }

            return anwserRating;
        }
        private string GetRanksByApples()
        {
            var topApples = _appServices.AppleGameDataService.GetAll()
                .OrderByDescending(a => a.TotalWins)
                .Take(10); //First 10 top-apples users

            string anwserRating = "";
            var currentUser = _appServices.UserService.Get(_userId);
            var currentPet = _appServices.PetService.Get(_userId);

            int counter = 1;
            foreach (var appleUser in topApples)
            {
                var userDB = _appServices.UserService.Get(appleUser.UserId);
                string name = " 🐱 " + _appServices.PetService.Get(appleUser.UserId)?.Name ?? userDB?.Username ?? userDB?.FirstName + userDB?.LastName ?? "";
                if (counter == 1)
                {
                    if (currentUser == null)
                        continue;

                    if (appleUser?.TotalWins == null)
                        continue;

                    anwserRating += ranksCommandApples + "\n\n";
                    if (currentUser.UserId == userDB?.UserId)
                        anwserRating += "<b>" + "🍏 " + appleUser.TotalWins + HttpUtility.HtmlEncode(name) + "</b>";
                    else
                        anwserRating += "🍏 " + appleUser.TotalWins + HttpUtility.HtmlEncode(name);
                    anwserRating += "\n⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯";
                    counter++;
                }
                else
                {
                    if (userDB == null)
                        continue;

                    if (appleUser?.TotalWins == null)
                        continue;

                    anwserRating += "\n";
                    if (currentUser.UserId == userDB.UserId)
                        anwserRating += "<b>" + counter + ". " + appleUser.TotalWins + HttpUtility.HtmlEncode(name) + "</b>";
                    else
                        anwserRating += counter + ". " + appleUser.TotalWins + HttpUtility.HtmlEncode(name);
                    counter++;
                }
            }

            if (!topApples.Any(a => a.UserId == _userId))
            {
                var currentUserApple =_appServices.AppleGameDataService.Get(_userId);

                anwserRating += "\n______________________________";
                var counterN = _appServices.AppleGameDataService.GetAll()
                .OrderByDescending(a => a.TotalWins)
                .ToList()
                .FindIndex(a => a.UserId == _userId);
                anwserRating += "\n <b> " + (counterN == -1 ? _appServices.AppleGameDataService.GetAll()?.Count : counterN) + ". " + (currentUserApple?.TotalWins ?? 0) + HttpUtility.HtmlEncode(" 🐱 " + currentPet?.Name ?? currentUser.Username ?? currentUser.FirstName + currentUser.LastName) + "</b>";
            }


            return anwserRating;
        }
        private string GetRanksByGold()
        {
            var topPets = _appServices.PetService.GetAll().Join(_appServices.UserService.GetAll(),
                p => p.UserId,
                u => u.UserId,
                (pet, user) => new {user.UserId, user.Gold, pet.Name, pet.LastUpdateTime})
                .OrderByDescending(p => p.Gold)
                .ThenByDescending(p => p.LastUpdateTime)
                .Take(10); //First 10 top-gold pets

            string anwserRating = "";
            var currentUser = _appServices.UserService.Get(_userId);
            var currentPet = _appServices.PetService.Get(_userId);

            int counter = 1;
            foreach (var pet in topPets)
            {
                var userDB = _appServices.UserService.Get(pet.UserId);
                string name = pet.Name ?? userDB.Username ?? userDB.FirstName + userDB.LastName;
                if (counter == 1)
                {
                    anwserRating += ranksCommandGold + "\n\n";
                    if (currentUser.UserId == userDB.UserId)
                        anwserRating += "<b>" + "💎 " + pet.Gold + " 🐱 " + HttpUtility.HtmlEncode(name) + "</b>";
                    else
                        anwserRating += "💎 " + pet.Gold + " 🐱 " + HttpUtility.HtmlEncode(name);
                    anwserRating += "\n⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯";
                    counter++;
                }
                else
                {
                    anwserRating += "\n";
                    if (currentUser.UserId == userDB.UserId)
                        anwserRating += "<b>" + counter + ". " + pet.Gold + " 🐱 " + HttpUtility.HtmlEncode(name) + "</b>";
                    else
                        anwserRating += counter + ". " + pet.Gold + " 🐱 " + HttpUtility.HtmlEncode(name);
                    counter++;
                }
            }

            if (!topPets.Any(a => a.UserId == currentUser.UserId))
            {
                string name = currentPet.Name ?? currentUser.Username ?? currentUser.FirstName + currentUser.LastName;

                anwserRating += "\n______________________________";
                anwserRating += "\n <b>" +
                    _appServices.PetService.GetAll().Join(_appServices.UserService.GetAll(),
                    p => p.UserId,
                    u => u.UserId,
                    (pet, user) => new { user.UserId, user.Gold, pet.Name, pet.LastUpdateTime })
                    .OrderByDescending(p => p.Gold)
                    .ThenByDescending(p => p.LastUpdateTime)
                    .ToList()
                    .FindIndex(a => a.UserId == currentUser.UserId) + ". " + currentUser.Gold + " 🐱 " + HttpUtility.HtmlEncode(name) + "</b>";
            }

            return anwserRating;
        }

        private void UpdateWorkOnPCButtonToDefault()
        {
            string toSendTextIfTimeOver = string.Format(workCommand,
                                                        new TimesToWait().WorkOnPCToWait.TotalMinutes,
                                                        Rewards.WorkOnPCGoldReward);

            List<CallbackModel> inlineParts = new InlineItems().InlineWork;
            InlineKeyboardMarkup toSendInlineIfTimeOver = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            Log.Debug($"Callbacked UpdateWorkOnPCButtonToDefault for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendTextIfTimeOver, toSendInlineIfTimeOver),
                                                              false);
        }

        private bool CheckStatusIsInactive(Pet petDB, bool IsGoToSleepCommand = false)
        {
            if (petDB.CurrentStatus == (int)CurrentStatus.Sleeping && !IsGoToSleepCommand)
            {
                string denyText = string.Format(denyAccessSleeping);
                _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, denyText, true);

                return true;
            }

            if (petDB.CurrentStatus == (int)CurrentStatus.WorkingOnPC)
            {
                string denyText = string.Format(denyAccessWorking);
                _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, denyText, true);

                return true;
            }

            return false;
        }

        private void SendAlertToUser(string textInAlert, bool isWarning = false)
        {
            Log.Debug($"Sent alert for {_userInfo}");
            _appServices.BotControlService.AnswerCallbackQueryAsync(_callback?.Id, _userId, textInAlert, isWarning);
        }
    }
}
