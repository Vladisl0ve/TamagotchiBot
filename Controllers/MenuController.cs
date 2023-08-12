using MongoDB.Driver.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TamagotchiBot.Models;
using TamagotchiBot.Models.Answers;
using TamagotchiBot.Models.Mongo;
using TamagotchiBot.Services;
using TamagotchiBot.Services.Interfaces;
using TamagotchiBot.Services.Mongo;
using TamagotchiBot.UserExtensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static TamagotchiBot.Resources.Resources;
using static TamagotchiBot.UserExtensions.Constants;
using Extensions = TamagotchiBot.UserExtensions.Extensions;

namespace TamagotchiBot.Controllers
{
    public class MenuController
    {
        private readonly IApplicationServices _appServices;
        private readonly UserService _userService;
        private readonly PetService _petService;
        private readonly ChatService _chatService;
        private readonly BotControlService _bcService;
        private readonly AllUsersDataService _allUsersService;
        private readonly BannedUsersService _bannedService;
        private readonly AdsProducersService _adsProducersService;
        private readonly AppleGameDataService _appleGameDataService;
        private readonly Message _message = null;
        private readonly CallbackQuery _callback = null;
        private readonly long _userId;

        private MenuController(IApplicationServices services, Message message = null, CallbackQuery callback = null)
        {
            _callback = callback;
            _message = message;
            _userId = callback?.From.Id ?? message.From.Id;

            _appServices = services;
            _userService = services.UserService;
            _petService = services.PetService;
            _chatService = services.ChatService;
            _bcService = services.BotControlService;
            _allUsersService = services.AllUsersDataService;
            _bannedService = services.BannedUsersService;
            _appleGameDataService = services.AppleGameDataService;
            _adsProducersService = services.AdsProducersService;

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

            var aud = _allUsersService.Get(user.Id);
            if (aud == null) //if new
            {
                _allUsersService.Create(new AllUsersData()
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
            aud.Culture = _userService.Get(user.Id)?.Culture ?? "ru";
            aud.LastMessage = message?.Text ?? string.Empty;
            aud.MessageCounter = message == null ? aud.MessageCounter : aud.MessageCounter + 1;
            aud.CallbacksCounter = callback == null ? aud.CallbacksCounter : aud.CallbacksCounter + 1;
            _allUsersService.Update(aud);
        }

        public Answer ProcessMessage()
        {
            return CommandHandler();
        }

        private void UpdateIndicators()
        {
            Telegram.Bot.Types.User userFromMsg = _message?.From ?? _callback?.From;
            var petDB = _appServices.PetService.Get(_userId);
            var petResult = new Pet().Clone(petDB);

            if (userFromMsg == null || petDB == null)
                return;

            int minuteCounter = (int)(DateTime.UtcNow - petDB.LastUpdateTime).TotalMinutes;

            //EXP
            petResult.EXP = UpdateIndicatorEXP(minuteCounter, petDB);

            //Satiety & HP
            var satietyHp = UpdateIndicatorSatietyAndHP(minuteCounter, petDB);
            petResult.Satiety = satietyHp.Item1;
            petResult.HP = satietyHp.Item2;

            //Joy
            petResult.Joy = UpdateIndicatorJoy(minuteCounter, petDB);

            //Fatigue
            petResult.Fatigue = UpdateIndicatorFatigue(minuteCounter, petDB);

            //Hygiene
            petResult.Hygiene = UpdateIndicatorHygiene(minuteCounter, petDB);

            //Sleeping
            if (petDB.CurrentStatus == (int)CurrentStatus.Sleeping)
            {
                var sleepResult =  UpdateIndicatorSleeping(petDB);
                var currentStatus = sleepResult.Item1;
                var currentFatigue = sleepResult.Item2;

                petResult.CurrentStatus = (int)currentStatus;
                petResult.Fatigue = currentStatus != CurrentStatus.Active ? petResult.Fatigue : currentFatigue;
            }

            //Work
            if (petDB.CurrentStatus == (int)CurrentStatus.WorkingOnPC)
                petResult.CurrentStatus = (int)UpdateIndicatorWork(petDB);

            petResult.LastUpdateTime = DateTime.UtcNow;
            _appServices.PetService.Update(userFromMsg.Id, petResult);
        }

        private CurrentStatus UpdateIndicatorWork(Pet petDB)
        {
            TimeSpan remainsTime = new TimesToWait().WorkOnPCToWait - (DateTime.UtcNow - petDB.StartWorkingTime);

            //if callback handled when time of work is over
            if (remainsTime <= TimeSpan.Zero)
                return CurrentStatus.Active;

            return CurrentStatus.WorkingOnPC;
        }

        public Answer CommandHandler()
        {
            string textReceived = _message.Text;
            if (textReceived == null)
                return null;

            textReceived = textReceived.ToLower();

            UpdateIndicators();

            /*            if (pet != null && IsPetGone())
                        {
                            DeleteDataOfUser();
                            bot.SendChatActionAsync(user.UserId, Telegram.Bot.Types.Enums.ChatAction.Typing);
                            return GetFarewellAnswer(pet.Name, user.FirstName ?? user.Username);
                        }
            */

            var petDB = _appServices.PetService.Get(_userId);

            if (textReceived == "/language")
                return ChangeLanguage();
            if (textReceived == "/help")
                return ShowHelpInfo();

            if (textReceived == "/pet")
                return ShowPetInfo(petDB);
            if (textReceived == "/bathroom")
                return GoToBathroom(petDB);
            if (textReceived == "/kitchen")
                return GoToKitchen(petDB);
            if (textReceived == "/gameroom")
                return GoToGameroom(petDB);
            if (textReceived == "/hospital")
                return GoToHospital(petDB);
            if (textReceived == "/ranks")
                return ShowRankingInfo();
            if (textReceived == "/sleep")
                return GoToSleep(petDB);
            if (textReceived == "/test")
            {
                return new Answer()
                {
                    Text = DevelopWarning,
                    StickerId = StickersId.DevelopWarningSticker,
                    ReplyMarkup = null,
                    InlineKeyboardMarkup = null
                };
            }
            if (textReceived == "/menu")
                return ShowMenuInfo();
            if (textReceived == "/rename")
                return RenamePet(petDB);
            if (textReceived == "/work")
                return ShowWorkInfo(petDB);
            if (textReceived == "/reward")
                return ShowRewardInfo(petDB);
#if DEBUG
            if (textReceived == "/restart")
                return RestartPet(petDB);
#endif
            return null;
        }

        private Answer ShowWorkInfo(Pet petDB)
        {
            var accessCheck = CheckStatusIsInactiveOrNull(petDB, IsGoToWorkCommand: true);
            if (accessCheck != null)
                return accessCheck;

            string toSendText = string.Empty;

            List<CommandModel> inlineParts;
            InlineKeyboardMarkup toSendInline = default;

            if (petDB.CurrentStatus == (int)CurrentStatus.WorkingOnPC)
            {
                TimeSpan remainsTime = new TimesToWait().WorkOnPCToWait - (DateTime.UtcNow - petDB.StartWorkingTime);

                //if callback handled when pet is working
                if (remainsTime > TimeSpan.Zero)
                    return ShowRemainedTimeWork(remainsTime);
            }
            else
            {
                toSendText = string.Format(workCommand, new TimesToWait().WorkOnPCToWait.TotalSeconds / 60, Rewards.WorkOnPCGoldReward);

                inlineParts = new InlineItems().InlineWork;
                toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

                var aud = _allUsersService.Get(_userId);
                aud.WorkCommandCounter++;
                _allUsersService.Update(aud);
            }

            return new Answer()
            {
                Text = toSendText,
                StickerId = StickersId.PetWork_Cat,
                InlineKeyboardMarkup = toSendInline
            };
        }
        private Answer ShowRewardInfo(Pet petDB)
        {
            string toSendText = string.Empty;

            List<CommandModel> inlineParts;
            InlineKeyboardMarkup toSendInline = default;

            if (petDB.GotDailyRewardTime.AddHours(new TimesToWait().DailyRewardToWait.TotalHours) > DateTime.UtcNow)
            {
                TimeSpan remainsTime = new TimesToWait().DailyRewardToWait - (DateTime.UtcNow - petDB.GotDailyRewardTime);

                //if callback handled when pet is working
                if (remainsTime > TimeSpan.Zero)
                    return ShowRemainedTimeDailyReward(remainsTime);
            }
            else
            {
                toSendText = string.Format(rewardCommand);

                inlineParts = new InlineItems().InlineRewards;
                toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

                var aud = _allUsersService.Get(_userId);
                aud.RewardCommandCounter++;
                _allUsersService.Update(aud);
            }

            return new Answer()
            {
                Text = toSendText,
                StickerId = StickersId.DailyRewardSticker,
                InlineKeyboardMarkup = toSendInline
            };
        }


        public AnswerCallback CallbackHandler()
        {
            if (_callback.Data == null)
                return null;

            UpdateIndicators();

            var userDb = _appServices.UserService.Get(_userId);
            var petDb = _appServices.PetService.Get(_userId);

            if (userDb == null || petDb == null)
                return null;

            if (_callback.Data == "petCommandInlineBasicInfo")
                return ShowBasicInfoInline(petDb);

            if (_callback.Data == "petCommandInlineExtraInfo")
                return ShowExtraInfoInline(petDb);

            if (_callback.Data == "kitchenCommandInlineBread")
                return FeedWithBreadInline(petDb);

            if (_callback.Data == "kitchenCommandInlineRedApple")
                return FeedWithAppleInline(petDb);

            if (_callback.Data == "kitchenCommandInlineChocolate")
                return FeedWithChocolateInline(petDb);

            if (_callback.Data == "kitchenCommandInlineLollipop")
                return FeedWithLollipopInline(petDb);

            if (_callback.Data == "sleepCommandInlinePutToSleep")
                return PutToSleepInline(petDb);

            if (_callback.Data == "workCommandInlineWorkOnPC")
                return WorkOnPCInline(petDb);

            if (_callback.Data == "workCommandInlineShowTime")
                return WorkOnPCInline(petDb);

            if (_callback.Data == "rewardCommandInlineDailyReward")
                return GetDailyRewardInline(petDb);

            if (_callback.Data == "rewardCommandDailyRewardInlineShowTime")
                return GetDailyRewardInline(petDb);

            if (_callback.Data == "gameroomCommandInlineCard")
                return PlayCardInline(petDb);

            if (_callback.Data == "gameroomCommandInlineDice")
                return PlayDiceInline(petDb);

            if (_callback.Data == "hospitalCommandCurePills")
                return CureWithPill(petDb);

            if (_callback.Data == "bathroomCommandBrushTeeth")
                return TeethInline(petDb);

            if (_callback.Data == "bathroomCommandTakeShower")
                return TakeShowerInline(petDb);

            if (_callback.Data == "ranksCommandInlineGold")
                return ShowRanksGold();

            if (_callback.Data == "ranksCommandInlineLevel")
                return ShowRanksLevel();

            if (_callback.Data == "ranksCommandInlineApples")
                return ShowRanksApples();

            return null;
        }

        /*        private void DeleteDataOfUser()
                {
                    _petService.Remove(user.UserId);
                    _chatService.Remove(user.UserId);
                    _userService.Remove(user.UserId);
                    _appleGameDataService.Delete(user.UserId);
                }
        */

        public Answer GetFarewellAnswer(string petName, string username)
        {
            string textToSend = string.Format(FarewellText, petName, username);

            return new Answer()
            {
                Text = textToSend,
                StickerId = StickersId.PetGone_Cat,
                IsPetGoneMessage = true
            };
        }
        private Answer ChangeLanguage()
        {
            _ = _userService.UpdateLanguage(_userId, null);

            var aud = _allUsersService.Get(_userId);
            aud.LanguageCommandCounter++;
            _allUsersService.Update(aud);

            return new Answer()
            {
                Text = Resources.Resources.ChangeLanguage,
                StickerId = StickersId.ChangeLanguageSticker,
                ReplyMarkup = LanguagesMarkup,
                InlineKeyboardMarkup = null
            };
        }
        private Answer ShowPetInfo(Pet petDB)
        {
            string toSendText = string.Format(petCommand,
                                              petDB.Name,
                                              petDB.HP,
                                              petDB.EXP,
                                              petDB.Level,
                                              petDB.Satiety,
                                              petDB.Fatigue,
                                              Extensions.GetCurrentStatus(petDB.CurrentStatus),
                                              petDB.Joy,
                                              petDB.Gold,
                                              petDB.Hygiene);

            var aud = _allUsersService.Get(_userId);
            aud.PetCommandCounter++;
            _allUsersService.Update(aud);

            return new Answer()
            {
                Text = toSendText,
                StickerId = StickersId.PetInfo_Cat,
                ReplyMarkup = null,
                InlineKeyboardMarkup = Extensions.InlineKeyboardOptimizer(new InlineItems().InlinePet)
            };

        }
        private bool CheckStatusIsInactive(Pet petDB, bool IsGoToSleepCommand = false)
        {
            if (petDB.CurrentStatus == (int)CurrentStatus.Sleeping && !IsGoToSleepCommand)
            {
                string denyText = string.Format(denyAccessSleeping);
                _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, denyText, true);

                return true;
            }

            if (petDB.CurrentStatus == (int)CurrentStatus.WorkingOnPC)
            {
                string denyText = string.Format(denyAccessWorking);
                _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, denyText, true);

                return true;
            }

            return false;
        }

