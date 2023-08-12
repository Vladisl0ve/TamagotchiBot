using System.Globalization;
using TamagotchiBot.Services.Interfaces;
using Telegram.Bot.Types;
using Serilog;
using System;
using TamagotchiBot.Models.Mongo;
using static TamagotchiBot.Resources.Resources;
using static TamagotchiBot.UserExtensions.Constants;
using TamagotchiBot.Models.Answers;
using Extensions = TamagotchiBot.UserExtensions.Extensions;
using Chat = TamagotchiBot.Models.Mongo.Chat;
using TamagotchiBot.UserExtensions;

namespace TamagotchiBot.Controllers
{
    public class CreatorController
    {
        private IApplicationServices _appServices;
        private readonly Message message = null;
        private readonly CallbackQuery callback = null;
        private readonly long _userId;

        public CreatorController(IApplicationServices services, Message message = null, CallbackQuery callback = null)
        {
            this.callback = callback;
            this.message = message;
            _userId = callback?.From.Id ?? message.From.Id;

            _appServices = services;

            Culture = new CultureInfo(_appServices.UserService.Get(_userId)?.Culture ?? "ru");
            UpdateOrCreateAUD(message?.From ?? callback.From, message, callback);
        }
        public void CreateUserAndChat()
        {
            if (message != null)
                CreateUserFromMessage(message);
            else if (callback != null)
                CreateUserFromCallback(callback);
        }
        public Answer AskALanguage()
        {
            _appServices.UserService.UpdateIsLanguageAskedOnCreate(_userId, true);

            return new Answer(ChangeLanguage,
                StickersId.ChangeLanguageSticker,
                LanguagesMarkup,
                null);
        }

        private void CreateUserFromMessage(Message msg)
        {
            if (msg == null)
                return;

            var ads = Extensions.GetAdsProducerFromStart(msg.Text);
            if (ads != null)
                _appServices.AdsProducersService.AddOrInsert(ads);

            _appServices.UserService.Create(msg.From);
            _appServices.ChatService.Create(new Chat()
            {
                ChatId = msg.Chat.Id,
                Name = msg.Chat.Username ?? msg.Chat.Title ?? "ID: " + msg.Chat.Id.ToString(),
                UserId = _userId,
                LastMessage = null
            });

            Log.Information($"User {msg.From.Username ?? msg.From.Id.ToString()}, ChatID: {msg.Chat.Id} has been added to Db");

            Culture = new CultureInfo(msg.From.LanguageCode ?? "ru");
        }
        private void CreateUserFromCallback(CallbackQuery callback)
        {
            var msg = callback.Message;

            if (msg == null)
                return;

            var ads = Extensions.GetAdsProducerFromStart(msg.Text);
            if (ads != null)
                _appServices.AdsProducersService.AddOrInsert(ads);

            _appServices.UserService.Create(callback.From);
            _appServices.ChatService.Create(new Chat()
            {
                ChatId = msg.Chat.Id,
                Name = msg.Chat.Username ?? msg.Chat.Title ?? "ID: " + msg.Chat.Id.ToString(),
                UserId = _userId,
                LastMessage = null
            });

            Log.Information($"User {msg.From.Username ?? msg.From.Id.ToString()}, ChatID: {msg.Chat.Id} has been added to Db");

            Culture = new CultureInfo(msg.From.LanguageCode ?? "ru");
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

        internal bool CreatePet()
        {
            var msgText = message?.Text;
            if (string.IsNullOrEmpty(msgText))
                return false;

            _appServices.PetService.Create(new Pet()
            {
                UserId = _userId,
                Name = msgText,
                Level = 1,
                BirthDateTime = DateTime.UtcNow,
                LastUpdateTime = DateTime.UtcNow,
                NextRandomEventNotificationTime = DateTime.UtcNow.AddMinutes(25),
                EXP = 0,
                HP = 100,
                Joy = 70,
                Fatigue = 0,
                Satiety = 80,
                IsWelcomed = true,
                Type = null,
                Gold = 50
            });
            Log.Information($"Pet of UserID: {_userId} has been added to Db");

            _appServices.UserService.UpdateIsPetNameAskedOnCreate(_userId, false);

            var toSend = new Answer()
            {
                Text = ConfirmedName,
                StickerId = StickersId.PetConfirmedName_Cat,
                ReplyMarkup = null,
                InlineKeyboardMarkup = null
            };

            _appServices.BotControlService.SendAnswerAsync(toSend, _userId);
            return true;
        }
        internal bool ApplyNewLanguage()
        {
            var msgText = message?.Text;
            if (string.IsNullOrEmpty(msgText))
                return false;

            var newLanguage = msgText.GetCulture();
            if (string.IsNullOrEmpty(newLanguage))
                return false;

            _appServices.UserService.UpdateLanguage(_userId, newLanguage);
            _appServices.UserService.UpdateIsLanguageAskedOnCreate(_userId, false);
            return true;
        }
        internal void SendWelcomeText()
        {
            var toSend =  new Answer()
            {
                Text = Welcome,
                StickerId = StickersId.WelcomeSticker
            };
            _appServices.BotControlService.SendAnswerAsync(toSend, _userId);
        }
        internal void AskForAPetName()
        {
            var toSend =  new Answer()
            {
                Text = ChooseName,
                StickerId = StickersId.PetChooseName_Cat
            };
            _appServices.BotControlService.SendAnswerAsync(toSend, _userId);

            _appServices.UserService.UpdateIsPetNameAskedOnCreate(_userId, true);
        }
    }
}
