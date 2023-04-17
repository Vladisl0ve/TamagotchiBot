using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TamagotchiBot.Models;
using TamagotchiBot.Models.Answers;
using TamagotchiBot.Models.Mongo;
using TamagotchiBot.Services;
using TamagotchiBot.Services.Mongo;
using TamagotchiBot.UserExtensions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static TamagotchiBot.Resources.Resources;
using static TamagotchiBot.UserExtensions.Constants;
using Chat = TamagotchiBot.Models.Mongo.Chat;
using User = TamagotchiBot.Models.Mongo.User;

namespace TamagotchiBot.Controllers
{
    public class MenuController
    {
        private readonly UserService _userService;
        private readonly PetService _petService;
        private readonly ChatService _chatService;
        private readonly BotControlService _bcService;
        private readonly AllUsersDataService _allUsersService;
        private readonly ITelegramBotClient bot;
        private readonly Message message = null;
        private readonly CallbackQuery callback = null;
        private readonly long _userId;

        private User user;
        private Pet pet;
        private Chat chat;

        public MenuController(ITelegramBotClient bot,
                              UserService userService,
                              PetService petService,
                              ChatService chatService,
                              BotControlService botControlService,
                              AllUsersDataService allUsersService,
                              CallbackQuery callback)
        {
            this.bot = bot;
            _userService = userService;
            _petService = petService;
            this.callback = callback;
            _chatService = chatService;
            _bcService = botControlService;
            this._allUsersService = allUsersService;
            _userId = callback.From.Id;

            GetFromDb();

            Culture = new CultureInfo(user?.Culture ?? "ru");
            UpdateOrCreateAUD(callback.From, message, callback);
        }

        public MenuController(ITelegramBotClient bot,
                              UserService userService,
                              PetService petService,
                              ChatService chatService,
                              BotControlService botControlService,
                              AllUsersDataService allUsersService,
                              Message message)
        {
            this.bot = bot;
            _userService = userService;
            _petService = petService;
            this.message = message;
            _chatService = chatService;
            _bcService = botControlService;
            this._allUsersService = allUsersService;
            _userId = message.From.Id;

            GetFromDb();

            Culture = new CultureInfo(user?.Culture ?? "ru");
            UpdateOrCreateAUD(message.From, message, callback);
        }

        private void GetFromDb()
        {
            user = _userService.Get(_userId);

            if (user != null)
            {
                pet = _petService.Get(user.UserId);
                chat = _chatService.Get(message?.Chat.Id ?? callback.Message.Chat.Id);
            }
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
            aud.Culture = _userService.Get(user.Id)?.Culture ?? "en";
            aud.LastMessage = message?.Text ?? string.Empty;
            aud.MessageCounter = message == null ? aud.MessageCounter : aud.MessageCounter + 1;
            aud.CallbacksCounter = callback == null ? aud.CallbacksCounter : aud.CallbacksCounter + 1;
            _allUsersService.Update(aud);
        }
        public Answer CreateUser()
        {
            if (message != null)
            {
                _userService.Create(message.From);
                _chatService.Create(new Chat()
                {
                    ChatId = message.Chat.Id,
                    Name = message.Chat.Username ?? message.Chat.Title,
                    UserId = _userId,
                    LastMessage = null
                });

                Log.Information($"User {message.From.Username} has been added to Db");

                Culture = new CultureInfo(message.From.LanguageCode);
            }
            else if (callback != null)
            {
                _userService.Create(callback.From);
                _chatService.Create(new Chat()
                {
                    ChatId = callback.Message.Chat.Id,
                    Name = callback.Message.Chat.Username ?? callback.Message.Chat.Title,
                    UserId = callback.Message.From.Id,
                    LastMessage = null
                });

                Log.Information($"User {callback.Message.From.Username} has been added to Db");

                Culture = new CultureInfo(message.From.LanguageCode);
            }

            return new Answer(Resources.Resources.ChangeLanguage,
                               StickersId.ChangeLanguageSticker,
                               LanguagesMarkup,
                               null);
        }