        private Answer CheckStatusIsInactiveOrNull(Pet petDB, bool IsGoToSleepCommand = false, bool IsGoToWorkCommand = false)
        {
            if (petDB.CurrentStatus == (int)CurrentStatus.Sleeping && !IsGoToSleepCommand)
            {
                string denyText = string.Format(denyAccessSleeping);
                return new Answer()
                {
                    Text = denyText,
                    StickerId = StickersId.PetBusy_Cat
                };
            }

            if (petDB.CurrentStatus == (int)CurrentStatus.WorkingOnPC && !IsGoToWorkCommand)
            {
                string denyText = string.Format(denyAccessWorking);
                return new Answer()
                {
                    Text = denyText,
                    StickerId = StickersId.PetBusy_Cat
                };
            }

            return null;
        }
        private Answer GoToBathroom(Pet petDB)
        {
            var accessCheck = CheckStatusIsInactiveOrNull(petDB);
            if (accessCheck != null)
                return accessCheck;

            string toSendText = string.Format(bathroomCommand, petDB.Hygiene);

            List<CommandModel> inlineParts = new InlineItems().InlineHygiene;
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            var aud = _allUsersService.Get(_userId);
            aud.BathroomCommandCounter++;
            _allUsersService.Update(aud);

            return new Answer()
            {
                Text = toSendText,
                StickerId = StickersId.PetBathroom_Cat,
                InlineKeyboardMarkup = toSendInline
            };
        }
        private Answer GoToKitchen(Pet petDB)
        {
            var accessCheck = CheckStatusIsInactiveOrNull(petDB);
            if (accessCheck != null)
                return accessCheck;

            string toSendText = string.Format(kitchenCommand, petDB.Satiety, petDB.Gold);

            List<CommandModel> inlineParts = new InlineItems().InlineFood;
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            var aud = _allUsersService.Get(_userId);
            aud.KitchenCommandCounter++;
            _allUsersService.Update(aud);

            return new Answer()
            {
                Text = toSendText,
                StickerId = StickersId.PetKitchen_Cat,
                InlineKeyboardMarkup = toSendInline
            };

        }
        private Answer GoToGameroom(Pet petDB)
        {
            var accessCheck = CheckStatusIsInactiveOrNull(petDB);
            if (accessCheck != null)
                return accessCheck;

            string toSendText = string.Format(gameroomCommand,
                                              petDB.Fatigue,
                                              petDB.Joy,
                                              petDB.Gold,
                                              Factors.CardGameJoyFactor,
                                              Costs.AppleGame,
                                              Factors.DiceGameJoyFactor,
                                              Costs.DiceGame);

            List<CommandModel> inlineParts = new InlineItems().InlineGames;
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            var aud = _allUsersService.Get(_userId);
            aud.GameroomCommandCounter++;
            _allUsersService.Update(aud);

            return new Answer()
            {
                Text = toSendText,
                StickerId = StickersId.PetGameroom_Cat,
                InlineKeyboardMarkup = toSendInline
            };

        }
        private Answer GoToHospital(Pet petDB)
        {
            var accessCheck = CheckStatusIsInactiveOrNull(petDB);
            if (accessCheck != null)
                return accessCheck;

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

            List<CommandModel> inlineParts = new InlineItems().InlineHospital;
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts);

