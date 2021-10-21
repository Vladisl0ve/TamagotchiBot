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

namespace TamagotchiBot.Controllers
{
    public class GameController
    {
        private readonly UserService _userService;
        private readonly PetService _petService;
        private readonly Message message;
        private readonly CallbackQuery callback;

        private CultureInfo localCulture;

        public GameController(UserService userService, PetService petService, CallbackQuery callback)
        {
            _userService = userService;
            _petService = petService;
            this.callback = callback;
        }

        public GameController(UserService userService, PetService petService, Message message)
        {
            _userService = userService;
            _petService = petService;
            this.message = message;
        }

        public Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup> CreateUser()
        {
            _userService.Create(message.From);
            Log.Information($"User {message.From.Username} has been added to Db");

            localCulture = new CultureInfo(message.From.LanguageCode);
            Resources.Resources.Culture = localCulture;

            return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(Resources.Resources.Welcome, Constants.WelcomeSticker, null, null);
        }

        public Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup> Process()
        {
            var userMessage = message.From;
            User userDb = _userService.Get(userMessage.Id);

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
            int toAddExp = minuteCounter * Constants.ExpFactor;

            pet.EXP += toAddExp;

            if (pet.EXP > 100)
            {
                pet.Level = 0;
                pet.Level += pet.EXP / Constants.ExpToLvl;
                pet.EXP -= (pet.EXP / Constants.ExpToLvl) * Constants.ExpToLvl;
            }
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
                string toSendText = string.Format(Resources.Resources.petCommand, pet.Name, pet.HP, pet.EXP, pet.Level);

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
                return new Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup>(Resources.Resources.DevelopWarning,
                                                            Constants.DevelopWarningSticker,
                                                            null,
                                                            null);
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



            return ExtrasHandler();
        }

        public Tuple<string, InlineKeyboardMarkup> CallbackHandler()
        {
            if (callback.Data == null)
                return null;

            if (callback.Data == "petCommandInlineBasicInfo")
            {
                Pet pet = _petService.Get(callback.From.Id);
                string toSendText = string.Format(Resources.Resources.petCommand, pet.Name, pet.HP, pet.EXP, pet.Level);
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

            return null;
        }
    }
}
