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

        private CultureInfo localCulture;

        public GameController(ITelegramBotClient bot, UserService userService, PetService petService, ChatService chatService, CallbackQuery callback)
        {
            this.bot = bot;
            _userService = userService;
            _petService = petService;
            this.callback = callback;
            _chatService = chatService;
        }

        public GameController(ITelegramBotClient bot, UserService userService, PetService petService, ChatService chatService, Message message)
        {
            this.bot = bot;
            _userService = userService;
            _petService = petService;
            this.message = message;
            _chatService = chatService;
        }

        public Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup> CreateUser()
        {
            _userService.Create(message.From);
            _chatService.Create(new Models.Chat()
            {
                ChatId = message.Chat.Id,
                Name = message.Chat.Username ?? message.Chat.Title,
                UserId = message.From.Id
            });

            Log.Information($"User {message.From.Username} has been added to Db");
            Log.Information($"Chat {message.Chat.Username ?? message.Chat.Title} has been added to Db");

            localCulture = new CultureInfo(message.From.LanguageCode);
            Resources.Resources.Culture = localCulture;

            return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(Resources.Resources.Welcome, Constants.WelcomeSticker, null, null);
        }

        public Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup> Process()
        {
            var userMessage = message.From;
            User userDb = _userService.Get(userMessage.Id);
            Chat chatDb = _chatService.Get(message.Chat.Id);

            if (userDb.Culture != null)
            {
                localCulture = new CultureInfo(userDb.Culture);
                Resources.Resources.Culture = localCulture;
            }

            if (userMessage.Username != userDb.Username || userMessage.LastName != userDb.LastName || userMessage.FirstName != userDb.FirstName)
            {
                _userService.Update(userDb.UserId, message.From);
                Log.Information($"User {message.From.Username} has been updated in Db");
            }

            if (chatDb == null)
            {
                _chatService.Create(new Chat() { ChatId = message.Chat.Id, Name = message.Chat.Username ?? message.Chat.Title, UserId = message.From.Id });
                chatDb = _chatService.Get(message.Chat.Id);
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
            else if (message.Chat.Title != null && chatDb.Name != message.Chat.Username)
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

            var pet = _petService.Get(userDb.UserId);

            if ((message.Text != null && message.Text == "/language") || (userDb.Culture == null && message.Text != null))
                return CommandHandler();

            if (pet == null || pet.Name == null)
                return CreatePet();
            else
                return CommandHandler();

        }

        private void UpdateIndicators()
        {
            Telegram.Bot.Types.User user = message == null ? callback.From : message.From;
            Pet pet = _petService.Get(user.Id);

            if (pet.LastUpdateTime.Year == 1)
                pet.LastUpdateTime = DateTime.UtcNow;


            int minuteCounter = (int)(DateTime.UtcNow - pet.LastUpdateTime).TotalMinutes;
            //EXP
            int toAddExp = minuteCounter * Constants.ExpFactor;

            pet.EXP += toAddExp;

            if (pet.EXP > 100)
            {
                pet.Level = 0;
                pet.Level += pet.EXP / Constants.ExpToLvl;
                pet.EXP -= (pet.EXP / Constants.ExpToLvl) * Constants.ExpToLvl;
            }

            //Hunger
            double toAddHunger = Math.Round(minuteCounter * Constants.StarvingFactor, 2);
            pet.Starving += toAddHunger;

            if (pet.Starving > 100)
            {
                pet.HP -= (int)pet.Starving - 100;

                if (pet.HP < 0)
                    pet.HP = 0;

                pet.Starving = 100;

            }

            //Fatigue
            double toAddFatigue = Math.Round(minuteCounter * Constants.FatigueFactor);
            pet.Fatigue += (int)toAddFatigue;

            if (pet.Fatigue > 100)
                pet.Fatigue = 100;

            pet.LastUpdateTime = DateTime.UtcNow;

            _petService.Update(user.Id, pet);
        }

        private Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup> CreatePet()
        {
            if (message.Text == null)
                return null;

            Pet pet = _petService.Get(message.From.Id);

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
                return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(Resources.Resources.ChooseName, Constants.PetChooseName_Cat, null, null);
            }

            if (pet.Name == null)
            {
                _petService.UpdateName(message.From.Id, message.Text);
                return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(Resources.Resources.ConfirmedName, Constants.PetConfirmedName_Cat, null, null);
            }

            return null;
        }

        public Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup> ExtrasHandler() //catching exceptional situations (but not exceptions!)
        {
            User user = _userService.Get(message.From.Id);

            if (user.Culture == null)
            {
                if (message.Text == null)
                    return null;

                string last = message.Text.Split(' ').Last();
                string culture = last.Culture();

                if (culture == null)
                    return null;

                localCulture = new CultureInfo(culture);
                Resources.Resources.Culture = localCulture;

                _userService.UpdateLanguage(user.UserId, culture);

                string stickerToSend;

                switch (culture.Language())
                {
                    case Constants.Language.Polski:
                        stickerToSend = Constants.PolishLanguageSetSticker;
                        break;
                    case Constants.Language.English:
                        stickerToSend = Constants.EnglishLanguageSetSticker;
                        break;
                    case Constants.Language.Беларуская:
                        stickerToSend = Constants.BelarussianLanguageSetSticker;
                        break;
                    case Constants.Language.Русский:
                        stickerToSend = Constants.RussianLanguageSetSticker;
                        break;
                    default:
                        stickerToSend = null;
                        break;
                }


                return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(Resources.Resources.ConfirmedLanguage, stickerToSend, new ReplyKeyboardRemove(), null);
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
            if (textRecieved == "/pet")
            {
                Pet pet = _petService.Get(message.From.Id);
                string toSendText = string.Format(Resources.Resources.petCommand, pet.Name, pet.HP, pet.EXP, pet.Level, pet.Starving, Extensions.GetFatigue(pet.Fatigue));

                return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(toSendText,
                                                                                            Constants.PetInfo_Cat,
                                                                                            null,
                                                                                            Extensions.InlineKeyboardOptimizer(new List<Tuple<string, string>>() { new Tuple<string, string>(Resources.Resources.petCommandInlineExtraInfo, "petCommandInlineExtraInfo") }));
            }

            if (textRecieved == "/language")
            {
                User user = _userService.UpdateLanguage(message.From.Id, null);
                return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(Resources.Resources.ChangeLanguage,
                                                                            Constants.ChangeLanguageSticker,
                                                                            Constants.LanguagesMarkup,
                                                                            null);
            }

            if (textRecieved == "/bathroom")
            {
                return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(Resources.Resources.DevelopWarning,
                                                            Constants.DevelopWarningSticker,
                                                            null,
                                                            null);
            }

            if (textRecieved == "/kitchen")
            {
                Pet pet = _petService.Get(message.From.Id);
                string toSendText = string.Format(Resources.Resources.kitchenCommand, pet.Starving);

                List<Tuple<string, string>> inlineParts = Constants.inlineParts;
                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

                return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(toSendText,
                                                            Constants.PetKitchen_Cat,
                                                            null,
                                                            toSendInline);
            }

            if (textRecieved == "/gameroom")
            {
                return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(Resources.Resources.DevelopWarning,
                                                            Constants.DevelopWarningSticker,
                                                            null,
                                                            null);
            }

            if (textRecieved == "/ranks")
            {
                return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(Resources.Resources.DevelopWarning,
                                                            Constants.DevelopWarningSticker,
                                                            null,
                                                            null);
            }

            if (textRecieved == "/sleep")
            {
                return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(Resources.Resources.DevelopWarning,
                                                            Constants.DevelopWarningSticker,
                                                            null,
                                                            null);
            }

            if (textRecieved == "/test")
            {
                return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(Resources.Resources.DevelopWarning,
                                                            Constants.DevelopWarningSticker,
                                                            null,
                                                            null);
            }
            if (textRecieved == "/restart")
            {
                return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(Resources.Resources.DevelopWarning,
                                                            Constants.DevelopWarningSticker,
                                                            null,
                                                            null);
            }



            return ExtrasHandler();
        }

        public Tuple<string, InlineKeyboardMarkup> CallbackHandler()
        {
            if (callback.Data == null)
                return null;

            UpdateIndicators();

            if (callback.Data == "petCommandInlineBasicInfo")
            {
                Pet pet = _petService.Get(callback.From.Id);
                string toSendText = string.Format(Resources.Resources.petCommand, pet.Name, pet.HP, pet.EXP, pet.Level, pet.Starving, Extensions.GetFatigue(pet.Fatigue));
                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new List<Tuple<string, string>>() { new Tuple<string, string>(Resources.Resources.petCommandInlineExtraInfo, "petCommandInlineExtraInfo") });

                return new Tuple<string, InlineKeyboardMarkup>(toSendText, toSendInline);
            }

            if (callback.Data == "petCommandInlineExtraInfo")
            {
                Pet pet = _petService.Get(callback.From.Id);
                string toSendText = string.Format(Resources.Resources.petCommandMoreInfo1, pet.Name, pet.BirthDateTime);
                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new List<Tuple<string, string>>() { new Tuple<string, string>(Resources.Resources.petCommandInlineBasicInfo, "petCommandInlineBasicInfo") });

                return new Tuple<string, InlineKeyboardMarkup>(toSendText, toSendInline);
            }

            if (callback.Data == "kitchenCommandInlineBread")
            {
                Pet pet = _petService.Get(callback.From.Id);

                var newStarving = pet.Starving - Constants.BreadHungerFactor;
                if (newStarving < 0)
                    newStarving = 0;

                _petService.UpdateStarving(callback.From.Id, newStarving);

                string anwser = string.Format(Resources.Resources.PetFeedingAnwserCallback, (int)Constants.BreadHungerFactor);
                bot.AnswerCallbackQueryAsync(callback.Id, anwser);

                string toSendText = string.Format(Resources.Resources.kitchenCommand, newStarving);

                string s1 = callback.Message.Text.ToLower().Trim();
                s1 = s1.Replace("\n", string.Empty);
                string s2 = toSendText.ToLower().Trim();
                s2 = s2.Replace(Environment.NewLine, string.Empty);

                if (string.Equals(s1, s2, StringComparison.InvariantCultureIgnoreCase))
                    return null;

                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(Constants.inlineParts, 3);

                return new Tuple<string, InlineKeyboardMarkup>(toSendText, toSendInline);
            }

            if (callback.Data == "kitchenCommandInlineRedApple")
            {
                Pet pet = _petService.Get(callback.From.Id);

                var newStarving = pet.Starving - Constants.RedAppleHungerFactor;
                if (newStarving < 0)
                    newStarving = 0;

                _petService.UpdateStarving(callback.From.Id, newStarving);

                string anwser = string.Format(Resources.Resources.PetFeedingAnwserCallback, (int)Constants.RedAppleHungerFactor);
                bot.AnswerCallbackQueryAsync(callback.Id, anwser);

                string toSendText = string.Format(Resources.Resources.kitchenCommand, newStarving);

                string s1 = callback.Message.Text.ToLower().Trim();
                s1 = s1.Replace("\n", string.Empty);
                string s2 = toSendText.ToLower().Trim();
                s2 = s2.Replace(Environment.NewLine, string.Empty);

                if (string.Equals(s1, s2, StringComparison.InvariantCultureIgnoreCase))
                    return null;

                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(Constants.inlineParts, 3);

                return new Tuple<string, InlineKeyboardMarkup>(toSendText, toSendInline);
            }

            if (callback.Data == "kitchenCommandInlineChocolate")
            {
                Pet pet = _petService.Get(callback.From.Id);

                var newStarving = pet.Starving - Constants.ChocolateHungerFactor;
                if (newStarving < 0)
                    newStarving = 0;

                _petService.UpdateStarving(callback.From.Id, newStarving);

                string anwser = string.Format(Resources.Resources.PetFeedingAnwserCallback, (int)Constants.ChocolateHungerFactor);
                bot.AnswerCallbackQueryAsync(callback.Id, anwser);

                string toSendText = string.Format(Resources.Resources.kitchenCommand, newStarving);

                string s1 = callback.Message.Text.ToLower().Trim();
                s1 = s1.Replace("\n", string.Empty);
                string s2 = toSendText.ToLower().Trim();
                s2 = s2.Replace(Environment.NewLine, string.Empty);

                if (string.Equals(s1, s2, StringComparison.InvariantCultureIgnoreCase))
                    return null;

                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(Constants.inlineParts, 3);

                return new Tuple<string, InlineKeyboardMarkup>(toSendText, toSendInline);
            }

            if (callback.Data == "kitchenCommandInlineLollipop")
            {
                Pet pet = _petService.Get(callback.From.Id);

                var newStarving = pet.Starving - Constants.LollipopHungerFactor;
                if (newStarving < 0)
                    newStarving = 0;

                _petService.UpdateStarving(callback.From.Id, newStarving);

                string anwser = string.Format(Resources.Resources.PetFeedingAnwserCallback, (int)Constants.LollipopHungerFactor);
                bot.AnswerCallbackQueryAsync(callback.Id, anwser);

                string toSendText = string.Format(Resources.Resources.kitchenCommand, newStarving);

                string s1 = callback.Message.Text.ToLower().Trim();
                s1 = s1.Replace("\n", string.Empty);
                string s2 = toSendText.ToLower().Trim();
                s2 = s2.Replace(Environment.NewLine, string.Empty);

                if (string.Equals(s1, s2, StringComparison.InvariantCultureIgnoreCase))
                    return null;

                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(Constants.inlineParts, 3);

                return new Tuple<string, InlineKeyboardMarkup>(toSendText, toSendInline);
            }

            return null;
        }
    }
}