        public Answer Process()
        {
            var userMessage = message.From;
            User userDb = user;
            Chat chatDb = chat;

            if (userDb.Culture != null)
            {
                Culture = new CultureInfo(userDb.Culture);
            }

            if (userMessage.Username != userDb.Username
                || userMessage.LastName != userDb.LastName
                || userMessage.FirstName != userDb.FirstName)
                UpdateUser(userDb.UserId);

            chatDb ??= UpdateChat(message);

            if (message.Chat.Username != null && chatDb.Name != message.Chat.Username)
            {
                _chatService.Update(message.Chat.Id, new Chat()
                {
                    Id = chatDb.Id,
                    Name = message.Chat.Username,
                    ChatId = chatDb.ChatId,
                    UserId = _userId
                });
                Log.Information($"Chat username {message.Chat.Username} has been updated in Db");
            }
            else if (message.Chat.Title != null && chatDb.Name != message.Chat.Title)
            {
                _chatService.Update(message.Chat.Id, new Chat()
                {
                    Id = chatDb.Id,
                    Name = message.Chat.Title,
                    ChatId = chatDb.ChatId,
                    UserId = _userId
                });
                Log.Information($"Chat title {message.Chat.Title} has been updated in Db");
            }

            if (message.Text != null && message.Text == "/language")
                return CommandHandler();

            if (userDb.Culture == null && message.Text != null)
                return CommandHandler();

            if (pet == null || pet.Name == null)
                return CreatePet();
            else
                return CommandHandler();

        }

        private void UpdateIndicators()
        {
            Telegram.Bot.Types.User messageUser = message?.From ?? callback?.From;

            if (messageUser == null || pet == null)
                return;

            //avoid nullPointerExeption
            if (pet.LastUpdateTime.Year == 1)
                pet.LastUpdateTime = DateTime.UtcNow;

            if (pet.StartSleepingTime.Year == 1)
                pet.StartSleepingTime = DateTime.UtcNow;

            int minuteCounter = (int)(DateTime.UtcNow - pet.LastUpdateTime).TotalMinutes;
            //EXP
            UpdateIndicatorEXP(minuteCounter);

            //Satiety
            UpdateIndicatorSatiety(minuteCounter);

            //Joy
            UpdateIndicatorJoy(minuteCounter);

            //Fatigue
            UpdateIndicatorFatigue(minuteCounter);

            //Sleeping
            if (pet.CurrentStatus == (int)CurrentStatus.Sleeping)
                UpdateIndicatorSleeping();

            //Work
            if (pet.CurrentStatus == (int)CurrentStatus.WorkingOnPC)
                UpdateIndicatorWork();

            pet.LastUpdateTime = DateTime.UtcNow;

            _petService.Update(messageUser.Id, pet);
        }

        private void UpdateIndicatorWork()
        {
            TimeSpan remainsTime = new TimesToWait().WorkOnPCToWait - (DateTime.UtcNow - pet.StartWorkingTime);

            //if callback handled when time of work is over
            if (remainsTime <= TimeSpan.Zero)
            {
                pet.CurrentStatus = (int)CurrentStatus.Active;
                _petService.UpdateCurrentStatus(_userId, pet.CurrentStatus);
            }

        }

        public Answer ExtrasHandler() //catching exceptional situations (but not exceptions!)
        {
            if (user.Culture == null)
            {
                if (message.Text == null)
                    return null;

                string last = message.Text.Split(' ').Last();
                string culture = last.GetCulture();

                if (culture == null)
                    return null;

                Culture = new CultureInfo(culture);

                _userService.UpdateLanguage(user.UserId, culture);
                string stickerToSend = culture.Language() switch
                {
                    Language.Polish => StickersId.PolishLanguageSetSticker,
                    Language.English => StickersId.EnglishLanguageSetSticker,
                    Language.Belarusian => StickersId.BelarussianLanguageSetSticker,
                    Language.Russian => StickersId.RussianLanguageSetSticker,
                    _ => null,
                };
                return new Answer()
                {
                    Text = ConfirmedLanguage,
                    StickerId = stickerToSend,
                    ReplyMarkup = new ReplyKeyboardRemove()
                };
            }
            else if (chat.LastMessage == "/rename")
            {
                _petService.UpdateName(user.UserId, message.Text);

                return new Answer()
                {
                    Text = ConfirmedName,
                    StickerId = StickersId.WelcomeSticker,
                    ReplyMarkup = new ReplyKeyboardRemove()
                };
            }

            return null;
        }

        public Answer CommandHandler()
        {
            string textReceived = message.Text;
            if (textReceived == null)
                return null;

            textReceived = textReceived.ToLower();

            UpdateIndicators();

            if (pet != null && IsPetGone())
            {
                DeleteDataOfUser();
                bot.SendChatActionAsync(user.UserId, Telegram.Bot.Types.Enums.ChatAction.Typing);
                return GetFarewellAnswer(pet.Name, user.FirstName ?? user.Username);
            }

            if (textReceived == "/language")
                return ChangeLanguage();
            if (textReceived == "/pet")
                return CheckPetStatus();
            if (textReceived == "/bathroom")
                return GoToBathroom();
            if (textReceived == "/kitchen")
                return GoToKitchen();
            if (textReceived == "/gameroom")
                return GoToGameroom();
            if (textReceived == "/hospital")
                return GoToHospital();
            if (textReceived == "/ranks")
                return ShowRankingInfo();
            if (textReceived == "/sleep")
                return GoToSleep();
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
            if (textReceived == "/help")
                return ShowHelpInfo();
            if (textReceived == "/menu")
                return ShowMenuInfo();
            if (textReceived == "/rename")
                return RenamePet();
            if (textReceived == "/work")
                return ShowWorkInfo();
#if DEBUG //only for debug purpose
            if (textReceived == "/restart")
                return RestartPet();
#endif

            return ExtrasHandler();
        }

