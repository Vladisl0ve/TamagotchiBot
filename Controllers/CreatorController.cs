using System.Globalization;
using TamagotchiBot.Services.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot;
using Serilog;
using System;
using TamagotchiBot.Models.Mongo;
using static TamagotchiBot.Resources.Resources;
using static TamagotchiBot.UserExtensions.Constants;
using TamagotchiBot.Models.Answers;
using Extensions = TamagotchiBot.UserExtensions.Extensions;
using Chat = TamagotchiBot.Models.Mongo.Chat;

namespace TamagotchiBot.Controllers
{
    public class CreatorController
    {
        private IApplicationServices _appServices;
        private readonly ITelegramBotClient bot;
        private readonly Message message = null;
        private readonly CallbackQuery callback = null;
        private readonly long _userId;

        public CreatorController(IApplicationServices services, ITelegramBotClient bot, Message message = null, CallbackQuery callback = null)
        {
            this.bot = bot;
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
    }
}
