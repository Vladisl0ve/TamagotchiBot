using MongoDB.Driver.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
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
    public class MenuController : ControllerBase
    {
        private readonly IApplicationServices _appServices;
        private readonly Message _message = null;
        private readonly CallbackQuery _callback = null;
        private readonly long _userId;
        private readonly string _userInfo;
        private readonly CultureInfo _userCulture;

        private MenuController(IApplicationServices services, Message message = null, CallbackQuery callback = null)
        {
            _callback = callback;
            _message = message;
            _userId = callback?.From.Id ?? message.From.Id;
            _appServices = services;
            _userInfo = Extensions.GetLogUser(_appServices.UserService.Get(_userId));
            _userCulture = new CultureInfo(_appServices.UserService.Get(_userId)?.Culture ?? "ru");
        }
        public MenuController(IApplicationServices services, CallbackQuery callback) : this(services, null, callback)
        {
        }

        public MenuController(IApplicationServices services, Message message) : this(services, message, null)
        {
        }

        public async Task ProcessMessage(string customText = null)
        {
            await CommandHandler(customText);
        }

        private async Task CommandHandler(string customText = null)
        {
            string textReceived = customText ?? _message.Text;
            if (textReceived == null)
                return;

            textReceived = textReceived.ToLower();
            if (textReceived.First() == '/')
                textReceived = textReceived.Substring(1);

            var petDB = _appServices.PetService.Get(_userId);
            var userDB = _appServices.UserService.Get(_userId);

            if (textReceived == "language" || GetAllTranslatedAndLowered("languageCommandDescription").Contains(textReceived))
            {
                await ChangeLanguageCmd();
                return;
            }
            if (textReceived == "help" || GetAllTranslatedAndLowered("helpCommandDescription").Contains(textReceived))
            {
                await ShowHelpInfo();
                return;
            }
            if (textReceived == "pet" || GetAllTranslatedAndLowered("petCommandDescription").Contains(textReceived))
            {
                await ShowPetInfo(petDB);
                return;
            }
            if (textReceived == "bathroom" || GetAllTranslatedAndLowered("bathroomCommandDescription").Contains(textReceived))
            {
                await GoToBathroom(petDB);
                return;
            }
            if (textReceived == "kitchen" || GetAllTranslatedAndLowered("kitchenCommandDescription").Contains(textReceived))
            {
                await GoToKitchen(petDB);
                return;
            }
            if (textReceived == "gameroom" || GetAllTranslatedAndLowered("gameroomCommandDescription").Contains(textReceived))
            {
                await GoToGameroom(petDB);
                return;
            }
            if (textReceived == "hospital" || GetAllTranslatedAndLowered("hospitalCommandDescription").Contains(textReceived))
            {
                await GoToHospital(petDB);
                return;
            }
            if (textReceived == "ranks" || GetAllTranslatedAndLowered("ranksCommandDescription").Contains(textReceived))
            {
                await ShowRankingInfo();
                return;
            }
            if (textReceived == "sleep" || GetAllTranslatedAndLowered("sleepCommandDescription").Contains(textReceived))
            {
                await GoToSleep(petDB);
                return;
            }
            if (textReceived == "changelog" || GetAllTranslatedAndLowered("changelogCommandDescription").Contains(textReceived))
            {
                await ShowChangelogsInfo();
                return;
            }
            if (textReceived == "menu" || GetAllTranslatedAndLowered("menuCommandDescription").Contains(textReceived))
            {
                await ShowMenuInfo();
                return;
            }
            if (textReceived == "rename" || GetAllTranslatedAndLowered("renameCommandDescription").Contains(textReceived))
            {
                await RenamePet(petDB);
                return;
            }
            if (textReceived == "work" || GetAllTranslatedAndLowered("workCommandDescription").Contains(textReceived))
            {
                await ShowWorkInfo(petDB);
                return;
            }
            if (textReceived == "reward" || GetAllTranslatedAndLowered("rewardCommandDescription").Contains(textReceived))
            {
                await ShowRewardInfo(userDB);
                return;
            }
            if (textReceived == "referal" || GetAllTranslatedAndLowered("referalCommandDescription").Contains(textReceived))
            {
                await ShowReferalInfo();
                return;
            }
            if (textReceived == "test")
            {
                Log.Debug($"Called /test for {_userInfo}");
                var toSend = new AnswerMessage()
                {
                    Text = nameof(DevelopWarning).UseCulture(_userCulture),
                    StickerId = StickersId.DevelopWarningSticker,
                    ReplyMarkup = null,
                    InlineKeyboardMarkup = null
                };
                await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
                return;
            }

            Log.Debug($"[MESSAGE] '{customText ?? _message.Text}' FROM {_userInfo}");
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
                StartWorkInline(petDb, JobType.WorkingOnPC);
                return;
            }

            if (_callback.Data == new CallbackButtons.WorkCommand().WorkCommandInlineShowTime(default, JobType.WorkingOnPC).CallbackData)
            {
                StartWorkInline(petDb, JobType.WorkingOnPC);
                return;
            }

            if (_callback.Data == new CallbackButtons.WorkCommand().WorkCommandInlineDistributeFlyers.CallbackData)
            {
                StartWorkInline(petDb, JobType.FlyersDistributing);
                return;
            }

            if (_callback.Data == new CallbackButtons.WorkCommand().WorkCommandInlineShowTime(default, JobType.FlyersDistributing).CallbackData)
            {
                StartWorkInline(petDb, JobType.FlyersDistributing);
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

            if (_callback.Data == new CallbackButtons.BathroomCommand().BathroomCommandMakePoo.CallbackData)
            {
                MakePooInline(petDb);
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
        private async Task ShowWorkInfo(Pet petDB)
        {
            Log.Debug($"Called /ShowWorkInfo for {_userInfo}");

            var accessCheck = CheckStatusIsInactiveOrNull(petDB, IsGoToWorkCommand: true);
            if (accessCheck != null)
            {
                Log.Debug($"Pet is busy for {_userInfo}");
                await _appServices.BotControlService.SendAnswerMessageAsync(accessCheck, _userId, false);
                return;
            }

            if (petDB.CurrentStatus == (int)CurrentStatus.Working)
            {
                await ServeWorkCommandPetStillWorking(petDB, (JobType)petDB.CurrentJob);
                return;
            }

            string toSendText = string.Format(nameof(workCommand).UseCulture(_userCulture),
                                       new DateTime(new TimesToWait().WorkOnPCToWait.Ticks).ToString("HH:mm:ss"),
                                       Rewards.WorkOnPCGoldReward,
                                       petDB.Fatigue,
                                       new DateTime(new TimesToWait().FlyersDistToWait.Ticks).ToString("HH:mm:ss"),
                                       Rewards.FlyersDistributingGoldReward);

            List<CallbackModel> inlineParts = new InlineItems().InlineWork;
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.WorkCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.PetWork_Cat,
                InlineKeyboardMarkup = toSendInline,
                ReplyMarkup = new ReplyKeyboardItems().MenuKeyboardMarkup
            };

            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task ShowRewardInfo(User userDB)
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
                    await _appServices.BotControlService.SendAnswerMessageAsync(GetRemainedTimeDailyReward(remainsTime), _userId, false);
                    return;
                }
            }
            else
            {
                toSendText = string.Format(nameof(rewardCommand).UseCulture(_userCulture));

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
                InlineKeyboardMarkup = toSendInline,
                ReplyMarkup = new ReplyKeyboardItems().MenuKeyboardMarkup
            };

            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task ChangeLanguageCmd()
        {
            _appServices.UserService.UpdateLanguage(_userId, null);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.LanguageCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = nameof(ChangeLanguage).UseCulture(_userCulture),
                StickerId = StickersId.ChangeLanguageSticker,
                ReplyMarkup = LanguagesMarkup,
                InlineKeyboardMarkup = null
            };
            Log.Debug($"Called /ChangeLanugage for {_userInfo}");
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task ShowPetInfo(Pet petDB)
        {
            var encodedPetName = HttpUtility.HtmlEncode(petDB.Name);
            encodedPetName = "<b>" + encodedPetName + "</b>";

            string toSendText = string.Format(nameof(petCommand).UseCulture(_userCulture),
                                              encodedPetName,
                                              petDB.HP,
                                              petDB.EXP,
                                              petDB.Level,
                                              petDB.Satiety,
                                              petDB.Fatigue,
                                              Extensions.GetCurrentStatus(petDB.CurrentStatus),
                                              petDB.Joy,
                                              _appServices.UserService.Get(_userId).Gold,
                                              petDB.Hygiene,
                                              petDB.Level * Factors.ExpToLvl);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.PetCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.PetInfo_Cat,
                ReplyMarkup = new ReplyKeyboardItems().MenuKeyboardMarkup,
                InlineKeyboardMarkup = Extensions.InlineKeyboardOptimizer(new InlineItems().InlinePet),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
            };
            Log.Debug($"Called /ShowPetInfo for {_userInfo}");
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private AnswerMessage CheckStatusIsInactiveOrNull(Pet petDB, bool IsGoToSleepCommand = false, bool IsGoToWorkCommand = false)
        {
            if (petDB.CurrentStatus == (int)CurrentStatus.Sleeping && !IsGoToSleepCommand)
            {
                string denyText = string.Format(nameof(denyAccessSleeping).UseCulture(_userCulture));
                return new AnswerMessage()
                {
                    Text = denyText,
                    StickerId = StickersId.PetBusy_Cat
                };
            }

            if (petDB.CurrentStatus == (int)CurrentStatus.Working && !IsGoToWorkCommand)
            {
                string denyText = string.Format(nameof(denyAccessWorking).UseCulture(_userCulture));
                return new AnswerMessage()
                {
                    Text = denyText,
                    StickerId = StickersId.PetBusy_Cat
                };
            }

            return null;
        }
        private async Task GoToBathroom(Pet petDB)
        {
            Log.Debug($"Called /GoToBathroom for {_userInfo}");
            var accessCheck = CheckStatusIsInactiveOrNull(petDB);
            if (accessCheck != null)
            {
                Log.Debug($"Pet is busy for {_userInfo}");
                await _appServices.BotControlService.SendAnswerMessageAsync(accessCheck, _userId, false);
                return;
            }

            string toSendText = string.Format(nameof(bathroomCommand).UseCulture(_userCulture), petDB.Hygiene);

            List<CallbackModel> inlineParts = new InlineItems().InlineHygiene;
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.BathroomCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.PetBathroom_Cat,
                InlineKeyboardMarkup = toSendInline,
                ReplyMarkup = new ReplyKeyboardItems().MenuKeyboardMarkup
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task GoToKitchen(Pet petDB)
        {
            Log.Debug($"Called /GoToKitchen for {_userInfo}");
            var accessCheck = CheckStatusIsInactiveOrNull(petDB);
            if (accessCheck != null)
            {
                Log.Debug($"Pet is busy for {_userInfo}");
                await _appServices.BotControlService.SendAnswerMessageAsync(accessCheck, _userId, false);
                return;
            }

            string toSendText = string.Format(nameof(kitchenCommand).UseCulture(_userCulture), petDB.Satiety, _appServices.UserService.Get(_userId).Gold);

            List<CallbackModel> inlineParts = new InlineItems().InlineFood;
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.KitchenCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.PetKitchen_Cat,
                InlineKeyboardMarkup = toSendInline,
                ReplyMarkup = new ReplyKeyboardItems().MenuKeyboardMarkup
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task GoToGameroom(Pet petDB)
        {
            Log.Debug($"Called /GoToGameroom for {_userInfo}");
            var accessCheck = CheckStatusIsInactiveOrNull(petDB);
            if (accessCheck != null)
            {
                Log.Debug($"Pet is busy for {_userInfo}");
                await _appServices.BotControlService.SendAnswerMessageAsync(accessCheck, _userId, false);
                return;
            }

            string toSendText = string.Format(nameof(gameroomCommand).UseCulture(_userCulture),
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
                InlineKeyboardMarkup = toSendInline,
                ReplyMarkup = new ReplyKeyboardItems().MenuKeyboardMarkup
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task GoToHospital(Pet petDB)
        {
            Log.Debug($"Called /GoToHospital for {_userInfo}");
            var accessCheck = CheckStatusIsInactiveOrNull(petDB);
            if (accessCheck != null)
            {
                Log.Debug($"Pet is busy for {_userInfo}");
                await _appServices.BotControlService.SendAnswerMessageAsync(accessCheck, _userId, false);
                return;
            }

            string commandHospital =  petDB.HP switch
            {
                >= 80 => nameof(hospitalCommandHighHp).UseCulture(_userCulture),
                >20 and < 80 => nameof(hospitalCommandMidHp).UseCulture(_userCulture),
                _ => nameof(hospitalCommandLowHp).UseCulture(_userCulture)
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
                InlineKeyboardMarkup = toSendInline,
                ReplyMarkup = new ReplyKeyboardItems().MenuKeyboardMarkup
            };

            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task ShowRankingInfo()
        {
            Log.Debug($"Called /ShowRankingInfo for {_userInfo}");

            var anwserRating = GetRanksByLevel();

            if (anwserRating == null)
                return;

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.RanksCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = anwserRating,
                StickerId = StickersId.PetRanks_Cat,
                InlineKeyboardMarkup = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineRanks, 3),
                ReplyMarkup = new ReplyKeyboardItems().MenuKeyboardMarkup,
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
            };

            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task GoToSleep(Pet petDB)
        {
            Log.Debug($"Called /GoToSleep for {_userInfo}");

            var accessCheck = CheckStatusIsInactiveOrNull(petDB, true);
            if (accessCheck != null)
            {
                Log.Debug($"Pet is busy for {_userInfo}");
                await _appServices.BotControlService.SendAnswerMessageAsync(accessCheck, _userId, false);
                return;
            }

            string toSendText = string.Format(nameof(sleepCommand).UseCulture(_userCulture),
                                              petDB.Name,
                                              petDB.Fatigue,
                                              Extensions.GetCurrentStatus(petDB.CurrentStatus));

            InlineKeyboardMarkup toSendInline;
            if (petDB.CurrentStatus == (int)CurrentStatus.Sleeping)
            {
                var ticksToWait = (petDB.ToWakeUpTime - DateTime.UtcNow).Ticks;
                string timeToWaitStr = string.Format(nameof(sleepCommandInlineShowTime).UseCulture(_userCulture), new DateTime().AddTicks(ticksToWait).ToString("HH:mm:ss"));

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
                            Text = nameof(sleepCommandInlinePutToSleep).UseCulture(_userCulture),
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
                InlineKeyboardMarkup = toSendInline,
                ReplyMarkup = new ReplyKeyboardItems().MenuKeyboardMarkup
            };

            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task ShowChangelogsInfo()
        {
            string linkToDiscussChat = "https://t.me/news_virtualpetbot";
            string toSendText = string.Format(nameof(changelogCommand).UseCulture(_userCulture), linkToDiscussChat);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.ChangelogCommandSticker,
                InlineKeyboardMarkup = new InlineKeyboardButton(nameof(ChangelogGoToDicussChannelButton).UseCulture(_userCulture))
                {
                    Url = linkToDiscussChat
                },
                ReplyMarkup = new ReplyKeyboardItems().MenuKeyboardMarkup
            };
            Log.Debug($"Called /ShowChangelogsInfo for {_userInfo}");
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task ShowHelpInfo()
        {
            string toSendText = string.Format(nameof(helpCommand).UseCulture(_userCulture));

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.HelpCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.HelpCommandSticker,
                ReplyMarkup = new ReplyKeyboardItems().MenuKeyboardMarkup
            };
            Log.Debug($"Called /ShowHelpInfo for {_userInfo}");
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task ShowReferalInfo()
        {
            Log.Debug($"Called /ShowReferalInfo for {_userInfo}");
            var botUsername = (await _appServices.SInfoService.GetBotUserInfo()).Username;

            var refAmounts = _appServices.ReferalInfoService.GetDoneRefsAmount(_userId);
            var goldByRef = Rewards.ReferalAdded * refAmounts;
            var refLink = Extensions.GetReferalLink(_userId, botUsername);
            string toSendText = string.Format(nameof(referalCommand).UseCulture(_userCulture), refAmounts, goldByRef, refLink);

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
                ReplyMarkup = new ReplyKeyboardItems().MenuKeyboardMarkup,
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task ShowMenuInfo()
        {
            Log.Debug($"Called /ShowMenuInfo for {_userInfo}");

            string toSendText = string.Format(nameof(menuCommand).UseCulture(_userCulture));

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.MenuCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.MenuCommandSticker,
                ReplyMarkup = new ReplyKeyboardItems().MenuKeyboardMarkup,
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task RenamePet(Pet petDB)
        {
            Log.Debug($"Called /RenamePet for {_userInfo}");

            if (_appServices.BannedUsersService.GetAll().Exists(bs => bs.UserId == _userId && bs.IsRenameBanned))
            {
                Log.Debug($"Banned for renaming for {_userInfo}");

                string toSendTextBan = string.Format(nameof(renameBannedCommand).UseCulture(_userCulture));

                var audF = _appServices.AllUsersDataService.Get(_userId);
                audF.RenameCommandCounter++;
                _appServices.AllUsersDataService.Update(audF);

                var toSend = new AnswerMessage() { Text = toSendTextBan, StickerId = StickersId.BannedSticker };
                await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
                return;
            }

            string toSendText = string.Format(nameof(renameCommand).UseCulture(_userCulture));

            _appServices.MetaUserService.UpdateIsPetNameAskedOnRename(_userId, true);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.RenameCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSendFinal = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.RenamePetSticker,
                ReplyMarkup = new ReplyKeyboardRemove()
            };

            await _appServices.BotControlService.SendAnswerMessageAsync(toSendFinal, _userId, false);
        }
        #endregion

        #region Inline Answers
        private void ShowBasicInfoInline(Pet petDB)
        {
            var encodedPetName = HttpUtility.HtmlEncode(petDB.Name);
            encodedPetName = "<b>" + encodedPetName + "</b>";
            var userDB = _appServices.UserService.Get(_userId);
            string toSendText = string.Format(nameof(petCommand).UseCulture(_userCulture),
                                              encodedPetName,
                                              petDB.HP,
                                              petDB.EXP,
                                              petDB.Level,
                                              petDB.Satiety,
                                              petDB.Fatigue,
                                              Extensions.GetCurrentStatus(petDB.CurrentStatus),
                                              petDB.Joy,
                                              userDB.Gold,
                                              petDB.Hygiene,
                                              petDB.Level * Factors.ExpToLvl);

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
            string toSendText = string.Format(nameof(petCommandMoreInfo1).UseCulture(_userCulture), encodedPetName, petDB.BirthDateTime, _appServices.ReferalInfoService.GetDoneRefsAmount(_userId));
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

            string answerLocal = string.Format(nameof(tooManyStarvingCommand).UseCulture(_userCulture));
            _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, answerLocal, true);
        }
        private void NotEnoughGold()
        {
            Log.Debug($"Sent alert NotEnoughGold for {_userInfo}");

            string anwserLocal = string.Format(nameof(goldNotEnough).UseCulture(_userCulture));
            _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwserLocal, true);
        }

        private void PetIsTooTired(JobType job = JobType.None)
        {
            Log.Debug($"Sent alert PetIsTooTired for {_userInfo}");

            string anwserLocal = job switch
            {
                JobType.WorkingOnPC => string.Format(nameof(tooTiredForJobPC).UseCulture(_userCulture), Factors.WorkOnPCFatigueFactor),
                JobType.FlyersDistributing => string.Format(nameof(tooTiredForJobFlyers).UseCulture(_userCulture), Factors.FlyersDistributingFatigueFactor),
                _ => string.Format(nameof(tooTiredText).UseCulture(_userCulture))
            };
            _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwserLocal, true);
        }

        private void PetIsFullOfJoy()
        {
            Log.Debug($"Sent alert PetIsFullOfJoy for {_userInfo}");

            string anwserLocal = string.Format(nameof(PetIsFullOfJoyText).UseCulture(_userCulture));
            _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwserLocal, true);
        }

        private void TakeShowerInline(Pet petDB)
        {
            if (petDB.Hygiene >= 100)
            {
                SendAlertToUser(nameof(PetIsCleanEnoughAlert).UseCulture(_userCulture), true);
                return;
            }

            var newHygiene = petDB.Hygiene + HygieneFactors.ShowerFactor;
            newHygiene = newHygiene > 100 ? 100 : newHygiene;

            _appServices.PetService.UpdateHygiene(_userId, newHygiene);

            string anwser = string.Format(nameof(PetHygieneAnwserCallback).UseCulture(_userCulture), HygieneFactors.ShowerFactor);
            SendAlertToUser(anwser);

            string toSendText = string.Format(nameof(bathroomCommand).UseCulture(_userCulture), newHygiene);
            List<CallbackModel> inlineParts = new InlineItems().InlineHygiene;
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            Log.Debug($"Callbacked TakeShowerInline for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }
        private void MakePooInline(Pet petDB)
        {
            if (petDB.Hygiene >= 100)
            {
                SendAlertToUser(nameof(PetDoesntWantToPoo).UseCulture(_userCulture), true);
                return;
            }

            var newHygiene = petDB.Hygiene + HygieneFactors.PoopFactor;
            newHygiene = newHygiene > 100 ? 100 : newHygiene;

            _appServices.PetService.UpdateHygiene(_userId, newHygiene);

            string anwser = string.Format(nameof(PetHygieneAnwserCallback).UseCulture(_userCulture), HygieneFactors.PoopFactor);
            SendAlertToUser(anwser);

            string toSendText = string.Format(nameof(bathroomCommand).UseCulture(_userCulture), newHygiene);
            List<CallbackModel> inlineParts = new InlineItems().InlineHygiene;
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            Log.Debug($"Callbacked PoopInline for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }
        private void TeethInline(Pet petDB)
        {
            if (petDB.Hygiene >= 100)
            {
                SendAlertToUser(nameof(PetIsCleanEnoughAlert).UseCulture(_userCulture), true);
                return;
            }

            var newHygiene = petDB.Hygiene + HygieneFactors.TeethFactor;
            newHygiene = newHygiene > 100 ? 100 : newHygiene;

            _appServices.PetService.UpdateHygiene(_userId, newHygiene);

            string anwser = string.Format(nameof(PetHygieneAnwserCallback).UseCulture(_userCulture), HygieneFactors.TeethFactor);
            SendAlertToUser(anwser);

            string toSendText = string.Format(nameof(bathroomCommand).UseCulture(_userCulture), newHygiene);
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

            string anwser = string.Format(nameof(PetFeedingAnwserCallback).UseCulture(_userCulture), (int)FoodFactors.BreadHungerFactor);
            SendAlertToUser(anwser);

            string toSendText = string.Format(nameof(kitchenCommand).UseCulture(_userCulture), newSatiety, newGold);
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

            SendAlertToUser(string.Format(nameof(PetFeedingAnwserCallback).UseCulture(_userCulture), (int)FoodFactors.RedAppleHungerFactor));

            string toSendText = string.Format(nameof(kitchenCommand).UseCulture(_userCulture), newSatiety, newGold);
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

            string anwser = string.Format(nameof(PetFeedingAnwserCallback).UseCulture(_userCulture), (int)FoodFactors.ChocolateHungerFactor);
            SendAlertToUser(anwser);

            string toSendText = string.Format(nameof(kitchenCommand).UseCulture(_userCulture), newSatiety, newGold);
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

            string anwser = string.Format(nameof(PetFeedingAnwserCallback).UseCulture(_userCulture), (int)FoodFactors.LollipopHungerFactor);
            SendAlertToUser(anwser);

            string toSendText = string.Format(nameof(kitchenCommand).UseCulture(_userCulture), newSatiety, newGold);
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineFood, 3);

            Log.Debug($"Callbacked FeedWithLollipopInline for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }

        private void PutToSleepInline(Pet petDB)
        {
            Pet petResult = Pet.Clone(petDB);

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
            string toSendText = string.Format(nameof(sleepCommand).UseCulture(_userCulture), petDB.Name, petDB.Fatigue, Extensions.GetCurrentStatus(petDB.CurrentStatus));
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
            _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, nameof(PetSleepingDoesntWantYetAnwserCallback).UseCulture(_userCulture));
            string sendTxt = string.Format(nameof(sleepCommand).UseCulture(_userCulture), petDB.Name, petDB.Fatigue, Extensions.GetCurrentStatus(petDB.CurrentStatus));

            InlineKeyboardMarkup toSendInlineWhileActive =
                Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
                {
                    new CallbackModel()
                    {
                        Text = nameof(sleepCommandInlinePutToSleep).UseCulture(_userCulture),
                        CallbackData = "sleepCommandInlinePutToSleep"
                    }
                });

            Log.Debug($"Callbacked DeclineSleepingInline for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback.Message?.MessageId ?? 0,
                                                              new AnswerCallback(sendTxt, toSendInlineWhileActive),
                                                              false);
        }
        private void UpdateSleepingInline(Pet petDB)
        {
            _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, nameof(sleepCommandInlinePutToSleep).UseCulture(_userCulture));

            string toSendText = string.Format(nameof(sleepCommand).UseCulture(_userCulture), petDB.Name, petDB.Fatigue, Extensions.GetCurrentStatus(petDB.CurrentStatus));

            var timeToWait = petDB.ToWakeUpTime - DateTime.UtcNow;
            timeToWait = timeToWait < TimeSpan.Zero ? default : timeToWait;

            InlineKeyboardMarkup toSendInline =
                Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
                {
                    new CallbackButtons.SleepCommand().SleepCommandInlinePutToSleep(timeToWait)
                });

            Log.Debug($"Callbacked UpdateSleepingInline for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback.Message?.MessageId ?? 0,
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

            string anwser = string.Format(nameof(PetPlayingAnwserCallback).UseCulture(_userCulture), Factors.CardGameFatigueFactor);
            SendAlertToUser(anwser);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.CardsPlayedCounter++;
            _appServices.AllUsersDataService.Update(aud);

            string toSendText = string.Format(nameof(gameroomCommand).UseCulture(_userCulture),
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

            string anwser = string.Format(nameof(PetPlayingAnwserCallback).UseCulture(_userCulture), Factors.DiceGameJoyFactor);
            _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwser);

            string toSendText = string.Format(nameof(gameroomCommand).UseCulture(_userCulture),
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
                                                              _callback.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }
        private void StartWorkInline(Pet petDB, JobType jobType)
        {
            if (petDB == null)
                return;

            if (petDB.CurrentStatus == (int)CurrentStatus.Active)
            {
                if (jobType == JobType.WorkingOnPC)
                {
                    StartWorkOnPC(petDB);
                    return;
                }

                if (jobType == JobType.FlyersDistributing)
                {
                    StartJobFlyers(petDB);
                    return;
                }
            }
            else if (petDB.CurrentStatus == (int)CurrentStatus.Working)
            {
                if (petDB.CurrentJob == (int)JobType.WorkingOnPC)
                {
                    ServeWorkOnPC(petDB);
                    return;
                }
                if (petDB.CurrentJob == (int)JobType.FlyersDistributing)
                {
                    ServeJobFlyers(petDB);
                    return;
                }
            }
        }

        private void GetDailyRewardInline(User userDB)
        {
            var dateTimeWhenOver = userDB.GotDailyRewardTime.Add(new TimesToWait().DailyRewardToWait);
            if (dateTimeWhenOver > DateTime.UtcNow)
            {
                Log.Debug($"Callbacked GetDailyRewardInline (still waiting) for {_userInfo}");
                _appServices.BotControlService.SendAnswerCallback(_userId,
                                                                  _callback.Message?.MessageId ?? 0,
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

            string anwser = string.Format(nameof(DailyRewardAnwserCallback).UseCulture(_userCulture), Rewards.DailyGoldReward);
            SendAlertToUser(anwser, true);

            Log.Debug($"Callbacked GetDailyRewardInline (default) for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              ShowRemainedTimeDailyRewardCallback(new TimeSpan(23, 59, 59), false),
                                                              false);
            return;
        }

        private AnswerMessage GetRemainedTimeWork(TimeSpan remainedTime, JobType job)
        {
            AnswerMessage result = job switch
            {
                JobType.WorkingOnPC => new AnswerMessage()
                {
                    InlineKeyboardMarkup = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
                    {
                        new CallbackButtons.WorkCommand().WorkCommandInlineShowTime(remainedTime, JobType.WorkingOnPC)
                    }),
                    Text = string.Format(nameof(workCommandPCWorking).UseCulture(_userCulture)),
                    StickerId = StickersId.PetWorkOnPC_Cat
                },
                //DEFAULT, also Flyers job
                _ => new AnswerMessage()
                {
                    InlineKeyboardMarkup = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
                    {
                        new CallbackButtons.WorkCommand().WorkCommandInlineShowTime(remainedTime, JobType.FlyersDistributing)
                    }),
                    Text = string.Format(nameof(workCommandFlyersWorking).UseCulture(_userCulture)),
                    StickerId = StickersId.PetFlyersJob_Cat
                },
            };
            return result;
        }
        private AnswerCallback ShowRemainedTimeWorkOnPCCallback(TimeSpan remainedTime = default)
        {
            InlineKeyboardMarkup toSendInline;
            string toSendText;

            if (remainedTime == default)
                remainedTime = new TimeSpan(0);

            string anwser = string.Format(nameof(workCommandPCWorking).UseCulture(_userCulture));
            _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwser);

            toSendText = string.Format(nameof(workCommandPCWorking).UseCulture(_userCulture));
            toSendInline = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
            {
                new CallbackButtons.WorkCommand().WorkCommandInlineShowTime(remainedTime, JobType.WorkingOnPC)
            });

            return new AnswerCallback(toSendText, toSendInline);
        }
        private AnswerCallback ShowRemainedTimeJobFlyersCallback(TimeSpan remainedTime = default)
        {
            InlineKeyboardMarkup toSendInline;
            string toSendText;

            if (remainedTime == default)
                remainedTime = new TimeSpan(0);

            string anwser = string.Format(nameof(workCommandFlyersWorking).UseCulture(_userCulture));
            _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwser);

            toSendText = string.Format(nameof(workCommandFlyersWorking).UseCulture(_userCulture));
            toSendInline = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
            {
                new CallbackButtons.WorkCommand().WorkCommandInlineShowTime(remainedTime, JobType.FlyersDistributing)
            });

            return new AnswerCallback(toSendText, toSendInline);
        }

        private AnswerMessage GetRemainedTimeDailyReward(TimeSpan remainedTime)
        {
            InlineKeyboardMarkup toSendInline;
            string toSendText;

            toSendText = string.Format(nameof(rewardCommandDailyRewardGotten).UseCulture(_userCulture));
            string inlineStr = string.Format(nameof(rewardCommandDailyRewardInlineShowTime).UseCulture(_userCulture), new DateTime(remainedTime.Ticks).ToString("HH:mm:ss"));

            toSendInline = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
                    {
                        new CallbackModel()
                        {
                            Text = inlineStr,
                            CallbackData = "rewardCommandDailyRewardInlineShowTime"
                        }
                    });

            return new AnswerMessage() { InlineKeyboardMarkup = toSendInline, Text = toSendText, StickerId = StickersId.DailyRewardSticker, ReplyMarkup = new ReplyKeyboardItems().MenuKeyboardMarkup };
        }
        private AnswerCallback ShowRemainedTimeDailyRewardCallback(TimeSpan remainedTime = default, bool isAlert = false)
        {
            InlineKeyboardMarkup toSendInline;
            string toSendText;

            if (remainedTime == default)
                remainedTime = new TimeSpan(0);

            if (isAlert)
            {
                string anwser = string.Format(nameof(rewardCommandDailyRewardGotten).UseCulture(_userCulture));
                SendAlertToUser(anwser, isAlert);
            }

            toSendText = string.Format(nameof(rewardCommandDailyRewardGotten).UseCulture(_userCulture));

            toSendInline = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
            {
                new CallbackButtons.RewardsCommand().RewardCommandDailyRewardInlineShowTime(remainedTime)
            });

            return new AnswerCallback(toSendText, toSendInline);
        }

        private void CureWithPill(Pet petDB)
        {
            Log.Debug($"Callbacked CureWithPill for {_userInfo}");
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

            string anwser = string.Format(nameof(PetCuringAnwserCallback).UseCulture(_userCulture), Factors.PillHPFactor, Factors.PillJoyFactor);
            SendAlertToUser(anwser, true);

            string commandHospital =  newHP switch
            {
                >= 80 => nameof(hospitalCommandHighHp).UseCulture(_userCulture),
                >20 and < 80 => nameof(hospitalCommandMidHp).UseCulture(_userCulture),
                _ => nameof(hospitalCommandLowHp).UseCulture(_userCulture)
            };

            string toSendText = string.Format(commandHospital, newHP);
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineHospital);

            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }

        private void ShowRanksGold()
        {
            Log.Debug($"Callbacked ShowRanksGold for {_userInfo}");
            string toSendText = GetRanksByGold();
            if (toSendText == null)
                return;

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineRanks);

            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline, Telegram.Bot.Types.Enums.ParseMode.Html),
                                                              false);
        }
        private void ShowRanksApples()
        {
            Log.Debug($"Callbacked ShowRanksApples for {_userInfo}");
            string toSendText = GetRanksByApples();
            if (toSendText == null)
                return;

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineRanks);

            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline, Telegram.Bot.Types.Enums.ParseMode.Html),
                                                              false);
        }
        private void ShowRanksLevel()
        {
            Log.Debug($"Callbacked ShowRanksLevel for {_userInfo}");
            string toSendText = GetRanksByLevel();
            if (toSendText == null)
                return;

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineRanks);
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline, Telegram.Bot.Types.Enums.ParseMode.Html),
                                                              false);
        }

        #endregion

        private string GetRanksByLevel()
        {
            try
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
                        anwserRating += nameof(ranksCommand).UseCulture(_userCulture) + "\n\n";
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
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return null;
            }
        }
        private string GetRanksByApples()
        {
            try
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

                        anwserRating += nameof(ranksCommandApples).UseCulture(_userCulture) + "\n\n";
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
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return null;
            }
        }
        private string GetRanksByGold()
        {
            try
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
                        anwserRating += nameof(ranksCommandGold).UseCulture(_userCulture) + "\n\n";
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
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return null;
            }
        }

        private void UpdateWorkOnPCButtonToDefault(Pet petDB)
        {
            string toSendTextIfTimeOver = string.Format(nameof(workCommand).UseCulture(_userCulture),
                                                        new DateTime(new TimesToWait().WorkOnPCToWait.Ticks).ToString("HH:mm:ss"),
                                                        Rewards.WorkOnPCGoldReward,
                                                        petDB.Fatigue,
                                                        new DateTime(new TimesToWait().FlyersDistToWait.Ticks).ToString("HH:mm:ss"),
                                                        Rewards.FlyersDistributingGoldReward);

            List<CallbackModel> inlineParts = new InlineItems().InlineWork;
            InlineKeyboardMarkup toSendInlineIfTimeOver = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            Log.Debug($"Callbacked UpdateWorkOnPCButtonToDefault for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendTextIfTimeOver, toSendInlineIfTimeOver),
                                                              false);
        }
        private async Task ServeWorkCommandPetStillWorking(Pet petDB, JobType job)
        {
            TimeSpan timeToWait = job switch
            {
                JobType.WorkingOnPC => new TimesToWait().WorkOnPCToWait,
                JobType.FlyersDistributing => new TimesToWait().FlyersDistToWait,
                _ => new TimeSpan(0)
            };
            TimeSpan remainsTime = timeToWait - (DateTime.UtcNow - petDB.StartWorkingTime);

            //if _callback handled when pet is working
            if (remainsTime > TimeSpan.Zero)
            {
                Log.Debug($"Pet is working for {_userInfo}");
                await _appServices.BotControlService.SendAnswerMessageAsync(GetRemainedTimeWork(remainsTime, job), _userId, false);
            }
        }

        private bool CheckStatusIsInactive(Pet petDB, bool IsGoToSleepCommand = false)
        {
            if (petDB.CurrentStatus == (int)CurrentStatus.Sleeping && !IsGoToSleepCommand)
            {
                string denyText = string.Format(nameof(denyAccessSleeping).UseCulture(_userCulture));
                _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, denyText, true);

                return true;
            }

            if (petDB.CurrentStatus == (int)CurrentStatus.Working)
            {
                string denyText = string.Format(nameof(denyAccessWorking).UseCulture(_userCulture));
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
        private void EditMessageToDefaultWorkCommand(Pet petDB)
        {
            petDB.CurrentStatus = (int)CurrentStatus.Active;
            _appServices.PetService.UpdateCurrentStatus(_userId, petDB.CurrentStatus);

            string toSendTextIfTimeOver = string.Format(nameof(workCommand).UseCulture(_userCulture),
                                                        new DateTime(new TimesToWait().WorkOnPCToWait.Ticks).ToString("HH:mm:ss"),
                                                        Rewards.WorkOnPCGoldReward,
                                                        petDB.Fatigue,
                                                        new DateTime(new TimesToWait().FlyersDistToWait.Ticks).ToString("HH:mm:ss"),
                                                        Rewards.FlyersDistributingGoldReward);
            List<CallbackModel> inlineParts = new InlineItems().InlineWork;
            InlineKeyboardMarkup toSendInlineIfTimeOver = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            Log.Debug($"Callbacked ShowDefaultWorkCommand (work is over) for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendTextIfTimeOver, toSendInlineIfTimeOver),
                                                              false);
        }
        private void StartWorkOnPC(Pet petDB)
        {
            InlineKeyboardMarkup toSendInline;
            string toSendText;

            if (_callback.Data == new CallbackButtons.WorkCommand().WorkCommandInlineShowTime(default, JobType.WorkingOnPC).CallbackData)
            {
                UpdateWorkOnPCButtonToDefault(petDB);
                return;
            }

            var newFatigue = petDB.Fatigue + Factors.WorkOnPCFatigueFactor;
            if (newFatigue > 100)
            {
                PetIsTooTired(JobType.WorkingOnPC);
                return;
            }

            _appServices.PetService.UpdateFatigue(_userId, newFatigue);
            _appServices.UserService.UpdateGold(_userId, _appServices.UserService.Get(_userId).Gold + Rewards.WorkOnPCGoldReward);
            _appServices.PetService.UpdateCurrentStatus(_userId, (int)CurrentStatus.Working);
            _appServices.PetService.UpdateCurrentJob(_userId, (int)JobType.WorkingOnPC);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.GoldEarnedCounter += Rewards.WorkOnPCGoldReward;
            aud.WorkOnPCCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var startWorkingTime = DateTime.UtcNow;
            _appServices.PetService.UpdateStartWorkingTime(_userId, startWorkingTime);

            string anwser = string.Format(nameof(PetWorkingAnswerCallback).UseCulture(_userCulture), Factors.WorkOnPCFatigueFactor, Rewards.WorkOnPCGoldReward);
            SendAlertToUser(anwser, true);

            toSendText = string.Format(nameof(workCommandPCWorking).UseCulture(_userCulture));

            TimeSpan remainsTime = new TimesToWait().WorkOnPCToWait - (DateTime.UtcNow - startWorkingTime);

            toSendInline = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
                {
                    new CallbackButtons.WorkCommand().WorkCommandInlineShowTime(remainsTime, JobType.WorkingOnPC)
                });

            Log.Debug($"Callbacked StartWorkOnPC for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }
        private void ServeWorkOnPC(Pet petDB)
        {
            TimeSpan remainsTime = new TimesToWait().WorkOnPCToWait - (DateTime.UtcNow - petDB.StartWorkingTime);

            //if _callback handled when time of work is over
            if (remainsTime <= TimeSpan.Zero)
            {
                EditMessageToDefaultWorkCommand(petDB);
                return;
            }

            Log.Debug($"Callbacked ServeWorkOnPC (still working) for {_userInfo}");

            //if _callback handled when pet is still working
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              ShowRemainedTimeWorkOnPCCallback(remainsTime),
                                                              false);
        }
        private void StartJobFlyers(Pet petDB)
        {
            InlineKeyboardMarkup toSendInline;
            string toSendText;

            if (_callback.Data == new CallbackButtons.WorkCommand().WorkCommandInlineShowTime(default, JobType.FlyersDistributing).CallbackData)
            {
                UpdateWorkOnPCButtonToDefault(petDB);
                return;
            }

            var newFatigue = petDB.Fatigue + Factors.FlyersDistributingFatigueFactor;
            if (newFatigue > 100)
            {
                PetIsTooTired(JobType.FlyersDistributing);
                return;
            }

            _appServices.PetService.UpdateFatigue(_userId, newFatigue);
            _appServices.UserService.UpdateGold(_userId, _appServices.UserService.Get(_userId).Gold + Rewards.FlyersDistributingGoldReward);
            _appServices.PetService.UpdateCurrentStatus(_userId, (int)CurrentStatus.Working);
            _appServices.PetService.UpdateCurrentJob(_userId, (int)JobType.FlyersDistributing);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.GoldEarnedCounter += Rewards.FlyersDistributingGoldReward;
            aud.WorkFlyersCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var startWorkingTime = DateTime.UtcNow;
            _appServices.PetService.UpdateStartWorkingTime(_userId, startWorkingTime);

            string anwser = string.Format(nameof(PetWorkingAnswerCallback).UseCulture(_userCulture), Factors.FlyersDistributingFatigueFactor, Rewards.FlyersDistributingGoldReward);
            SendAlertToUser(anwser, true);

            toSendText = string.Format(nameof(workCommandFlyersWorking).UseCulture(_userCulture));

            TimeSpan remainsTime = new TimesToWait().FlyersDistToWait - (DateTime.UtcNow - startWorkingTime);

            toSendInline = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
                {
                    new CallbackButtons.WorkCommand().WorkCommandInlineShowTime(remainsTime, JobType.FlyersDistributing)
                });

            Log.Debug($"Callbacked StartJobFlyers for {_userInfo}");
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }
        private void ServeJobFlyers(Pet petDB)
        {
            TimeSpan remainsTime = new TimesToWait().FlyersDistToWait - (DateTime.UtcNow - petDB.StartWorkingTime);

            //if _callback handled when time of work is over
            if (remainsTime <= TimeSpan.Zero)
            {
                EditMessageToDefaultWorkCommand(petDB);
                return;
            }

            Log.Debug($"Callbacked ServeJobFlyers (still working) for {_userInfo}");

            //if _callback handled when pet is still working
            _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              ShowRemainedTimeJobFlyersCallback(remainsTime),
                                                              false);
        }
    }
}