        private Answer ShowWorkInfo()
        {
            var accessCheck = CheckStatusIsInactiveOrNull(IsGoToWorkCommand: true);
            if (accessCheck != null)
                return accessCheck;

            string toSendText = string.Empty;

            List<CommandModel> inlineParts;
            InlineKeyboardMarkup toSendInline = default;

            if (pet.CurrentStatus == (int)CurrentStatus.WorkingOnPC)
            {
                TimeSpan remainsTime = new TimesToWait().WorkOnPCToWait - (DateTime.UtcNow - pet.StartWorkingTime);

                //if callback handled when pet is working
                if (remainsTime > TimeSpan.Zero)
                    return ShowRemainedTimeWork(remainsTime);
            }
            else
            {
                toSendText = string.Format(workCommand);

                inlineParts = new InlineItems().InlineWork;
                toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

                var aud = _allUsersService.Get(_userId);
                aud.WorkCommandCounter++;
                _allUsersService.Update(aud);

                chat.LastMessage = "/work";
                _chatService.Update(chat.ChatId, chat);
            }

            return new Answer()
            {
                Text = toSendText,
                StickerId = StickersId.PetWork_Cat,
                InlineKeyboardMarkup = toSendInline
            };
        }

        public AnswerCallback CallbackHandler()
        {
            var userDb = user;
            var petDb = pet;

            if (userDb == null || petDb == null)
                return null;

            if (callback.Data == null)
                return null;

            UpdateIndicators();

            if (callback.Data == "petCommandInlineBasicInfo")
                return ShowBasicInfoInline();

            if (callback.Data == "petCommandInlineExtraInfo")
                return ShowExtraInfoInline();

            if (callback.Data == "kitchenCommandInlineBread") //56.379999999999995
                return FeedWithBreadInline();

            if (callback.Data == "kitchenCommandInlineRedApple")
                return FeedWithAppleInline();

            if (callback.Data == "kitchenCommandInlineChocolate")
                return FeedWithChocolateInline();

            if (callback.Data == "kitchenCommandInlineLollipop")
                return FeedWithLollipopInline();

            if (callback.Data == "sleepCommandInlinePutToSleep")
                return PutToSleepInline();

            if (callback.Data == "workCommandInlineWorkOnPC")
                return WorkOnPCInline();

            if (callback.Data == "workCommandInlineShowTime")
                return WorkOnPCInline();

            if (callback.Data == "gameroomCommandInlineCard")
                return PlayCardInline();

            if (callback.Data == "gameroomCommandInlineDice")
                return PlayDiceInline();

            if (callback.Data == "hospitalCommandCurePills")
                return CureWithPill();

            return null;
        }
        private Answer CreatePet()
        {
            if (message.Text == null)
                return null;

            if (pet == null)
            {
                _petService.Create(new Pet()
                {
                    Name = null,
                    Level = 1,
                    BirthDateTime = DateTime.UtcNow,
                    LastUpdateTime = DateTime.UtcNow,
                    EXP = 0,
                    HP = 100,
                    Joy = 100,
                    Fatigue = 0,
                    Satiety = 100,
                    IsWelcomed = true,
                    Type = null,
                    Gold = 0,
                    UserId = _userId
                });
                Log.Information($"Pet of {user.Username} has been added to Db");

                chat.LastMessage = "/welcome";
                _chatService.Update(chat.ChatId, chat);
                return new Answer()
                {
                    Text = Welcome,
                    StickerId = StickersId.WelcomeSticker,
                    ReplyMarkup = new ReplyKeyboardRemove(),
                    InlineKeyboardMarkup = null
                };
            }

            if (pet.IsWelcomed)
            {
                pet.IsWelcomed = false;
                _petService.Update(_userId, pet);
                return new Answer()
                {
                    Text = ChooseName,
                    StickerId = StickersId.PetChooseName_Cat,
                    ReplyMarkup = null,
                    InlineKeyboardMarkup = null
                };
            }

            if (pet.Name == null)
            {
                _petService.UpdateName(_userId, message.Text);
                return new Answer()
                {
                    Text = ConfirmedName,
                    StickerId = StickersId.PetConfirmedName_Cat,
                    ReplyMarkup = null,
                    InlineKeyboardMarkup = null
                };
            }

            return null;
        }
        private bool IsPetGone()
        {
            return pet.HP <= 0;
        }
        private void DeleteDataOfUser()
        {
            _petService.Remove(user.UserId);
            _chatService.Remove(user.UserId);
            _userService.Remove(user.UserId);
        }
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
        private Answer CheckPetStatus()
        {
            string toSendText = string.Format(petCommand, pet.Name, pet.HP, pet.EXP, pet.Level, pet.Satiety,
                                                  Extensions.GetFatigue(pet.Fatigue),
                                                  Extensions.GetCurrentStatus(pet.CurrentStatus),
                                                  pet.Joy,
                                                  pet.Gold);

            var aud = _allUsersService.Get(_userId);
            aud.PetCommandCounter++;
            _allUsersService.Update(aud);

            chat.LastMessage = "/pet";
            _chatService.Update(chat.ChatId, chat);
            return new Answer()
            {
                Text = toSendText,
                StickerId = StickersId.PetInfo_Cat,
                ReplyMarkup = null,
                InlineKeyboardMarkup = Extensions.InlineKeyboardOptimizer(new InlineItems().InlinePet)
            };

        }
        private bool CheckStatusIsInactive(bool IsGoToSleepCommand = false)
        {
            if (pet.CurrentStatus == (int)CurrentStatus.Sleeping && !IsGoToSleepCommand)
            {
                string denyText = string.Format(denyAccessSleeping);
                _bcService.AnswerCallbackQueryAsync(callback.Id, user.UserId, denyText, true);

                return true;
            }

            if (pet.CurrentStatus == (int)CurrentStatus.WorkingOnPC)
            {
                string denyText = string.Format(denyAccessWorking);
                _bcService.AnswerCallbackQueryAsync(callback.Id, user.UserId, denyText, true);

                return true;
            }

            return false;
        }

