using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using TamagotchiBot.UserExtensions;
using TamagotchiBot.Models;
using TamagotchiBot.Services;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using User = TamagotchiBot.Models.User;
using Chat = TamagotchiBot.Models.Chat;
using Telegram.Bot;
using static TamagotchiBot.UserExtensions.Constants;
using static TamagotchiBot.Resources.Resources;

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

        public Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup> CreateUser()
        {
            if (message != null)
            {
                _userService.Create(message.From);
                _chatService.Create(new Chat()
                {
                    ChatId = message.Chat.Id,
                    Name = message.Chat.Username ?? message.Chat.Title,
                    UserId = message.From.Id
                });

                Log.Information($"User {message.From.Username} has been added to Db");
                Log.Information($"Chat {message.Chat.Username ?? message.Chat.Title} has been added to Db");

                Culture = new CultureInfo(message.From.LanguageCode);
            }
            else if (callback != null)
            {
                _userService.Create(callback.From);
                _chatService.Create(new Chat()
                {
                    ChatId = callback.Message.Chat.Id,
                    Name = callback.Message.Chat.Username ?? callback.Message.Chat.Title,
                    UserId = callback.Message.From.Id
                });

                Log.Information($"User {callback.Message.From.Username} has been added to Db");
                Log.Information($"Chat {callback.Message.Chat.Username ?? callback.Message.Chat.Title} has been added to Db");

                Culture = new CultureInfo(message.From.LanguageCode);
            }

            bot.SetMyCommandsAsync(Extensions.GetCommands());

            return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(Welcome, WelcomeSticker, null, null);
        }

        public Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup> Process()
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
                chatDb = _chatService.Create(new Chat() { ChatId = message.Chat.Id, Name = message.Chat.Username ?? message.Chat.Title, UserId = message.From.Id });
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

            if (pet.CurrentStatus == 0)
                pet.CurrentStatus = 0;

            if (pet.StartSleepingTime.Year == 1)
                pet.StartSleepingTime = DateTime.UtcNow;



            int minuteCounter = (int)(DateTime.UtcNow - pet.LastUpdateTime).TotalMinutes;
            //EXP
            int toAddExp = minuteCounter * ExpFactor;

            pet.EXP += toAddExp;

            if (pet.EXP > 100)
            {
                pet.Level += pet.EXP / ExpToLvl;
                pet.EXP -= (pet.EXP / ExpToLvl) * ExpToLvl;
            }

            //Hunger
            double toAddHunger = Math.Round(minuteCounter * StarvingFactor, 2);
            pet.Starving += toAddHunger;
            pet.Starving = Math.Round(pet.Starving, 2);

            if (pet.Starving > 100)
            {
                pet.HP -= (int)pet.Starving - 100;

                if (pet.HP < 0)
                    pet.HP = 0;

                pet.Starving = 100;

            }

            //Fatigue
            if (pet.CurrentStatus == 0)
            {
                double toAddFatigue = Math.Round(minuteCounter * FatigueFactor);
                pet.Fatigue += (int)toAddFatigue;
            }
            if (pet.Fatigue > 100)
                pet.Fatigue = 100;

            //Sleeping
            if (pet.CurrentStatus == 1)
            {
                int minuteSleepingCounter = (int)(DateTime.UtcNow - pet.StartSleepingTime).TotalMinutes;
                double toDecreaseFatigue = Math.Round(minuteSleepingCounter * RestFactor);
                pet.StartSleepingTime = DateTime.UtcNow;

                pet.Fatigue -= (int)toDecreaseFatigue;

                if (pet.Fatigue <= 0)
                {
                    pet.CurrentStatus = 0;
                    pet.Fatigue = 0;
                }
            }

            pet.LastUpdateTime = DateTime.UtcNow;

            _petService.Update(messageUser.Id, pet);
        }

        private Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup> CreatePet()
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
                    Type = null,
                    UserId = message.From.Id
                });
                return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(ChooseName, PetChooseName_Cat, null, null);
            }

            if (pet.Name == null)
            {
                _petService.UpdateName(message.From.Id, message.Text);
                return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(ConfirmedName, PetConfirmedName_Cat, null, null);
            }

            return null;
        }

        public Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup> ExtrasHandler() //catching exceptional situations (but not exceptions!)
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
                    case Language.Polski:
                        stickerToSend = PolishLanguageSetSticker;
                        break;
                    case Language.English:
                        stickerToSend = EnglishLanguageSetSticker;
                        break;
                    case Language.Беларуская:
                        stickerToSend = BelarussianLanguageSetSticker;
                        break;
                    case Language.Русский:
                        stickerToSend = RussianLanguageSetSticker;
                        break;
                    default:
                        stickerToSend = null;
                        break;
                }


                return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(ConfirmedLanguage, stickerToSend, new ReplyKeyboardRemove(), null);
            }

            return null;
        }

        public Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup> CommandHandler()
        {
            string textRecieved = message.Text;
            if (textRecieved == null)
                return null;

            textRecieved = textRecieved.ToLower();

            UpdateIndicators();

            if (textRecieved == "/language")
            {
                User user = _userService.UpdateLanguage(message.From.Id, null);
                return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(ChangeLanguage,
                                                                            ChangeLanguageSticker,
                                                                            LanguagesMarkup,
                                                                            null);
            }
            if (textRecieved == "/pet")
            {
                string toSendText = string.Format(petCommand, pet.Name, pet.HP, pet.EXP, pet.Level, pet.Starving,
                                                  Extensions.GetFatigue(pet.Fatigue),
                                                  Extensions.GetCurrentStatus(pet.CurrentStatus));

                return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(toSendText,
                                                                                            PetInfo_Cat,
                                                                                            null,
                                                                                            Extensions.InlineKeyboardOptimizer(new List<Tuple<string, string>>() { new Tuple<string, string>(petCommandInlineExtraInfo, "petCommandInlineExtraInfo") }));
            }

            if (textRecieved == "/bathroom")
            {
                return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(DevelopWarning,
                                                            DevelopWarningSticker,
                                                            null,
                                                            null);
            }

            if (textRecieved == "/kitchen")
            {
                string toSendText = string.Format(kitchenCommand, pet.Starving);

                List<Tuple<string, string>> inlineParts = inlineFood;
                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

                return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(toSendText,
                                                            PetKitchen_Cat,
                                                            null,
                                                            toSendInline);
            }

            if (textRecieved == "/gameroom")
            {
                return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(DevelopWarning,
                                                            DevelopWarningSticker,
                                                            null,
                                                            null);
            }

            if (textRecieved == "/ranks")
            {
                return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(DevelopWarning,
                                                            DevelopWarningSticker,
                                                            null,
                                                            null);
            }

            if (textRecieved == "/sleep")
            {
                string toSendText = string.Format(sleepCommand, pet.Name, pet.Fatigue, Extensions.GetCurrentStatus(pet.CurrentStatus));
                InlineKeyboardMarkup toSendInline;
                if (pet.CurrentStatus == (int)CurrentStatus.Sleeping)
                {
                    var minutesToWait = pet.Fatigue / RestFactor;
                    string timeToWaitStr = string.Format(sleepCommandInlineShowTime, new DateTime().AddMinutes(minutesToWait).ToString("HH:mm"));

                    toSendInline = Extensions.InlineKeyboardOptimizer(new List<Tuple<string, string>>() { new Tuple<string, string>(timeToWaitStr, "sleepCommandInlinePutToSleep") });
                }
                else
                    toSendInline = Extensions.InlineKeyboardOptimizer(new List<Tuple<string, string>>() { new Tuple<string, string>(sleepCommandInlinePutToSleep, "sleepCommandInlinePutToSleep") });

                return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(toSendText,
                                                            PetSleep_Cat,
                                                            null,
                                                            toSendInline);
            }

            if (textRecieved == "/test")
            {
                return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(DevelopWarning,
                                                            DevelopWarningSticker,
                                                            null,
                                                            null);
            }
            if (textRecieved == "/restart")
            {
                return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(DevelopWarning,
                                                            DevelopWarningSticker,
                                                            null,
                                                            null);
            }



            return ExtrasHandler();
        }

        public Tuple<string, InlineKeyboardMarkup> CallbackHandler()
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
                                                  Extensions.GetCurrentStatus(pet.CurrentStatus));
                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new List<Tuple<string, string>>() { new Tuple<string, string>(petCommandInlineExtraInfo, "petCommandInlineExtraInfo") });

                return new Tuple<string, InlineKeyboardMarkup>(toSendText, toSendInline);
            }

            if (callback.Data == "petCommandInlineExtraInfo")
            {
                string toSendText = string.Format(petCommandMoreInfo1, pet.Name, pet.BirthDateTime);
                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new List<Tuple<string, string>>() { new Tuple<string, string>(petCommandInlineBasicInfo, "petCommandInlineBasicInfo") });

                return new Tuple<string, InlineKeyboardMarkup>(toSendText, toSendInline);
            }

            if (callback.Data == "kitchenCommandInlineBread") //56.379999999999995
            {
                var newStarving = Math.Round(pet.Starving - BreadHungerFactor, 2);
                if (newStarving < 0)
                    newStarving = 0;

                _petService.UpdateStarving(callback.From.Id, newStarving);

                string anwser = string.Format(PetFeedingAnwserCallback, (int)BreadHungerFactor);
                bot.AnswerCallbackQueryAsync(callback.Id, anwser);

                string toSendText = string.Format(kitchenCommand, newStarving);

                if (toSendText.IsEqual(callback.Message.Text))
                    return null;

                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineFood, 3);

                return new Tuple<string, InlineKeyboardMarkup>(toSendText, toSendInline);
            }

            if (callback.Data == "kitchenCommandInlineRedApple")
            {
                var newStarving = Math.Round(pet.Starving - RedAppleHungerFactor, 2);
                if (newStarving < 0)
                    newStarving = 0;

                _petService.UpdateStarving(callback.From.Id, newStarving);

                string anwser = string.Format(PetFeedingAnwserCallback, (int)RedAppleHungerFactor);
                bot.AnswerCallbackQueryAsync(callback.Id, anwser);

                string toSendText = string.Format(kitchenCommand, newStarving);

                if (toSendText.IsEqual(callback.Message.Text))
                    return null;

                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineFood, 3);

                return new Tuple<string, InlineKeyboardMarkup>(toSendText, toSendInline);
            }

            if (callback.Data == "kitchenCommandInlineChocolate")
            {
                var newStarving = Math.Round(pet.Starving - ChocolateHungerFactor, 2);
                if (newStarving < 0)
                    newStarving = 0;

                _petService.UpdateStarving(callback.From.Id, newStarving);

                string anwser = string.Format(PetFeedingAnwserCallback, (int)ChocolateHungerFactor);
                bot.AnswerCallbackQueryAsync(callback.Id, anwser);

                string toSendText = string.Format(kitchenCommand, newStarving);

                if (toSendText.IsEqual(callback.Message.Text))
                    return null;

                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineFood, 3);

                return new Tuple<string, InlineKeyboardMarkup>(toSendText, toSendInline);
            }

            if (callback.Data == "kitchenCommandInlineLollipop")
            {
                var newStarving = Math.Round(pet.Starving - LollipopHungerFactor, 2);
                if (newStarving < 0)
                    newStarving = 0;

                _petService.UpdateStarving(callback.From.Id, newStarving);

                string anwser = string.Format(PetFeedingAnwserCallback, (int)LollipopHungerFactor);
                bot.AnswerCallbackQueryAsync(callback.Id, anwser);

                string toSendText = string.Format(kitchenCommand, newStarving);

                if (toSendText.IsEqual(callback.Message.Text))
                    return null;

                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineFood, 3);

                return new Tuple<string, InlineKeyboardMarkup>(toSendText, toSendInline);
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
                    if (pet.Fatigue < ToRestMinLimitOfFatigue)
                    {
                        bot.AnswerCallbackQueryAsync(callback.Id, PetSleepingDoesntWantYetAnwserCallback);
                        bot.EditMessageReplyMarkupAsync(callback.Message.Chat.Id, callback.Message.MessageId, Extensions.InlineKeyboardOptimizer(new List<Tuple<string, string>>() { new Tuple<string, string>(sleepCommandInlinePutToSleep, "sleepCommandInlinePutToSleep") }));
                        return null;
                    }

                    pet.CurrentStatus = (int)CurrentStatus.Sleeping;
                    pet.StartSleepingTime = DateTime.UtcNow;
                    _petService.Update(pet.UserId, pet);

                    string toSendText = string.Format(sleepCommand, pet.Name, pet.Fatigue, Extensions.GetCurrentStatus(pet.CurrentStatus));

                    var minutesToWait = pet.Fatigue / RestFactor;

                    string timeToWaitStr = string.Format(sleepCommandInlineShowTime, new DateTime().AddMinutes(minutesToWait).ToString("HH:mm"));

                    if (toSendText.IsEqual(callback.Message.Text))
                        return null;

                    InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new List<Tuple<string, string>>() { new Tuple<string, string>(timeToWaitStr, "sleepCommandInlinePutToSleep") });

                    return new Tuple<string, InlineKeyboardMarkup>(toSendText, toSendInline);

                }
            }

            return null;
        }
    }
}
