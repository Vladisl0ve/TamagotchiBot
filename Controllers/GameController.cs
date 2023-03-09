using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TamagotchiBot.Models.Anwsers;
using TamagotchiBot.Models.Mongo;
using TamagotchiBot.Services;
using TamagotchiBot.UserExtensions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static TamagotchiBot.Resources.Resources;
using Chat = TamagotchiBot.Models.Mongo.Chat;
using User = TamagotchiBot.Models.Mongo.User;

namespace TamagotchiBot.Controllers
{
    public class GameController
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

        public GameController(ITelegramBotClient bot, UserService userService, PetService petService, ChatService chatService, CallbackQuery callback)
        {
            this.bot = bot;
            _userService = userService;
            _petService = petService;
            this.callback = callback;
            _chatService = chatService;

            GetFromDb();

            Culture = new CultureInfo(user?.Culture ?? "en");
        }

        public GameController(ITelegramBotClient bot, UserService userService, PetService petService, ChatService chatService, Message message)
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

            return new Answer(ChangeLanguage,
                               Constants.StickersId.ChangeLanguageSticker,
                               Constants.LanguagesMarkup,
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

            if (userMessage.Username != userDb.Username || userMessage.LastName != userDb.LastName || userMessage.FirstName != userDb.FirstName)
            {
                _userService.Update(userDb.UserId, message.From);
                Log.Information($"User {message.From.Username} has been updated in Db");
            }

            if (chatDb == null)
            {
                chatDb = _chatService.Create(new Chat() { ChatId = message.Chat.Id, Name = message.Chat.Username ?? message.Chat.Title, UserId = message.From.Id, LastMessage = null });
                Log.Information($"Chat {message.Chat.Username ?? message.Chat.Title} has been overadded in Db");
            }

            if (message.Chat.Username != null && chatDb.Name != message.Chat.Username)
            {
                _chatService.Update(message.Chat.Id, new Chat()
                {
                    Id = chatDb.Id,
                    Name = message.Chat.Username,
                    ChatId = chatDb.ChatId,
                    UserId = message.From.Id
                });
                Log.Information($"Chat {message.Chat.Username} has been updated in Db");
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
                Log.Information($"Chat {message.Chat.Title} has been updated in Db");
            }

            if ((message.Text != null && message.Text == "/language") || (userDb.Culture == null && message.Text != null))
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

            //just for avoid nullPointerExeption
            if (pet.LastUpdateTime.Year == 1)
                pet.LastUpdateTime = DateTime.UtcNow;

            if (pet.StartSleepingTime.Year == 1)
                pet.StartSleepingTime = DateTime.UtcNow;



            int minuteCounter = (int)(DateTime.UtcNow - pet.LastUpdateTime).TotalMinutes;
            //EXP
            int toAddExp = minuteCounter * Constants.Factors.ExpFactor;

            pet.EXP += toAddExp;

            if (pet.EXP > 100)
            {
                pet.Level += pet.EXP / Constants.Factors.ExpToLvl;
                pet.EXP -= (pet.EXP / Constants.Factors.ExpToLvl) * Constants.Factors.ExpToLvl;
            }

            //Hunger
            double toAddHunger = Math.Round(minuteCounter * Constants.Factors.StarvingFactor, 2);
            pet.Starving += toAddHunger;
            pet.Starving = Math.Round(pet.Starving, 2);

            if (pet.Starving > 100)
            {
                pet.HP -= (int)pet.Starving - 100;

                if (pet.HP < 0)
                    pet.HP = 0;

                pet.Starving = 100;

            }

            //Joy
            double toDecreaseJoy = Math.Round(minuteCounter * Constants.Factors.JoyFactor);

            pet.Joy -= (int)toDecreaseJoy;

            if (pet.Joy < 0)
            {
                pet.Joy = 0;
            }

            //Fatigue
            if (pet.CurrentStatus == (int)Constants.CurrentStatus.Active)
            {
                double toAddFatigue = Math.Round(minuteCounter * Constants.Factors.FatigueFactor);
                pet.Fatigue += (int)toAddFatigue;
            }
            if (pet.Fatigue > 100)
                pet.Fatigue = 100;

            //Sleeping
            if (pet.CurrentStatus == (int)Constants.CurrentStatus.Sleeping)
            {
                int minuteSleepingCounter = (int)(DateTime.UtcNow - pet.StartSleepingTime).TotalMinutes;
                double toDecreaseFatigue = Math.Round(minuteSleepingCounter * Constants.Factors.RestFactor);
                pet.StartSleepingTime = DateTime.UtcNow;

                pet.Fatigue -= (int)toDecreaseFatigue;

                if (pet.Fatigue <= 0)
                {
                    pet.CurrentStatus = (int)Constants.CurrentStatus.Active;
                    pet.Fatigue = 0;
                }
            }

            pet.LastUpdateTime = DateTime.UtcNow;

            _petService.Update(messageUser.Id, pet);
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
                    Starving = 0,
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
                    StickerId = Constants.StickersId.WelcomeSticker,
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
                    StickerId = Constants.StickersId.PetChooseName_Cat,
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
                    StickerId = Constants.StickersId.PetConfirmedName_Cat,
                    ReplyMarkup = null,
                    InlineKeyboardMarkup = null
                };
            }

            return null;
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

                string stickerToSend;

                switch (culture.Language())
                {
                    case Constants.Language.Polish:
                        stickerToSend = Constants.StickersId.PolishLanguageSetSticker;
                        break;
                    case Constants.Language.English:
                        stickerToSend = Constants.StickersId.EnglishLanguageSetSticker;
                        break;
                    case Constants.Language.Belarusian:
                        stickerToSend = Constants.StickersId.BelarussianLanguageSetSticker;
                        break;
                    case Constants.Language.Russian:
                        stickerToSend = Constants.StickersId.RussianLanguageSetSticker;
                        break;
                    default:
                        stickerToSend = null;
                        break;
                }


                return new Answer()
                {
                    Text = ConfirmedLanguage,
                    StickerId = stickerToSend,
                    ReplyMarkup = new ReplyKeyboardRemove(),
                    InlineKeyboardMarkup = null
                };
            }

            else if (chat.LastMessage == "/rename")
            {
                _petService.UpdateName(user.UserId, message.Text);

                return new Answer()
                {
                    Text = ConfirmedName,
                    StickerId = Constants.StickersId.WelcomeSticker,
                    ReplyMarkup = new ReplyKeyboardRemove(),
                    InlineKeyboardMarkup = null
                };

            }

            return null;
        }

        public Answer CommandHandler()
        {
            string textRecieved = message.Text;
            if (textRecieved == null)
                return null;

            textRecieved = textRecieved.ToLower();

            UpdateIndicators();

            if (textRecieved == "/language")
            {
                _ = _userService.UpdateLanguage(message.From.Id, null);


                return new Answer()
                {
                    Text = ChangeLanguage,
                    StickerId = Constants.StickersId.ChangeLanguageSticker,
                    ReplyMarkup = Constants.LanguagesMarkup,
                    InlineKeyboardMarkup = null
                };
            }
            if (textRecieved == "/pet")
            {
                string toSendText = string.Format(petCommand, pet.Name, pet.HP, pet.EXP, pet.Level, pet.Starving,
                                                  Extensions.GetFatigue(pet.Fatigue),
                                                  Extensions.GetCurrentStatus(pet.CurrentStatus),
                                                  pet.Joy);

                chat.LastMessage = "/pet";
                _chatService.Update(chat.ChatId, chat);
                return new Answer()
                {
                    Text = toSendText,
                    StickerId = Constants.StickersId.PetInfo_Cat,
                    ReplyMarkup = null,
                    InlineKeyboardMarkup = Extensions.InlineKeyboardOptimizer(new List<Tuple<string, string>>() { new Tuple<string, string>(petCommandInlineExtraInfo, "petCommandInlineExtraInfo")
                    })
                };
            }

            if (textRecieved == "/bathroom")
            {
                if (pet.CurrentStatus == (int)Constants.CurrentStatus.Sleeping)
                {
                    string denyText = string.Format(denyAccessSleeping);
                    return new Answer()
                    {
                        Text = denyText,
                        StickerId = Constants.StickersId.PetBusy_Cat,
                        ReplyMarkup = null,
                        InlineKeyboardMarkup = null
                    };
                }

                chat.LastMessage = "/bathroom";
                _chatService.Update(chat.ChatId, chat);
                return new Answer()
                {
                    Text = DevelopWarning,
                    StickerId = Constants.StickersId.DevelopWarningSticker,
                    ReplyMarkup = null,
                    InlineKeyboardMarkup = null
                };
            }

            if (textRecieved == "/kitchen")
            {
                if (pet.CurrentStatus == (int)Constants.CurrentStatus.Sleeping)
                {
                    string denyText = string.Format(denyAccessSleeping);
                    return new Answer()
                    {
                        Text = denyText,
                        StickerId = Constants.StickersId.PetBusy_Cat,
                        ReplyMarkup = null,
                        InlineKeyboardMarkup = null
                    };
                }
                string toSendText = string.Format(kitchenCommand, pet.Starving);

                List<Tuple<string, string>> inlineParts = Constants.inlineFood;
                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

                chat.LastMessage = "/kitchen";
                _chatService.Update(chat.ChatId, chat);
                return new Answer()
                {
                    Text = toSendText,
                    StickerId = Constants.StickersId.PetKitchen_Cat,
                    ReplyMarkup = null,
                    InlineKeyboardMarkup = toSendInline
                };
            }

            if (textRecieved == "/gameroom")
            {
                if (pet.CurrentStatus == (int)Constants.CurrentStatus.Sleeping)
                {
                    string denyText = string.Format(denyAccessSleeping);
                    return new Answer()
                    {
                        Text = denyText,
                        StickerId = Constants.StickersId.PetBusy_Cat,
                        ReplyMarkup = null,
                        InlineKeyboardMarkup = null
                    };
                }

                string toSendText = string.Format(gameroomCommand, pet.Fatigue, pet.Joy);

                List<Tuple<string, string>> inlineParts = Constants.inlineGames;
                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

                chat.LastMessage = "/gameroom";
                _chatService.Update(chat.ChatId, chat);
                return new Answer() { Text = toSendText, StickerId = Constants.StickersId.PetGameroom_Cat, ReplyMarkup = null, InlineKeyboardMarkup = toSendInline };
            }

            if (textRecieved == "/ranks")
            {

                var topPets = _petService.Get().OrderByDescending(p => p.Level).Take(10); //First 10 top-level pets
                string anwserRating = "";

                int counter = 1;
                foreach (var pet in topPets)
                {
                    if (counter == 1)
                    {
                        var user = _userService.Get(pet.UserId);

                        anwserRating += ranksCommand + "\n\n";
                        anwserRating += "🌟 " + pet.Level + " 🐱 " + user.Username ?? user.FirstName + user.LastName;
                        anwserRating += "\n⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯";
                        counter++;
                    }
                    else
                    {
                        anwserRating += "\n";
                        var user = _userService.Get(pet.UserId);

                        string name = user.Username ?? user.FirstName + user.LastName;

                        anwserRating += counter + ". " + pet.Level + " 🐱 " + name;
                        counter++;
                    }
                }


                chat.LastMessage = "/ranks";
                _chatService.Update(chat.ChatId, chat);
                return new Answer()
                {
                    Text = anwserRating,
                    StickerId = Constants.StickersId.PetRanks_Cat,
                    ReplyMarkup = null,
                    InlineKeyboardMarkup = null
                };
            }

            if (textRecieved == "/sleep")
            {
                string toSendText = string.Format(sleepCommand, pet.Name, pet.Fatigue, Extensions.GetCurrentStatus(pet.CurrentStatus));
                InlineKeyboardMarkup toSendInline;
                if (pet.CurrentStatus == (int)Constants.CurrentStatus.Sleeping)
                {
                    var minutesToWait = pet.Fatigue / Constants.Factors.RestFactor;
                    string timeToWaitStr = string.Format(sleepCommandInlineShowTime, new DateTime().AddMinutes(minutesToWait).ToString("HH:mm"));

                    toSendInline = Extensions.InlineKeyboardOptimizer(new List<Tuple<string, string>>() { new Tuple<string, string>(timeToWaitStr, "sleepCommandInlinePutToSleep") });
                }
                else
                    toSendInline = Extensions.InlineKeyboardOptimizer(new List<Tuple<string, string>>() { new Tuple<string, string>(sleepCommandInlinePutToSleep, "sleepCommandInlinePutToSleep") });

                chat.LastMessage = "/sleep";
                _chatService.Update(chat.ChatId, chat);
                return new Answer()
                {
                    Text = toSendText,
                    StickerId = Constants.StickersId.PetSleep_Cat,
                    ReplyMarkup = null,
                    InlineKeyboardMarkup = toSendInline
                };
            }

            if (textRecieved == "/test")
            {
                return new Answer()
                {
                    Text = DevelopWarning,
                    StickerId = Constants.StickersId.DevelopWarningSticker,
                    ReplyMarkup = null,
                    InlineKeyboardMarkup = null
                };
            }
            if (textRecieved == "/restart")
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
                    StickerId = Constants.StickersId.DroppedPetSticker,
                    ReplyMarkup = null,
                    InlineKeyboardMarkup = null
                };
            }
            if (textRecieved == "/rename")
            {
                string toSendText = string.Format(renameCommand);

                chat.LastMessage = "/rename";
                _chatService.Update(chat.ChatId, chat);

                return new Answer()
                {
                    Text = toSendText,
                    StickerId = Constants.StickersId.RenamePetSticker,
                    ReplyMarkup = null,
                    InlineKeyboardMarkup = null
                };
            }




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
                string toSendText = string.Format(petCommand, pet.Name, pet.HP, pet.EXP, pet.Level, pet.Starving,
                                                  Extensions.GetFatigue(pet.Fatigue),
                                                  Extensions.GetCurrentStatus(pet.CurrentStatus),
                                                  pet.Joy);
                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new List<Tuple<string, string>>() { new Tuple<string, string>(petCommandInlineExtraInfo, "petCommandInlineExtraInfo") });

                return new AnswerCallback(toSendText, toSendInline);
            }

            if (callback.Data == "petCommandInlineExtraInfo")
            {
                string toSendText = string.Format(petCommandMoreInfo1, pet.Name, pet.BirthDateTime);
                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new List<Tuple<string, string>>() { new Tuple<string, string>(petCommandInlineBasicInfo, "petCommandInlineBasicInfo") });

                return new AnswerCallback(toSendText, toSendInline);
            }

            if (callback.Data == "kitchenCommandInlineBread") //56.379999999999995
            {
                var newStarving = Math.Round(pet.Starving - Constants.FoodFactors.BreadHungerFactor, 2);
                if (newStarving < 0)
                    newStarving = 0;

                _petService.UpdateStarving(callback.From.Id, newStarving);

                string anwser = string.Format(PetFeedingAnwserCallback, (int)Constants.FoodFactors.BreadHungerFactor);
                bot.AnswerCallbackQueryAsync(callback.Id, anwser);

                string toSendText = string.Format(kitchenCommand, newStarving);

                if (toSendText.IsEqual(callback.Message.Text))
                    return null;

                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(Constants.inlineFood, 3);

                return new AnswerCallback(toSendText, toSendInline);
            }

            if (callback.Data == "kitchenCommandInlineRedApple")
            {
                var newStarving = Math.Round(pet.Starving - Constants.FoodFactors.RedAppleHungerFactor, 2);
                if (newStarving < 0)
                    newStarving = 0;

                _petService.UpdateStarving(callback.From.Id, newStarving);

                string anwser = string.Format(PetFeedingAnwserCallback, (int)Constants.FoodFactors.RedAppleHungerFactor);
                bot.AnswerCallbackQueryAsync(callback.Id, anwser);

                string toSendText = string.Format(kitchenCommand, newStarving);

                if (toSendText.IsEqual(callback.Message.Text))
                    return null;

                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(Constants.inlineFood, 3);

                return new AnswerCallback(toSendText, toSendInline);
            }

            if (callback.Data == "kitchenCommandInlineChocolate")
            {
                var newStarving = Math.Round(pet.Starving - Constants.FoodFactors.ChocolateHungerFactor, 2);
                if (newStarving < 0)
                    newStarving = 0;

                _petService.UpdateStarving(callback.From.Id, newStarving);

                string anwser = string.Format(PetFeedingAnwserCallback, (int)Constants.FoodFactors.ChocolateHungerFactor);
                bot.AnswerCallbackQueryAsync(callback.Id, anwser);

                string toSendText = string.Format(kitchenCommand, newStarving);

                if (toSendText.IsEqual(callback.Message.Text))
                    return null;

                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(Constants.inlineFood, 3);

                return new AnswerCallback(toSendText, toSendInline);
            }

            if (callback.Data == "kitchenCommandInlineLollipop")
            {
                var newStarving = Math.Round(pet.Starving - Constants.FoodFactors.LollipopHungerFactor, 2);
                if (newStarving < 0)
                    newStarving = 0;

                _petService.UpdateStarving(callback.From.Id, newStarving);

                string anwser = string.Format(PetFeedingAnwserCallback, (int)Constants.FoodFactors.LollipopHungerFactor);
                bot.AnswerCallbackQueryAsync(callback.Id, anwser);

                string toSendText = string.Format(kitchenCommand, newStarving);

                if (toSendText.IsEqual(callback.Message.Text))
                    return null;

                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(Constants.inlineFood, 3);

                return new AnswerCallback(toSendText, toSendInline);
            }

            if (callback.Data == "sleepCommandInlinePutToSleep")
            {

                if (pet.CurrentStatus == (int)Constants.CurrentStatus.Sleeping)
                {
                    bot.AnswerCallbackQueryAsync(callback.Id, PetSleepingAlreadyAnwserCallback);
                    return null;
                }

                if (pet.CurrentStatus == (int)Constants.CurrentStatus.Active)
                {
                    if (pet.Fatigue < Constants.Limits.ToRestMinLimitOfFatigue)
                    {
                        bot.AnswerCallbackQueryAsync(callback.Id, PetSleepingDoesntWantYetAnwserCallback);
                        bot.EditMessageReplyMarkupAsync(callback.Message.Chat.Id, callback.Message.MessageId, Extensions.InlineKeyboardOptimizer(new List<Tuple<string, string>>() { new Tuple<string, string>(sleepCommandInlinePutToSleep, "sleepCommandInlinePutToSleep") }));
                        return null;
                    }

                    pet.CurrentStatus = (int)Constants.CurrentStatus.Sleeping;
                    pet.StartSleepingTime = DateTime.UtcNow;
                    _petService.Update(pet.UserId, pet);

                    string toSendText = string.Format(sleepCommand, pet.Name, pet.Fatigue, Extensions.GetCurrentStatus(pet.CurrentStatus));

                    var minutesToWait = pet.Fatigue / Constants.Factors.RestFactor;

                    string timeToWaitStr = string.Format(sleepCommandInlineShowTime, new DateTime().AddMinutes(minutesToWait).ToString("HH:mm"));

                    if (toSendText.IsEqual(callback.Message.Text))
                        return null;

                    InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new List<Tuple<string, string>>() { new Tuple<string, string>(timeToWaitStr, "sleepCommandInlinePutToSleep") });

                    return new AnswerCallback(toSendText, toSendInline);

                }
            }

            if (callback.Data == "gameroomCommandInlineCard")
            {
                var newFatigue = pet.Fatigue + Constants.Factors.CardGameFatigueFactor;
                if (newFatigue > 100)
                    newFatigue = 100;

                var newJoy = pet.Joy + Constants.Factors.CardGameJoyFactor;
                if (newJoy > 100)
                    newJoy = 100;

                _petService.UpdateFatigue(callback.From.Id, newFatigue);
                _petService.UpdateJoy(callback.From.Id, newJoy);

                string anwser = string.Format(PetPlayingAnwserCallback, Constants.Factors.CardGameFatigueFactor);
                bot.AnswerCallbackQueryAsync(callback.Id, anwser);

                string toSendText = string.Format(gameroomCommand, newFatigue, newJoy);

                if (toSendText.IsEqual(callback.Message.Text))
                    return null;

                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(Constants.inlineGames, 3);

                return new AnswerCallback(toSendText, toSendInline);
            }
            if (callback.Data == "gameroomCommandInlineDice")
            {
                var newFatigue = pet.Fatigue + Constants.Factors.DiceGameFatigueFactor;
                if (newFatigue > 100)
                    newFatigue = 100;

                var newJoy = pet.Joy + Constants.Factors.DiceGameJoyFactor;
                if (newJoy > 100)
                    newJoy = 100;

                _petService.UpdateFatigue(callback.From.Id, newFatigue);
                _petService.UpdateJoy(callback.From.Id, newJoy);

                string anwser = string.Format(PetPlayingAnwserCallback, Constants.Factors.DiceGameFatigueFactor);
                bot.AnswerCallbackQueryAsync(callback.Id, anwser);

                string toSendText = string.Format(gameroomCommand, newFatigue, newJoy);

                if (toSendText.IsEqual(callback.Message.Text))
                    return null;

                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(Constants.inlineGames, 3);

                return new AnswerCallback(toSendText, toSendInline);
            }
            return null;
        }
    }
}