        private Answer CheckStatusIsInactiveOrNull(bool IsGoToSleepCommand = false, bool IsGoToWorkCommand = false)
        {
            if (pet.CurrentStatus == (int)CurrentStatus.Sleeping && !IsGoToSleepCommand)
            {
                string denyText = string.Format(denyAccessSleeping);
                return new Answer()
                {
                    Text = denyText,
                    StickerId = StickersId.PetBusy_Cat
                };
            }

            if (pet.CurrentStatus == (int)CurrentStatus.WorkingOnPC && !IsGoToWorkCommand)
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
        private Answer GoToBathroom()
        {
            var accessCheck = CheckStatusIsInactiveOrNull();
            if (accessCheck != null)
                return accessCheck;

            var aud = _allUsersService.Get(_userId);
            aud.BathroomCommandCounter++;
            _allUsersService.Update(aud);

            chat.LastMessage = "/bathroom";
            _chatService.Update(chat.ChatId, chat);
            return new Answer()
            {
                Text = DevelopWarning,
                StickerId = StickersId.DevelopWarningSticker
            };

        }
        private Answer GoToKitchen()
        {
            var accessCheck = CheckStatusIsInactiveOrNull();
            if (accessCheck != null)
                return accessCheck;

            string toSendText = string.Format(kitchenCommand, pet.Satiety);

            List<CommandModel> inlineParts = new InlineItems().InlineFood;
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            var aud = _allUsersService.Get(_userId);
            aud.KitchenCommandCounter++;
            _allUsersService.Update(aud);

            chat.LastMessage = "/kitchen";
            _chatService.Update(chat.ChatId, chat);
            return new Answer()
            {
                Text = toSendText,
                StickerId = StickersId.PetKitchen_Cat,
                InlineKeyboardMarkup = toSendInline
            };

        }
        private Answer GoToGameroom()
        {
            var accessCheck = CheckStatusIsInactiveOrNull();
            if (accessCheck != null)
                return accessCheck;

            string toSendText = string.Format(gameroomCommand, pet.Fatigue, pet.Joy);

            List<CommandModel> inlineParts = new InlineItems().InlineGames;
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            var aud = _allUsersService.Get(_userId);
            aud.GameroomCommandCounter++;
            _allUsersService.Update(aud);

            chat.LastMessage = "/gameroom";
            _chatService.Update(chat.ChatId, chat);
            return new Answer()
            {
                Text = toSendText,
                StickerId = StickersId.PetGameroom_Cat,
                InlineKeyboardMarkup = toSendInline
            };

        }
        private Answer GoToHospital()
        {
            var accessCheck = CheckStatusIsInactiveOrNull();
            if (accessCheck != null)
                return accessCheck;

            string commandHospital =  pet.HP switch
            {
                >= 80 => hospitalCommandHighHp,
                >20 and < 80 => hospitalCommandMidHp,
                _ => hospitalCommandLowHp
            };

            string stickerHospital =  pet.HP switch
            {
                >= 80 => StickersId.PetHospitalHighHP_Cat,
                >20 and < 80 => StickersId.PetHospitalMidHP_Cat,
                _ => StickersId.PetHospitalLowHP_Cat
            };

            string toSendText = string.Format(commandHospital, pet.HP);

            List<CommandModel> inlineParts = new InlineItems().InlineHospital;
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts);

            var aud = _allUsersService.Get(_userId);
            aud.HospitalCommandCounter++;
            _allUsersService.Update(aud);

            chat.LastMessage = "/hospital";
            _chatService.Update(chat.ChatId, chat);
            return new Answer()
            {
                Text = toSendText,
                StickerId = stickerHospital,
                InlineKeyboardMarkup = toSendInline
            };

        }
        private Answer ShowRankingInfo()
        {
            var topPets = _petService.GetAll()
                .OrderByDescending(p => p.Level)
                .ThenByDescending(p => p.LastUpdateTime)
                .Take(10); //First 10 top-level pets

            string anwserRating = "";

            int counter = 1;
            foreach (var pet in topPets)
            {
                if (counter == 1)
                {
                    var user = _userService.Get(pet.UserId);

                    anwserRating += ranksCommand + "\n\n";
                    anwserRating += "🌟 " + pet.Level + " 🐱 " + pet.Name ?? user.Username ?? user.FirstName + user.LastName;
                    anwserRating += "\n⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯";
                    counter++;
                }
                else
                {
                    anwserRating += "\n";
                    var user = _userService.Get(pet.UserId);

                    string name = pet.Name ?? user.Username ?? user.FirstName + user.LastName;

                    anwserRating += counter + ". " + pet.Level + " 🐱 " + name;
                    counter++;
                }
            }

            var aud = _allUsersService.Get(_userId);
            aud.RanksCommandCounter++;
            _allUsersService.Update(aud);

            chat.LastMessage = "/ranks";
            _chatService.Update(chat.ChatId, chat);
            return new Answer()
            {
                Text = anwserRating,
                StickerId = StickersId.PetRanks_Cat
            };

        }
        private Answer GoToSleep()
        {
            var accessCheck = CheckStatusIsInactiveOrNull(true);
            if (accessCheck != null)
                return accessCheck;

            string toSendText = string.Format(sleepCommand, pet.Name, pet.Fatigue, Extensions.GetCurrentStatus(pet.CurrentStatus));

            InlineKeyboardMarkup toSendInline;
            if (pet.CurrentStatus == (int)CurrentStatus.Sleeping)
            {
                var minutesToWait = pet.Fatigue / Factors.RestFactor;
                string timeToWaitStr = string.Format(sleepCommandInlineShowTime, new DateTime().AddMinutes(minutesToWait).ToString("HH:mm"));

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

            chat.LastMessage = "/sleep";
            _chatService.Update(chat.ChatId, chat);
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
        private Answer RenamePet()
        {
            string toSendText = string.Format(renameCommand);

            var aud = _allUsersService.Get(_userId);
            aud.RenameCommandCounter++;
            _allUsersService.Update(aud);

            chat.LastMessage = "/rename";
            _chatService.Update(chat.ChatId, chat);

            return new Answer()
            {
                Text = toSendText,
                StickerId = StickersId.RenamePetSticker
            };

        }
        private Answer RestartPet()
        {
            string toSendText = string.Format(restartCommand, pet.Name);

            chat.LastMessage = "/restart";
            _chatService.Update(chat.ChatId, chat);

            _petService.Remove(user.UserId);
            _userService.Remove(user.UserId);
            _chatService.Remove(user.UserId);

            return new Answer()
            {
                Text = toSendText,
                StickerId = StickersId.DroppedPetSticker
            };
        }

        private void UpdateUser(long userId)
        {
            _userService.Update(userId, message.From);
            Log.Information($"User {message.From.Username} has been updated in Db");
        }
        private Chat UpdateChat(Message message)
        {
            var chatDb = _chatService.Create(new Chat()
            {
                ChatId = message.Chat.Id,
                Name = message.Chat.Username ?? message.Chat.Title,
                UserId = message.From.Id,
                LastMessage = null
            });
            Log.Information($"Chat {message.Chat.Username ?? message.Chat.Title} has been overadded in Db");
            return chatDb;
        }

        private void UpdateIndicatorEXP(int minuteCounter)
        {
            int toAddExp = minuteCounter * Factors.ExpFactor;

            pet.EXP += toAddExp;

            if (pet.EXP > 100)
            {
                pet.Level += pet.EXP / Factors.ExpToLvl;
                pet.EXP -= (pet.EXP / Factors.ExpToLvl) * Factors.ExpToLvl;
            }
        }
        private void UpdateIndicatorSatiety(int minuteCounter)
        {
            double decreaseSatiety = Math.Round(minuteCounter * Factors.StarvingFactor, 2);
            pet.Satiety -= decreaseSatiety;
            pet.Satiety = Math.Round(pet.Satiety, 2);

            if (pet.Satiety < 0)
            {
                pet.Satiety = 0;
                int tmpSat = (int)decreaseSatiety > 100 ? ((int)decreaseSatiety) - 100 : ((int)decreaseSatiety);
                pet.HP -= tmpSat;

                if (pet.HP < 0)
                    pet.HP = 0;
            }
        }
        private void UpdateIndicatorJoy(int minuteCounter)
        {
            double toDecreaseJoy = Math.Round(minuteCounter * Factors.JoyFactor);

            pet.Joy -= (int)toDecreaseJoy;

            if (pet.Joy < 0)
            {
                pet.Joy = 0;
            }


        }
        private void UpdateIndicatorFatigue(int minuteCounter)
        {
            if (pet.CurrentStatus == (int)CurrentStatus.Active)
            {
                double toAddFatigue = Math.Round(minuteCounter * Factors.FatigueFactor);
                pet.Fatigue += (int)toAddFatigue;
            }
            if (pet.Fatigue > 100)
                pet.Fatigue = 100;
        }
        private void UpdateIndicatorSleeping()
        {
            int minuteSleepingCounter = (int)(DateTime.UtcNow - pet.StartSleepingTime).TotalMinutes;
            double toDecreaseFatigue = Math.Round(minuteSleepingCounter * Factors.RestFactor);
            pet.StartSleepingTime = DateTime.UtcNow;

            pet.Fatigue -= (int)toDecreaseFatigue;

            if (pet.Fatigue <= 0)
            {
                pet.CurrentStatus = (int)CurrentStatus.Active;
                pet.Fatigue = 0;
            }
        }

        #region Inline Answers
        private AnswerCallback ShowBasicInfoInline()
        {
            string toSendText = string.Format(petCommand, pet.Name, pet.HP, pet.EXP, pet.Level, pet.Satiety,
                                                  Extensions.GetFatigue(pet.Fatigue),
                                                  Extensions.GetCurrentStatus(pet.CurrentStatus),
                                                  pet.Joy,
                                                  pet.Gold);
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
        private AnswerCallback ShowExtraInfoInline()
        {
            string toSendText = string.Format(petCommandMoreInfo1, pet.Name, pet.BirthDateTime);
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
        private AnswerCallback FeedWithBreadInline()
        {
            var newStarving = Math.Round(pet.Satiety + FoodFactors.BreadHungerFactor, 2);
            if (newStarving > 100)
                newStarving = 100;

            _petService.UpdateStarving(_userId, newStarving);
            var aud = _allUsersService.Get(_userId);
            aud.BreadEatenCounter++;
            _allUsersService.Update(aud);

            string anwser = string.Format(PetFeedingAnwserCallback, (int)FoodFactors.BreadHungerFactor);
            _bcService.AnswerCallbackQueryAsync(callback.Id, user.UserId, anwser);

            string toSendText = string.Format(kitchenCommand, newStarving);

            if (toSendText.IsEqual(callback.Message.Text))
                return null;

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineFood, 3);

            return new AnswerCallback(toSendText, toSendInline);
        }
        private AnswerCallback FeedWithAppleInline()
        {
            var newStarving = Math.Round(pet.Satiety + FoodFactors.RedAppleHungerFactor, 2);
            if (newStarving > 100)
                newStarving = 100;

            _petService.UpdateStarving(_userId, newStarving);
            var aud = _allUsersService.Get(_userId);
            aud.AppleEatenCounter++;
            _allUsersService.Update(aud);

            string anwser = string.Format(PetFeedingAnwserCallback, (int)FoodFactors.RedAppleHungerFactor);
            _bcService.AnswerCallbackQueryAsync(callback.Id, user.UserId, anwser);

            string toSendText = string.Format(kitchenCommand, newStarving);

            if (toSendText.IsEqual(callback.Message.Text))
                return null;

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineFood, 3);

            return new AnswerCallback(toSendText, toSendInline);
        }
        private AnswerCallback FeedWithChocolateInline()
        {
            var newStarving = Math.Round(pet.Satiety + FoodFactors.ChocolateHungerFactor, 2);
            if (newStarving > 100)
                newStarving = 100;

            _petService.UpdateStarving(_userId, newStarving);
            var aud = _allUsersService.Get(_userId);
            aud.ChocolateEatenCounter++;
            _allUsersService.Update(aud);

            string anwser = string.Format(PetFeedingAnwserCallback, (int)FoodFactors.ChocolateHungerFactor);
            _bcService.AnswerCallbackQueryAsync(callback.Id, user.UserId, anwser);

            string toSendText = string.Format(kitchenCommand, newStarving);

            if (toSendText.IsEqual(callback.Message.Text))
                return null;

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineFood, 3);

            return new AnswerCallback(toSendText, toSendInline);
        }
        private AnswerCallback FeedWithLollipopInline()
        {
            var newStarving = Math.Round(pet.Satiety + FoodFactors.LollipopHungerFactor, 2);
            if (newStarving > 100)
                newStarving = 100;

            _petService.UpdateStarving(_userId, newStarving);
            var aud = _allUsersService.Get(_userId);
            aud.LollypopEatenCounter++;
            _allUsersService.Update(aud);

            string anwser = string.Format(PetFeedingAnwserCallback, (int)FoodFactors.LollipopHungerFactor);
            _bcService.AnswerCallbackQueryAsync(callback.Id, user.UserId, anwser);

            string toSendText = string.Format(kitchenCommand, newStarving);

            if (toSendText.IsEqual(callback.Message.Text))
                return null;

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineFood, 3);

            return new AnswerCallback(toSendText, toSendInline);
        }
        private AnswerCallback PutToSleepInline()
        {
            if (CheckStatusIsInactive(true))
                return null;

            if (pet.CurrentStatus == (int)CurrentStatus.Sleeping)
            {
                _bcService.AnswerCallbackQueryAsync(callback.Id, user.UserId, PetSleepingAlreadyAnwserCallback);

                string toSendText = string.Format(sleepCommand, pet.Name, pet.Fatigue, Extensions.GetCurrentStatus(pet.CurrentStatus));

                var minutesToWait = pet.Fatigue / Factors.RestFactor;

                string timeToWaitStr = string.Format(sleepCommandInlineShowTime, new DateTime().AddMinutes(minutesToWait).ToString("HH:mm"));

                if (toSendText.IsEqual(callback.Message.Text))
                    return null;

                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new List<CommandModel>()
                    {
                        new CommandModel()
                        {
                            Text = timeToWaitStr,
                            CallbackData = "sleepCommandInlinePutToSleep"
                        }
                    });

                return new AnswerCallback(toSendText, toSendInline);
            }
            else
            {
                if (pet.Fatigue < Limits.ToRestMinLimitOfFatigue)
                {
                    _bcService.AnswerCallbackQueryAsync(callback.Id, user.UserId, PetSleepingDoesntWantYetAnwserCallback);
                    string sendTxt = string.Format(sleepCommand, pet.Name, pet.Fatigue, Extensions.GetCurrentStatus(pet.CurrentStatus));

                    InlineKeyboardMarkup toSendInlineWhileActive = Extensions.InlineKeyboardOptimizer(new List<CommandModel>()
                        {
                            new CommandModel()
                            {
                                Text = sleepCommandInlinePutToSleep,
                                CallbackData = "sleepCommandInlinePutToSleep"
                            }
                        });

                    Log.Warning("SLEEP TEST");
                    return new AnswerCallback(sendTxt, toSendInlineWhileActive);
                }

                pet.CurrentStatus = (int)CurrentStatus.Sleeping;
                pet.StartSleepingTime = DateTime.UtcNow;
                _petService.Update(pet.UserId, pet);

                var aud = _allUsersService.Get(_userId);
                aud.SleepenTimesCounter++;
                _allUsersService.Update(aud);

                string toSendText = string.Format(sleepCommand, pet.Name, pet.Fatigue, Extensions.GetCurrentStatus(pet.CurrentStatus));

                var minutesToWait = pet.Fatigue / Factors.RestFactor;

                string timeToWaitStr = string.Format(sleepCommandInlineShowTime, new DateTime().AddMinutes(minutesToWait).ToString("HH:mm"));

                if (toSendText.IsEqual(callback.Message.Text))
                    return null;

                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new List<CommandModel>()
                    {
                        new CommandModel()
                        {
                            Text = timeToWaitStr,
                            CallbackData = "sleepCommandInlinePutToSleep"
                        }
                    });

