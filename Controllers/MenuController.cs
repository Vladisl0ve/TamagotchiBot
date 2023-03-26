using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TamagotchiBot.Models;
using TamagotchiBot.Models.Answers;
using TamagotchiBot.Models.Mongo;
using TamagotchiBot.Services;
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

        private readonly ITelegramBotClient bot;
        private readonly Message message;
        private readonly CallbackQuery callback;

        private User user;
        private Pet pet;
        private Chat chat;

        public MenuController(ITelegramBotClient bot, UserService userService, PetService petService, ChatService chatService, CallbackQuery callback)
        {
            this.bot = bot;
            _userService = userService;
            _petService = petService;
            this.callback = callback;
            _chatService = chatService;

            GetFromDb();

            Culture = new CultureInfo(user?.Culture ?? "en");
        }

        public MenuController(ITelegramBotClient bot, UserService userService, PetService petService, ChatService chatService, Message message)
        {
            this.bot = bot;
            _userService = userService;
            _petService = petService;
            this.message = message;
            _chatService = chatService;

            GetFromDb();

            Culture = new CultureInfo(user?.Culture ?? "en");
        }

        private void GetFromDb()
        {
            if (message != null)
                user = _userService.Get(message.From.Id);
            else if (callback != null)
                user = _userService.Get(callback.From.Id);

            if (user != null)
            {
                pet = _petService.Get(user.UserId);
                chat = _chatService.Get(message?.Chat.Id ?? callback.Message.Chat.Id);
            }
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
                    UserId = message.From.Id,
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
                    UserId = message.From.Id
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
                    UserId = message.From.Id
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

            pet.LastUpdateTime = DateTime.UtcNow;

            _petService.Update(messageUser.Id, pet);
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
                DeleteAllUserData();
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
#if DEBUG //only for debug purpose
            if (textReceived == "/restart")
                return RestartPet();
#endif

            return ExtrasHandler();
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
            {
                string toSendText = string.Format(petCommand, pet.Name, pet.HP, pet.EXP, pet.Level, pet.Satiety,
                                                  Extensions.GetFatigue(pet.Fatigue),
                                                  Extensions.GetCurrentStatus(pet.CurrentStatus),
                                                  pet.Joy);
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

            if (callback.Data == "petCommandInlineExtraInfo")
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
                return new AnswerCallback(toSendText, toSendInline);
            }

            if (callback.Data == "kitchenCommandInlineBread") //56.379999999999995
            {
                var newStarving = Math.Round(pet.Satiety + FoodFactors.BreadHungerFactor, 2);
                if (newStarving > 100)
                    newStarving = 100;

                _petService.UpdateStarving(callback.From.Id, newStarving);

                string anwser = string.Format(PetFeedingAnwserCallback, (int)FoodFactors.BreadHungerFactor);
                bot.AnswerCallbackQueryAsync(callback.Id, anwser);

                string toSendText = string.Format(kitchenCommand, newStarving);

                if (toSendText.IsEqual(callback.Message.Text))
                    return null;

                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineFood, 3);

                return new AnswerCallback(toSendText, toSendInline);
            }

            if (callback.Data == "kitchenCommandInlineRedApple")
            {
                var newStarving = Math.Round(pet.Satiety + FoodFactors.RedAppleHungerFactor, 2);
                if (newStarving > 100)
                    newStarving = 100;

                _petService.UpdateStarving(callback.From.Id, newStarving);

                string anwser = string.Format(PetFeedingAnwserCallback, (int)FoodFactors.RedAppleHungerFactor);
                bot.AnswerCallbackQueryAsync(callback.Id, anwser);

                string toSendText = string.Format(kitchenCommand, newStarving);

                if (toSendText.IsEqual(callback.Message.Text))
                    return null;

                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineFood, 3);

                return new AnswerCallback(toSendText, toSendInline);
            }

            if (callback.Data == "kitchenCommandInlineChocolate")
            {
                var newStarving = Math.Round(pet.Satiety + FoodFactors.ChocolateHungerFactor, 2);
                if (newStarving > 100)
                    newStarving = 100;

                _petService.UpdateStarving(callback.From.Id, newStarving);

                string anwser = string.Format(PetFeedingAnwserCallback, (int)FoodFactors.ChocolateHungerFactor);
                bot.AnswerCallbackQueryAsync(callback.Id, anwser);

                string toSendText = string.Format(kitchenCommand, newStarving);

                if (toSendText.IsEqual(callback.Message.Text))
                    return null;

                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineFood, 3);

                return new AnswerCallback(toSendText, toSendInline);
            }

            if (callback.Data == "kitchenCommandInlineLollipop")
            {
                var newStarving = Math.Round(pet.Satiety + FoodFactors.LollipopHungerFactor, 2);
                if (newStarving > 100)
                    newStarving = 100;

                _petService.UpdateStarving(callback.From.Id, newStarving);

                string anwser = string.Format(PetFeedingAnwserCallback, (int)FoodFactors.LollipopHungerFactor);
                bot.AnswerCallbackQueryAsync(callback.Id, anwser);

                string toSendText = string.Format(kitchenCommand, newStarving);

                if (toSendText.IsEqual(callback.Message.Text))
                    return null;

                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineFood, 3);

                return new AnswerCallback(toSendText, toSendInline);
            }

            if (callback.Data == "sleepCommandInlinePutToSleep")
            {

                if (pet.CurrentStatus == (int)CurrentStatus.Sleeping)
                {
                    bot.AnswerCallbackQueryAsync(callback.Id, PetSleepingAlreadyAnwserCallback);
                    return null;
                }

                if (pet.CurrentStatus == (int)CurrentStatus.Active)
                {
                    if (pet.Fatigue < Limits.ToRestMinLimitOfFatigue)
                    {
                        bot.AnswerCallbackQueryAsync(callback.Id, PetSleepingDoesntWantYetAnwserCallback);
                        bot.EditMessageReplyMarkupAsync(callback.Message.Chat.Id, callback.Message.MessageId, Extensions.InlineKeyboardOptimizer(new List<CommandModel>()
                        {
                            new CommandModel()
                            {
                                Text = sleepCommandInlinePutToSleep,
                                CallbackData = "sleepCommandInlinePutToSleep"
                            }
                        }));
                        return null;
                    }

                    pet.CurrentStatus = (int)CurrentStatus.Sleeping;
                    pet.StartSleepingTime = DateTime.UtcNow;
                    _petService.Update(pet.UserId, pet);

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

            if (callback.Data == "gameroomCommandInlineCard")
            {
                var newFatigue = pet.Fatigue + Factors.CardGameFatigueFactor;
                if (newFatigue > 100)
                    newFatigue = 100;

                var newJoy = pet.Joy + Factors.CardGameJoyFactor;
                if (newJoy > 100)
                    newJoy = 100;

                _petService.UpdateFatigue(callback.From.Id, newFatigue);
                _petService.UpdateJoy(callback.From.Id, newJoy);

                string anwser = string.Format(PetPlayingAnwserCallback, Factors.CardGameFatigueFactor);
                bot.AnswerCallbackQueryAsync(callback.Id, anwser);

                string toSendText = string.Format(gameroomCommand, newFatigue, newJoy);

                if (toSendText.IsEqual(callback.Message.Text))
                    return null;

                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineGames, 3);

                return new AnswerCallback(toSendText, toSendInline);
            }
            if (callback.Data == "gameroomCommandInlineDice")
            {
                var newFatigue = pet.Fatigue + Factors.DiceGameFatigueFactor;
                if (newFatigue > 100)
                    newFatigue = 100;

                var newJoy = pet.Joy + Factors.DiceGameJoyFactor;
                if (newJoy > 100)
                    newJoy = 100;

                _petService.UpdateFatigue(callback.From.Id, newFatigue);
                _petService.UpdateJoy(callback.From.Id, newJoy);

                string anwser = string.Format(PetPlayingAnwserCallback, Factors.DiceGameFatigueFactor);
                bot.AnswerCallbackQueryAsync(callback.Id, anwser);

                string toSendText = string.Format(gameroomCommand, newFatigue, newJoy);

                if (toSendText.IsEqual(callback.Message.Text))
                    return null;

                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new InlineItems().InlineGames, 3);

                return new AnswerCallback(toSendText, toSendInline);
            }
            if (callback.Data == "hospitalCommandCurePills")
            {
                var newHP = pet.HP + Factors.PillHPFactor;
                if (newHP > 100)
                    newHP = 100;

                var newJoy = pet.Joy + Factors.PillJoyFactor;
                if (newJoy < 0)
                    newJoy = 0;

                _petService.UpdateHP(callback.From.Id, newHP);
                _petService.UpdateJoy(callback.From.Id, newJoy);

                string anwser = string.Format(PetCuringAnwserCallback, Factors.PillHPFactor, Factors.PillJoyFactor);
                bot.AnswerCallbackQueryAsync(callback.Id, anwser, true);

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
                    UserId = message.From.Id
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
                _petService.Update(message.From.Id, pet);
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
                _petService.UpdateName(message.From.Id, message.Text);
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
        private void DeleteAllUserData()
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
            _ = _userService.UpdateLanguage(message.From.Id, null);


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
                                                  pet.Joy);

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
        private Answer GoToBathroom()
        {
            if (pet.CurrentStatus == (int)CurrentStatus.Sleeping)
            {
                string denyText = string.Format(denyAccessSleeping);
                return new Answer()
                {
                    Text = denyText,
                    StickerId = StickersId.PetBusy_Cat
                };
            }

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
            if (pet.CurrentStatus == (int)CurrentStatus.Sleeping)
            {
                string denyText = string.Format(denyAccessSleeping);
                return new Answer()
                {
                    Text = denyText,
                    StickerId = StickersId.PetBusy_Cat,
                    ReplyMarkup = null,
                    InlineKeyboardMarkup = null
                };
            }
            string toSendText = string.Format(kitchenCommand, pet.Satiety);

            List<CommandModel> inlineParts = new InlineItems().InlineFood;
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

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
            if (pet.CurrentStatus == (int)CurrentStatus.Sleeping)
            {
                string denyText = string.Format(denyAccessSleeping);
                return new Answer()
                {
                    Text = denyText,
                    StickerId = StickersId.PetBusy_Cat,
                    ReplyMarkup = null,
                    InlineKeyboardMarkup = null
                };
            }

            string toSendText = string.Format(gameroomCommand, pet.Fatigue, pet.Joy);

            List<CommandModel> inlineParts = new InlineItems().InlineGames;
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

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
            if (pet.CurrentStatus == (int)CurrentStatus.Sleeping)
            {
                string denyText = string.Format(denyAccessSleeping);
                return new Answer()
                {
                    Text = denyText,
                    StickerId = StickersId.PetBusy_Cat,
                    ReplyMarkup = null,
                    InlineKeyboardMarkup = null
                };
            }

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

            return new Answer()
            {
                Text = toSendText,
                StickerId = StickersId.HelpCommandSticker
            };

        }
        private Answer ShowMenuInfo()
        {
            string toSendText = string.Format(menuCommand);

            return new Answer()
            {
                Text = toSendText,
                StickerId = StickersId.MenuCommandSticker
            };
        }
        private Answer RenamePet()
        {
            string toSendText = string.Format(renameCommand);

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
    }
}