            var aud = _allUsersService.Get(_userId);
            aud.HospitalCommandCounter++;
            _allUsersService.Update(aud);

            return new Answer()
            {
                Text = toSendText,
                StickerId = stickerHospital,
                InlineKeyboardMarkup = toSendInline
            };

        }
        private Answer ShowRankingInfo()
        {
            var anwserRating = GetRanksByLevel();

            var aud = _allUsersService.Get(_userId);
            aud.RanksCommandCounter++;
            _allUsersService.Update(aud);

            return new Answer()
            {
                Text = anwserRating,
                StickerId = StickersId.PetRanks_Cat,
                InlineKeyboardMarkup = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineRanks, 3),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
            };

        }

        private string GetRanksByLevel()
        {
            var topPets = _petService.GetAll()
                .OrderByDescending(p => p.Level)
                .ThenByDescending(p => p.LastUpdateTime)
                .Take(10); //First 10 top-level pets

            string anwserRating = "";
            var currentUser = _appServices.UserService.Get(_userId);
            var currentPet = _appServices.PetService.Get(_userId);

            int counter = 1;
            foreach (var petDB in topPets)
            {
                var userDB = _userService.Get(petDB.UserId);
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
                anwserRating += "\n <b>" + _petService.GetAll()
                .OrderByDescending(p => p.Level)
                .ThenByDescending(p => p.LastUpdateTime)
                .ToList()
                .FindIndex(a => a.UserId == currentUser.UserId) + ". " + currentPet.Level + " 🐱 " + HttpUtility.HtmlEncode(name) + "</b>";
            }

            return anwserRating;
        }
        private string GetRanksByApples()
        {
            var topApples = _appleGameDataService.GetAll()
                .OrderByDescending(a => a.TotalWins)
                .Take(10); //First 10 top-apples users

            string anwserRating = "";
            var currentUser = _appServices.UserService.Get(_userId);
            var currentPet = _appServices.PetService.Get(_userId);

            int counter = 1;
            foreach (var appleUser in topApples)
            {
                var userDB = _userService.Get(appleUser.UserId);
                string name = " 🐱 " + _petService.Get(appleUser.UserId)?.Name ?? userDB?.Username ?? userDB?.FirstName + userDB?.LastName ?? "";
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
                var currentUserApple =_appleGameDataService.Get(_userId);

                anwserRating += "\n______________________________";
                var counterN = _appleGameDataService.GetAll()
                .OrderByDescending(a => a.TotalWins)
                .ToList()
                .FindIndex(a => a.UserId == _userId);
                anwserRating += "\n <b> " + (counterN == -1 ? _appleGameDataService.GetAll()?.Count : counterN) + ". " + (currentUserApple?.TotalWins ?? 0) + HttpUtility.HtmlEncode(" 🐱 " + currentPet?.Name ?? currentUser.Username ?? currentUser.FirstName + currentUser.LastName) + "</b>";
            }


            return anwserRating;
        }
        private string GetRanksByGold()
        {
            var topPets = _petService.GetAll()
                .OrderByDescending(p => p.Gold)
                .ThenByDescending(p => p.LastUpdateTime)
                .Take(10); //First 10 top-gold pets

            string anwserRating = "";
            var currentUser = _appServices.UserService.Get(_userId);
            var currentPet = _appServices.PetService.Get(_userId);

            int counter = 1;
            foreach (var pet in topPets)
            {
                var userDB = _userService.Get(pet.UserId);
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
                anwserRating += "\n <b>" + _petService.GetAll()
                .OrderByDescending(p => p.Gold)
                .ThenByDescending(p => p.LastUpdateTime)
                .ToList()
                .FindIndex(a => a.UserId == currentUser.UserId) + ". " + currentPet.Gold + " 🐱 " + HttpUtility.HtmlEncode(name) + "</b>";
            }

            return anwserRating;
        }

        private Answer GoToSleep(Pet petDB)
        {
            var accessCheck = CheckStatusIsInactiveOrNull(petDB, true);
            if (accessCheck != null)
                return accessCheck;

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
                    new List<CommandModel>()
                    {
                        new CommandModel()
                        {
                            Text = timeToWaitStr,
                            CallbackData = "sleepCommandInlinePutToSleep"
                        }
                    });
            }
            else
                toSendInline = Extensions.InlineKeyboardOptimizer(
                    new List<CommandModel>()
                    {
                        new CommandModel()
                        {
                            Text = sleepCommandInlinePutToSleep,
                            CallbackData = "sleepCommandInlinePutToSleep"
                        }
                    });

            var aud = _allUsersService.Get(_userId);
            aud.SleepCommandCounter++;
            _allUsersService.Update(aud);

            return new Answer()
            {
                Text = toSendText,
                StickerId = StickersId.PetSleep_Cat,
                InlineKeyboardMarkup = toSendInline
            };

        }
        private Answer ShowHelpInfo()
        {
            string toSendText = string.Format(helpCommand);

            var aud = _allUsersService.Get(_userId);
            aud.HelpCommandCounter++;
            _allUsersService.Update(aud);

            return new Answer()
            {
                Text = toSendText,
                StickerId = StickersId.HelpCommandSticker
            };

        }
        private Answer ShowMenuInfo()
        {
            string toSendText = string.Format(menuCommand);

            var aud = _allUsersService.Get(_userId);
            aud.MenuCommandCounter++;
            _allUsersService.Update(aud);

            return new Answer()
            {
                Text = toSendText,
                StickerId = StickersId.MenuCommandSticker
            };
        }
        private Answer RenamePet(Pet petDB)
        {
            if (_bannedService.GetAll().Any(bs => bs.UserId == _userId && bs.IsRenameBanned))
            {
                string toSendTextBan = string.Format(renameBannedCommand);

                var audF = _allUsersService.Get(_userId);
                audF.RenameCommandCounter++;
                _allUsersService.Update(audF);

                return new Answer()
                {
                    Text = toSendTextBan,
                    StickerId = StickersId.BannedSticker
                };
            }

            string toSendText = string.Format(renameCommand);

            var aud = _allUsersService.Get(_userId);
            aud.RenameCommandCounter++;
            _allUsersService.Update(aud);

            return new Answer()
            {
                Text = toSendText,
                StickerId = StickersId.RenamePetSticker
            };

        }
        private Answer RestartPet(Pet petDB)
        {
            string toSendText = string.Format(restartCommand, petDB.Name);

            _petService.Remove(_userId);
            _userService.Remove(_userId);
            _chatService.Remove(_userId);

            return new Answer()
            {
                Text = toSendText,
                StickerId = StickersId.DroppedPetSticker
            };
        }

        private int UpdateIndicatorEXP(int minuteCounter, Pet pet)
        {
            var petResult = new Pet().Clone(pet);

            int toAddExp = minuteCounter * Factors.ExpFactor;
            petResult.EXP += toAddExp;

            if (petResult.EXP > 100)
            {
                petResult.Level += petResult.EXP / Factors.ExpToLvl;
                petResult.EXP -= (petResult.EXP / Factors.ExpToLvl) * Factors.ExpToLvl;
            }

            return petResult.EXP;
        }
        private (double, int) UpdateIndicatorSatietyAndHP(int minuteCounter, Pet pet)
        {
            var petResult = new Pet().Clone(pet);

            double decreaseSatiety = Math.Round(minuteCounter * Factors.StarvingFactor, 2);

            petResult.Satiety -= decreaseSatiety;
            petResult.Satiety = Math.Round(petResult.Satiety, 2);

            if (petResult.Satiety < 0)
            {
                petResult.Satiety = 0;
                int tmpSat = (int)decreaseSatiety > 100 ? ((int)decreaseSatiety) - 100 : ((int)decreaseSatiety);
                petResult.HP -= tmpSat;

                if (petResult.HP < 0)
                    petResult.HP = 0;
            }

            return (petResult.Satiety, petResult.HP);
        }
        private int UpdateIndicatorHygiene(int minuteCounter, Pet petDB)
        {
            var petResult = new Pet().Clone(petDB);

            double toDecreaseHygiene = Math.Round(minuteCounter * Factors.HygieneFactor);

            petResult.Hygiene -= (int)toDecreaseHygiene;

            if (petResult.Hygiene < 0)
                petResult.Hygiene = 0;

            return petResult.Hygiene;
        }
        private int UpdateIndicatorJoy(int minuteCounter, Pet pet)
        {
            var petResult = new Pet().Clone(pet);

            double toDecreaseJoy = Math.Round(minuteCounter * Factors.JoyFactor);

            petResult.Joy -= (int)toDecreaseJoy;

            if (petResult.Joy < 0)
                petResult.Joy = 0;

            return petResult.Joy;
        }
        private int UpdateIndicatorFatigue(int minuteCounter, Pet pet)
        {
            var petResult = new Pet().Clone(pet);

            if (petResult.CurrentStatus == (int)CurrentStatus.Active)
            {
                double toAddFatigue = Math.Round(minuteCounter * Factors.FatigueFactor);
                petResult.Fatigue += (int)toAddFatigue;
            }

            if (petResult.Fatigue > 100)
                petResult.Fatigue = 100;

            return petResult.Fatigue;
        }
        private (CurrentStatus, int) UpdateIndicatorSleeping(Pet petDB)
        {
            var petResult = new Pet().Clone(petDB);

            var remainsToSleepTime = petResult.ToWakeUpTime - DateTime.UtcNow;
            if (remainsToSleepTime <= TimeSpan.Zero)
            {
                petResult.CurrentStatus = (int)CurrentStatus.Active;
                petResult.Fatigue = 0;

                return ((CurrentStatus)petResult.CurrentStatus, petResult.Fatigue);
            }

            return ((CurrentStatus)petResult.CurrentStatus, petResult.Fatigue);
        }

        #region Inline Answers
        private AnswerCallback ShowBasicInfoInline(Pet petDB)
        {
            string toSendText = string.Format(petCommand,
                                              petDB.Name,
                                              petDB.HP,
                                              petDB.EXP,
                                              petDB.Level,
                                              petDB.Satiety,
                                              petDB.Fatigue,
                                              Extensions.GetCurrentStatus(petDB.CurrentStatus),
                                              petDB.Joy,
                                              petDB.Gold,
                                              petDB.Hygiene);
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new List<CommandModel>()
                {
                    new CommandModel()
                    {
                        Text = petCommandInlineExtraInfo,
                        CallbackData = "petCommandInlineExtraInfo"
                    }
                });

            return new AnswerCallback(toSendText, toSendInline);
        }
        private AnswerCallback ShowExtraInfoInline(Pet petDB)
        {
            string toSendText = string.Format(petCommandMoreInfo1, petDB.Name, petDB.BirthDateTime);
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new List<CommandModel>()
                {
                    new CommandModel()
                    {
                        Text = petCommandInlineBasicInfo,
                        CallbackData = "petCommandInlineBasicInfo"
                    }
                });

            var aud = _allUsersService.Get(_userId);
            aud.ExtraInfoShowedTimesCounter++;
            _allUsersService.Update(aud);
            return new AnswerCallback(toSendText, toSendInline);
        }
        private AnswerCallback FeedWithBreadInline(Pet petDB)
        {
            var newStarving = Math.Round(petDB.Satiety + FoodFactors.BreadHungerFactor, 2);
            if (newStarving > 100)
            {
                string toSendTextLocal = string.Format(kitchenCommand, petDB.Satiety, petDB.Gold);

                string answerLocal = string.Format(tooManyStarvingCommand);
                _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, answerLocal);

                InlineKeyboardMarkup toSendInlineLocal = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineFood, 3);
                return new AnswerCallback(toSendTextLocal, toSendInlineLocal);
            }


            var newGold = petDB.Gold - Costs.Bread;
            if (newGold < 0)
            {
                string toSendTextLocal = string.Format(kitchenCommand, petDB.Satiety, petDB.Gold);

                string anwserLocal = string.Format(goldNotEnough);
                _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwserLocal);

                InlineKeyboardMarkup toSendInlineLocal = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineFood, 3);
                return new AnswerCallback(toSendTextLocal, toSendInlineLocal);
            }
            _petService.UpdateGold(_userId, newGold);
            _petService.UpdateSatiety(_userId, newStarving);
            var aud = _allUsersService.Get(_userId);
            aud.BreadEatenCounter++;
            aud.GoldSpentCounter += Costs.Bread;
            _allUsersService.Update(aud);

            string anwser = string.Format(PetFeedingAnwserCallback, (int)FoodFactors.BreadHungerFactor);
            _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwser);

            string toSendText = string.Format(kitchenCommand, newStarving, newGold);

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineFood, 3);

            return new AnswerCallback(toSendText, toSendInline);
        }
        private AnswerCallback TakeShowerInline(Pet petDB)
        {
            var newHygiene = petDB.Hygiene + HygieneFactors.ShowerFactor;
            newHygiene = newHygiene > 100 ? 100 : newHygiene;

            _petService.UpdateHygiene(_userId, newHygiene);

            string anwser = string.Format(PetHygieneAnwserCallback, HygieneFactors.ShowerFactor);
            _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwser);

            string toSendText = string.Format(bathroomCommand, newHygiene);

            List<CommandModel> inlineParts = new InlineItems().InlineHygiene;
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            return new AnswerCallback(toSendText, toSendInline);
        }

        private AnswerCallback TeethInline(Pet petDB)
        {
            var newHygiene = petDB.Hygiene + HygieneFactors.TeethFactor;
            newHygiene = newHygiene > 100 ? 100 : newHygiene;

            _petService.UpdateHygiene(_userId, newHygiene);

            string anwser = string.Format(PetHygieneAnwserCallback, HygieneFactors.TeethFactor);
            _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwser);
            string toSendText = string.Format(bathroomCommand, newHygiene);

            List<CommandModel> inlineParts = new InlineItems().InlineHygiene;
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            return new AnswerCallback(toSendText, toSendInline);
        }
        private AnswerCallback FeedWithAppleInline(Pet petDB)
        {
            var newStarving = Math.Round(petDB.Satiety + FoodFactors.RedAppleHungerFactor, 2);
            if (newStarving > 100)
            {
                string toSendTextLocal = string.Format(kitchenCommand, petDB.Satiety, petDB.Gold);

                string anwserLocal = string.Format(tooManyStarvingCommand);
                _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwserLocal);

                InlineKeyboardMarkup toSendInlineLocal = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineFood, 3);
                return new AnswerCallback(toSendTextLocal, toSendInlineLocal);
            }

            var newGold = petDB.Gold - Constants.Costs.Apple;
            if (newGold < 0)
            {
                string toSendTextLocal = string.Format(kitchenCommand, petDB.Satiety, petDB.Gold);

                string anwserLocal = string.Format(goldNotEnough);
                _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwserLocal);

                InlineKeyboardMarkup toSendInlineLocal = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineFood, 3);
                return new AnswerCallback(toSendTextLocal, toSendInlineLocal);
            }

            _petService.UpdateGold(_userId, newGold);
            _petService.UpdateSatiety(_userId, newStarving);

            var aud = _allUsersService.Get(_userId);
            aud.AppleEatenCounter++;
            aud.GoldSpentCounter += Costs.Apple;
            _allUsersService.Update(aud);

            string anwser = string.Format(PetFeedingAnwserCallback, (int)FoodFactors.RedAppleHungerFactor);
            _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwser);

            string toSendText = string.Format(kitchenCommand, newStarving, newGold);

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineFood, 3);

            return new AnswerCallback(toSendText, toSendInline);
        }
        private AnswerCallback FeedWithChocolateInline(Pet petDB)
        {
            var newStarving = Math.Round(petDB.Satiety + FoodFactors.ChocolateHungerFactor, 2);
            if (newStarving > 100)
            {
                string toSendTextLocal = string.Format(kitchenCommand, petDB.Satiety, petDB.Gold);

                string anwserLocal = string.Format(tooManyStarvingCommand);
                _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwserLocal);

                InlineKeyboardMarkup toSendInlineLocal = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineFood, 3);
                return new AnswerCallback(toSendTextLocal, toSendInlineLocal);
            }

            var newGold = petDB.Gold - Constants.Costs.Chocolate;
            if (newGold < 0)
            {
                string toSendTextLocal = string.Format(kitchenCommand, petDB.Satiety, petDB.Gold);

                string anwserLocal = string.Format(goldNotEnough);
                _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwserLocal);

                InlineKeyboardMarkup toSendInlineLocal = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineFood, 3);
                return new AnswerCallback(toSendTextLocal, toSendInlineLocal);
            }

            _petService.UpdateGold(_userId, newGold);
            _petService.UpdateSatiety(_userId, newStarving);
            var aud = _allUsersService.Get(_userId);
            aud.ChocolateEatenCounter++;
            aud.GoldSpentCounter += Constants.Costs.Chocolate;
            _allUsersService.Update(aud);

            string anwser = string.Format(PetFeedingAnwserCallback, (int)FoodFactors.ChocolateHungerFactor);
            _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwser);

            string toSendText = string.Format(kitchenCommand, newStarving, newGold);

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineFood, 3);

            return new AnswerCallback(toSendText, toSendInline);
        }
        private AnswerCallback FeedWithLollipopInline(Pet petDB)
        {
            var newStarving = Math.Round(petDB.Satiety + FoodFactors.LollipopHungerFactor, 2);
            if (newStarving > 100)
            {
                string toSendTextLocal = string.Format(kitchenCommand, petDB.Satiety, petDB.Gold);

                string anwserLocal = string.Format(tooManyStarvingCommand);
                _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwserLocal);

                InlineKeyboardMarkup toSendInlineLocal = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineFood, 3);
                return new AnswerCallback(toSendTextLocal, toSendInlineLocal);
            }

            var newGold = petDB.Gold - Constants.Costs.Lollipop;
            if (newGold < 0)
            {
                string toSendTextLocal = string.Format(kitchenCommand, petDB.Satiety, petDB.Gold);

                string anwserLocal = string.Format(goldNotEnough);
                _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwserLocal);

                InlineKeyboardMarkup toSendInlineLocal = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineFood, 3);
                return new AnswerCallback(toSendTextLocal, toSendInlineLocal);
            }

            _petService.UpdateGold(_userId, newGold);
            _petService.UpdateSatiety(_userId, newStarving);

            var aud = _allUsersService.Get(_userId);
            aud.LollypopEatenCounter++;
            aud.GoldSpentCounter += Costs.Lollipop;
            _allUsersService.Update(aud);

            string anwser = string.Format(PetFeedingAnwserCallback, (int)FoodFactors.LollipopHungerFactor);
            _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwser);

            string toSendText = string.Format(kitchenCommand, newStarving, newGold);

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineFood, 3);

            return new AnswerCallback(toSendText, toSendInline);
        }
        private AnswerCallback PutToSleepInline(Pet petDB)
        {
            Pet petResult = new Pet().Clone(petDB);

            if (CheckStatusIsInactive(petResult, true))
                return null;

            if (petResult.CurrentStatus == (int)CurrentStatus.Sleeping)
            {
                UpdateSleepingInline(petResult);
                return null;
            }

            if (petResult.Fatigue < Limits.ToRestMinLimitOfFatigue)
            {
                DeclineSleepingInline(petResult);
                return null;
            }

            StartSleepingInline(petResult);

            return null;
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

            string toSendText = string.Format(sleepCommand, petDB.Name, petDB.Fatigue, Extensions.GetCurrentStatus(petDB.CurrentStatus));

            var ticksToWait = (petDB.ToWakeUpTime - DateTime.UtcNow).Ticks;

            string timeToWaitStr = string.Format(sleepCommandInlineShowTime, new DateTime().AddTicks(ticksToWait).ToString("HH:mm:ss"));

            InlineKeyboardMarkup toSendInline =
                Extensions.InlineKeyboardOptimizer(new List<CommandModel>()
                {
                    new CommandModel()
                    {
                        Text = timeToWaitStr,
                        CallbackData = "sleepCommandInlinePutToSleep"
                    }
                });

            _appServices.BotControlService.EditMessageTextAsync(_userId, _callback.Message.MessageId, toSendText, toSendInline);
        }

        private void DeclineSleepingInline(Pet petDB)
        {
            _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, PetSleepingDoesntWantYetAnwserCallback);
            string sendTxt = string.Format(sleepCommand, petDB.Name, petDB.Fatigue, Extensions.GetCurrentStatus(petDB.CurrentStatus));

            InlineKeyboardMarkup toSendInlineWhileActive =
                Extensions.InlineKeyboardOptimizer(new List<CommandModel>()
                {
                    new CommandModel()
                    {
                        Text = sleepCommandInlinePutToSleep,
                        CallbackData = "sleepCommandInlinePutToSleep"
                    }
                });

            _appServices.BotControlService.EditMessageTextAsync(_userId, _callback.Message.MessageId, sendTxt, toSendInlineWhileActive);
        }


        private void UpdateSleepingInline(Pet petDB)
        {
            _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, PetSleepingAlreadyAnwserCallback);

            string toSendText = string.Format(sleepCommand, petDB.Name, petDB.Fatigue, Extensions.GetCurrentStatus(petDB.CurrentStatus));

            var ticksToWait = (petDB.ToWakeUpTime - DateTime.UtcNow).Ticks;

            string timeToWaitStr = string.Format(sleepCommandInlineShowTime, new DateTime().AddTicks(ticksToWait).ToString("HH:mm:ss"));


            InlineKeyboardMarkup toSendInline =
                Extensions.InlineKeyboardOptimizer(new List<CommandModel>()
                {
                    new CommandModel()
                    {
                        Text = timeToWaitStr,
                        CallbackData = "sleepCommandInlinePutToSleep"
                    }
                });

            _appServices.BotControlService.EditMessageTextAsync(_userId, _callback.Message.MessageId, toSendText, toSendInline);
        }

        private AnswerCallback PlayCardInline(Pet petDB)
        {
            var newFatigue = petDB.Fatigue + Factors.CardGameFatigueFactor;
            if (newFatigue > 100)
                newFatigue = 100;

            var newJoy = petDB.Joy + Factors.CardGameJoyFactor;
            if (newJoy > 100)
                newJoy = 100;

            var newGold = petDB.Gold - Costs.AppleGame;
            if (newGold < 0)
            {
                string toSendTextLocal = string.Format(gameroomCommand,
                                                       petDB.Fatigue,
                                                       petDB.Joy,
                                                       petDB.Gold,
                                                       Factors.CardGameJoyFactor,
                                                       Costs.AppleGame,
                                                       Factors.DiceGameJoyFactor,
                                                       Costs.DiceGame);

                string anwserLocal = string.Format(goldNotEnough);
                _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwserLocal);

                InlineKeyboardMarkup toSendInlineLocal = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineGames, 3);
                return new AnswerCallback(toSendTextLocal, toSendInlineLocal);
            }

            _petService.UpdateGold(_userId, newGold);
            _petService.UpdateFatigue(_userId, newFatigue);
            _petService.UpdateJoy(_userId, newJoy);

            string anwser = string.Format(PetPlayingAnwserCallback, Factors.CardGameFatigueFactor);
            _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwser);
            var aud = _allUsersService.Get(_userId);
            aud.CardsPlayedCounter++;
            _allUsersService.Update(aud);

            string toSendText = string.Format(gameroomCommand, newFatigue, newJoy, newGold, Factors.CardGameJoyFactor, Factors.CardGameJoyFactor, Costs.AppleGame, Factors.DiceGameJoyFactor, Costs.DiceGame);

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineGames, 3);

            return new AnswerCallback(toSendText, toSendInline);
        }
        private AnswerCallback PlayDiceInline(Pet petDB)
        {
            var newFatigue = petDB.Fatigue + Factors.DiceGameFatigueFactor;
            if (newFatigue > 100)
                newFatigue = 100;

            var newJoy = petDB.Joy + Factors.DiceGameJoyFactor;
            if (newJoy > 100)
                newJoy = 100;

            var newGold = petDB.Gold - Costs.DiceGame;
            if (newGold < 0)
            {
                string toSendTextLocal = string.Format(gameroomCommand,
                                                       petDB.Fatigue,
                                                       petDB.Joy,
                                                       petDB.Gold,
                                                       Factors.CardGameJoyFactor,
                                                       Costs.AppleGame,
                                                       Factors.DiceGameJoyFactor,
                                                       Costs.DiceGame);

                string anwserLocal = string.Format(goldNotEnough);
                _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwserLocal);

                InlineKeyboardMarkup toSendInlineLocal = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineGames, 3);
                return new AnswerCallback(toSendTextLocal, toSendInlineLocal);
            }

            _petService.UpdateGold(_userId, newGold);
            _petService.UpdateFatigue(_userId, newFatigue);
            _petService.UpdateJoy(_userId, newJoy);
            var aud = _allUsersService.Get(_userId);
            aud.DicePlayedCounter++;
            aud.GoldSpentCounter += Costs.DiceGame;
            _allUsersService.Update(aud);

            string anwser = string.Format(PetPlayingAnwserCallback, Factors.DiceGameFatigueFactor);
            _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwser);

            string toSendText = string.Format(gameroomCommand, newFatigue, newJoy, newGold, Factors.CardGameJoyFactor, Costs.AppleGame, Factors.DiceGameJoyFactor, Costs.DiceGame);

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineGames, 3);

            return new AnswerCallback(toSendText, toSendInline);
        }
        private AnswerCallback WorkOnPCInline(Pet petDB)
        {
            InlineKeyboardMarkup toSendInline;
            string toSendText;

            if (petDB.CurrentStatus == (int)CurrentStatus.Active)
            {
                if (_callback.Data == "workCommandInlineShowTime" || _callback.Data == null)
                {
                    string toSendTextIfTimeOver = string.Format(workCommand,
                                                                new TimesToWait().WorkOnPCToWait.TotalSeconds/60,
                                                                Rewards.WorkOnPCGoldReward);

                    List<CommandModel> inlineParts = new InlineItems().InlineWork;
                    InlineKeyboardMarkup toSendInlineIfTimeOver = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

                    return new AnswerCallback(toSendTextIfTimeOver, toSendInlineIfTimeOver);
                }


                var newFatigue = petDB.Fatigue + Factors.WorkOnPCFatigueFactor;
                if (newFatigue > 100)
                    newFatigue = 100;

                _petService.UpdateFatigue(_userId, newFatigue);
                _petService.UpdateGold(_userId, petDB.Gold += Rewards.WorkOnPCGoldReward);
                _petService.UpdateCurrentStatus(_userId, (int)CurrentStatus.WorkingOnPC);

                var aud = _allUsersService.Get(_userId);
                aud.GoldEarnedCounter += Rewards.WorkOnPCGoldReward;
                aud.WorkOnPCCounter++;
                _allUsersService.Update(aud);

                var startWorkingTime = DateTime.UtcNow;
                _petService.UpdateStartWorkingTime(_userId, startWorkingTime);

                string anwser = string.Format(PetWorkingAnswerCallback, Factors.WorkOnPCFatigueFactor, Rewards.WorkOnPCGoldReward);
                _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwser, true);

                toSendText = string.Format(workCommandPCWorking);

                TimeSpan remainsTime = new TimesToWait().WorkOnPCToWait - (DateTime.UtcNow - startWorkingTime);
                string inlineStr = string.Format(workCommandInlineShowTime, new DateTime(remainsTime.Ticks).ToString("HH:mm:ss"));

                toSendInline = Extensions.InlineKeyboardOptimizer(new List<CommandModel>()
                    {
                        new CommandModel()
                        {
                            Text = inlineStr,
                            CallbackData = "workCommandInlineShowTime"
                        }
                    });

                return new AnswerCallback(toSendText, toSendInline);
            }
            else if (petDB.CurrentStatus == (int)CurrentStatus.WorkingOnPC)
            {
                TimeSpan remainsTime = new TimesToWait().WorkOnPCToWait - (DateTime.UtcNow - petDB.StartWorkingTime);

                //if callback handled when time of work is over
                if (remainsTime <= TimeSpan.Zero)
                {
                    petDB.CurrentStatus = (int)CurrentStatus.Active;
                    _petService.UpdateCurrentStatus(_userId, petDB.CurrentStatus);

                    string toSendTextIfTimeOver = string.Format(workCommand, new TimesToWait().WorkOnPCToWait.TotalSeconds/60, Rewards.WorkOnPCGoldReward);

                    List<CommandModel> inlineParts = new InlineItems().InlineWork;
                    InlineKeyboardMarkup toSendInlineIfTimeOver = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

                    return new AnswerCallback(toSendTextIfTimeOver, toSendInlineIfTimeOver);
                }

                //if callback handled when pet is still working
                return ShowRemainedTimeWorkCallback(remainsTime);
            }
            else
                return null;
        }
        private AnswerCallback GetDailyRewardInline(Pet petDB)
        {
            var dateTimeWhenOver = petDB.GotDailyRewardTime.Add(new TimesToWait().DailyRewardToWait);
            if (dateTimeWhenOver > DateTime.UtcNow)
                return ShowRemainedTimeDailyRewardCallback(dateTimeWhenOver - DateTime.UtcNow, true);

            var newGold = petDB.Gold + Constants.Rewards.DailyGoldReward;

            _petService.UpdateGold(_userId, newGold);
            _petService.UpdateDailyRewardTime(_userId, DateTime.UtcNow);
            var aud = _allUsersService.Get(_userId);
            aud.GoldEarnedCounter += Constants.Rewards.DailyGoldReward;
            _allUsersService.Update(aud);

            string anwser = string.Format(DailyRewardAnwserCallback, Rewards.DailyGoldReward);
            _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwser);

            return ShowRemainedTimeDailyRewardCallback(new TimeSpan(23, 59, 59), false);
        }

        private Answer ShowRemainedTimeWork(TimeSpan remainedTime)
        {
            InlineKeyboardMarkup toSendInline;
            string toSendText;

            toSendText = string.Format(workCommandPCWorking);
            string inlineStr = string.Format(workCommandInlineShowTime, new DateTime(remainedTime.Ticks).ToString("HH:mm:ss"));

            toSendInline = Extensions.InlineKeyboardOptimizer(new List<CommandModel>()
                    {
                        new CommandModel()
                        {
                            Text = inlineStr,
                            CallbackData = "workCommandInlineShowTime"
                        }
                    });

            return new Answer() { InlineKeyboardMarkup = toSendInline, Text = toSendText, StickerId = StickersId.PetWork_Cat };
        }
        private AnswerCallback ShowRemainedTimeWorkCallback(TimeSpan remainedTime = default)
        {
            InlineKeyboardMarkup toSendInline;
            string toSendText;

            if (remainedTime == default)
                remainedTime = new TimeSpan(0);

            string anwser = string.Format(workCommandPCWorking);
            _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwser);

            toSendText = string.Format(workCommandPCWorking);
            string inlineStr = string.Format(workCommandInlineShowTime, new DateTime(remainedTime.Ticks).ToString("HH:mm:ss"));

            toSendInline = Extensions.InlineKeyboardOptimizer(new List<CommandModel>()
                    {
                        new CommandModel()
                        {
                            Text = inlineStr,
                            CallbackData = "workCommandInlineShowTime"
                        }
                    });

            return new AnswerCallback(toSendText, toSendInline);
        }

        private Answer ShowRemainedTimeDailyReward(TimeSpan remainedTime)
        {
            InlineKeyboardMarkup toSendInline;
            string toSendText;

            toSendText = string.Format(rewardCommandDailyRewardGotten);
            string inlineStr = string.Format(rewardCommandDailyRewardInlineShowTime, new DateTime(remainedTime.Ticks).ToString("HH:mm:ss"));

            toSendInline = Extensions.InlineKeyboardOptimizer(new List<CommandModel>()
                    {
                        new CommandModel()
                        {
                            Text = inlineStr,
                            CallbackData = "rewardCommandDailyRewardInlineShowTime"
                        }
                    });

            return new Answer() { InlineKeyboardMarkup = toSendInline, Text = toSendText, StickerId = StickersId.DailyRewardSticker };
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
                _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwser);
            }

            toSendText = string.Format(rewardCommandDailyRewardGotten);
            string inlineStr = string.Format(rewardCommandDailyRewardInlineShowTime, new DateTime(remainedTime.Ticks).ToString("HH:mm:ss"));

            toSendInline = Extensions.InlineKeyboardOptimizer(new List<CommandModel>()
                    {
                        new CommandModel()
                        {
                            Text = inlineStr,
                            CallbackData = "rewardCommandDailyRewardInlineShowTime"
                        }
                    });
            return new AnswerCallback(toSendText, toSendInline);
        }
        private AnswerCallback CureWithPill(Pet petDB)
        {
            var newHP = petDB.HP + Factors.PillHPFactor;
            if (newHP > 100)
                newHP = 100;

            var newJoy = petDB.Joy + Factors.PillJoyFactor;
            if (newJoy < 0)
                newJoy = 0;

            _petService.UpdateHP(_userId, newHP);
            _petService.UpdateJoy(_userId, newJoy);

            var aud = _allUsersService.Get(_userId);
            aud.PillEatenCounter++;
            _allUsersService.Update(aud);

            string anwser = string.Format(PetCuringAnwserCallback, Factors.PillHPFactor, Factors.PillJoyFactor);
            _bcService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwser, true);

            string commandHospital =  newHP switch
            {
                >= 80 => hospitalCommandHighHp,
                >20 and < 80 => hospitalCommandMidHp,
                _ => hospitalCommandLowHp
            };

            string toSendText = string.Format(commandHospital, newHP);

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineHospital);

            return new AnswerCallback(toSendText, toSendInline);
        }
        private AnswerCallback ShowRanksGold()
        {
            string toSendText = GetRanksByGold();

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineRanks);

            return new AnswerCallback(toSendText, toSendInline, Telegram.Bot.Types.Enums.ParseMode.Html);
        }

        private AnswerCallback ShowRanksApples()
        {
            string toSendText = GetRanksByApples();

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineRanks);

            return new AnswerCallback(toSendText, toSendInline, Telegram.Bot.Types.Enums.ParseMode.Html);
        }

        private AnswerCallback ShowRanksLevel()
        {
            string toSendText = GetRanksByLevel();

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineRanks);

            return new AnswerCallback(toSendText, toSendInline, Telegram.Bot.Types.Enums.ParseMode.Html);
        }

        #endregion

    }
}