                return new AnswerCallback(toSendText, toSendInline);
            }
        }
        private AnswerCallback PlayCardInline()
        {
            var newFatigue = pet.Fatigue + Factors.CardGameFatigueFactor;
            if (newFatigue > 100)
                newFatigue = 100;

            var newJoy = pet.Joy + Factors.CardGameJoyFactor;
            if (newJoy > 100)
                newJoy = 100;

            _petService.UpdateFatigue(_userId, newFatigue);
            _petService.UpdateJoy(_userId, newJoy);

            string anwser = string.Format(PetPlayingAnwserCallback, Factors.CardGameFatigueFactor);
            _bcService.AnswerCallbackQueryAsync(callback.Id, user.UserId, anwser);
            var aud = _allUsersService.Get(_userId);
            aud.CardsPlayedCounter++;
            _allUsersService.Update(aud);

            string toSendText = string.Format(gameroomCommand, newFatigue, newJoy);

            if (toSendText.IsEqual(callback.Message.Text))
                return null;

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineGames, 3);

            return new AnswerCallback(toSendText, toSendInline);
        }
        private AnswerCallback PlayDiceInline()
        {
            var newFatigue = pet.Fatigue + Factors.DiceGameFatigueFactor;
            if (newFatigue > 100)
                newFatigue = 100;

            var newJoy = pet.Joy + Factors.DiceGameJoyFactor;
            if (newJoy > 100)
                newJoy = 100;

            _petService.UpdateFatigue(_userId, newFatigue);
            _petService.UpdateJoy(_userId, newJoy);
            var aud = _allUsersService.Get(_userId);
            aud.DicePlayedCounter++;
            _allUsersService.Update(aud);

            string anwser = string.Format(PetPlayingAnwserCallback, Factors.DiceGameFatigueFactor);
            _bcService.AnswerCallbackQueryAsync(callback.Id, user.UserId, anwser);

            string toSendText = string.Format(gameroomCommand, newFatigue, newJoy);

            if (toSendText.IsEqual(callback.Message.Text))
                return null;

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineGames, 3);

            return new AnswerCallback(toSendText, toSendInline);
        }
        private AnswerCallback WorkOnPCInline()
        {
            InlineKeyboardMarkup toSendInline;
            string toSendText;

            if (pet.CurrentStatus == (int)CurrentStatus.Active)
            {
                if (callback.Data == "workCommandInlineShowTime" || callback.Data == null)
                {
                    string toSendTextIfTimeOver = string.Format(workCommand);

                    List<CommandModel> inlineParts = new InlineItems().InlineWork;
                    InlineKeyboardMarkup toSendInlineIfTimeOver = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

                    return new AnswerCallback(toSendTextIfTimeOver, toSendInlineIfTimeOver);
                }


                var newFatigue = pet.Fatigue + Factors.WorkOnPCFatigueFactor;
                if (newFatigue > 100)
                    newFatigue = 100;

                _petService.UpdateFatigue(_userId, newFatigue);
                _petService.UpdateGold(_userId, Rewards.WorkOnPCGoldReward);
                _petService.UpdateCurrentStatus(_userId, (int)CurrentStatus.WorkingOnPC);

                var aud = _allUsersService.Get(_userId);
                aud.GoldEarnedCounter += Rewards.WorkOnPCGoldReward;
                aud.WorkOnPCCounter++;
                _allUsersService.Update(aud);

                var startWorkingTime = DateTime.UtcNow;
                _petService.UpdateStartWorkingTime(_userId, startWorkingTime);

                string anwser = string.Format(PetWorkingAnswerCallback, Factors.WorkOnPCFatigueFactor, Rewards.WorkOnPCGoldReward);
                _bcService.AnswerCallbackQueryAsync(callback.Id, user.UserId, anwser, true);

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
            else if (pet.CurrentStatus == (int)CurrentStatus.WorkingOnPC)
            {
                TimeSpan remainsTime = new TimesToWait().WorkOnPCToWait - (DateTime.UtcNow - pet.StartWorkingTime);

                //if callback handled when time of work is over
                if (remainsTime <= TimeSpan.Zero)
                {
                    pet.CurrentStatus = (int)CurrentStatus.Active;
                    _petService.UpdateCurrentStatus(_userId, pet.CurrentStatus);

                    string toSendTextIfTimeOver = string.Format(workCommand);

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
            _bcService.AnswerCallbackQueryAsync(callback.Id, user.UserId, anwser);

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

        private AnswerCallback CureWithPill()
        {
            var newHP = pet.HP + Factors.PillHPFactor;
            if (newHP > 100)
                newHP = 100;

            var newJoy = pet.Joy + Factors.PillJoyFactor;
            if (newJoy < 0)
                newJoy = 0;

            _petService.UpdateHP(_userId, newHP);
            _petService.UpdateJoy(_userId, newJoy);

            var aud = _allUsersService.Get(_userId);
            aud.PillEatenCounter++;
            _allUsersService.Update(aud);

            string anwser = string.Format(PetCuringAnwserCallback, Factors.PillHPFactor, Factors.PillJoyFactor);
            _bcService.AnswerCallbackQueryAsync(callback.Id, user.UserId, anwser, true);

            string commandHospital =  newHP switch
            {
                >= 80 => hospitalCommandHighHp,
                >20 and < 80 => hospitalCommandMidHp,
                _ => hospitalCommandLowHp
            };

            string toSendText = string.Format(commandHospital, newHP);

            if (toSendText.IsEqual(callback.Message.Text))
                return null;

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineHospital);

            return new AnswerCallback(toSendText, toSendInline);
        }

        #endregion

    }
}
