﻿using Serilog;
using System;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TamagotchiBot.Controllers;
using TamagotchiBot.UserExtensions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Newtonsoft.Json;
using TamagotchiBot.Services.Interfaces;
using static TamagotchiBot.UserExtensions.Constants;
using System.Linq;
using TamagotchiBot.Database;

namespace TamagotchiBot.Handlers
{
    public class UpdateHandler : IUpdateHandler
    {
        private readonly IApplicationServices _appServices;
        private readonly IEnvsSettings _envs;

        public UpdateHandler(IApplicationServices services, IEnvsSettings envs)
        {
            _appServices = services;
            _envs = envs;
        }

        public Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken token)
        {
            if (!ToContinueHandlingUpdateChecking(update))
                return Task.CompletedTask;

            var messageFromUser = update.Message;
            var callbackFromUser = update.CallbackQuery;
            var userId = messageFromUser?.From.Id ?? callbackFromUser?.From.Id ?? default;
            var chatId = messageFromUser?.Chat.Id ?? callbackFromUser?.Message.Chat.Id ?? default;
            var msgAudience = chatId > 0 ? MessageAudience.Private : MessageAudience.Group;

            HandleUpdate(update, msgAudience);

            new SetCommandController(_appServices, messageFromUser, callbackFromUser).UpdateCommands(msgAudience);

            _appServices.SInfoService.UpdateLastGlobalUpdate();
            return Task.CompletedTask;

            void HandleUpdate(Update update, MessageAudience messageAudience)
            {
                switch (messageAudience)
                {
                    case MessageAudience.Private:
                        {
                            switch (update.Type)
                            {
                                case UpdateType.Message:
                                    OnMessagePrivate(update.Message);
                                    return;
                                case UpdateType.CallbackQuery:
                                    OnCallbackPrivate(update.CallbackQuery);
                                    return;
                                default:
                                    return;
                            };
                        }
                    case MessageAudience.Group:
                        {
                            switch (update.Type)
                            {
                                case UpdateType.Message:
                                    OnMessageGroup(update.Message);
                                    return;
                                case UpdateType.CallbackQuery:
                                    OnCallbackGroup(update.CallbackQuery);
                                    return;
                                default:
                                    return;
                            };
                        }
                    default:
                        return;
                }
            }
            Task OnMessageGroup(Message message)
            {
                return message.Type switch
                {
                    MessageType.ChatMembersAdded => OnChatMemberAdded(),
                    MessageType.ChatMemberLeft => OnChatMemberLeft(),
                    _ => OnTextMessageGroup(message, userId)
                };

                Task OnChatMemberAdded()
                {
                    if (message.NewChatMembers.Any(u => u.Id == bot.BotId))
                    {
                        _appServices.ChatsMPService.Create(new Models.Mongo.ChatsMP()
                        {
                            ChatId = message.Chat.Id,
                            Name = message.Chat.Title
                        });

                        MultiplayerController multiplayerController = new MultiplayerController(_appServices, message);
                        multiplayerController.SendWelcomeMessageOnStart();

                        new SetCommandController(_appServices, messageFromUser, callbackFromUser).UpdateCommandsForThisChat();
                        Log.Information($"Bot has been added to new chat #{message.Chat.Title}, ID:{message.Chat.Id} #");
                    }

                    return Task.CompletedTask;
                }
                Task OnChatMemberLeft()
                {
                    if (message.LeftChatMember.Id == bot.BotId)
                    {
                        _appServices.ChatsMPService.Remove(message.Chat.Id);

                        Log.Information($"Bot has been deleted from chat #{message.Chat.Title}, ID:{message.Chat.Id} #");
                    }

                    return Task.CompletedTask;
                }
                async Task OnTextMessageGroup(Message message, long userId)
                {
                    var botUsername = (await _appServices.SInfoService.GetBotUserInfo()).Username.ToLower();
                    if (!message.Text.Contains($"@{botUsername}") && message.EntityValues == null)
                        return;

                    MultiplayerController multiplayerController = new MultiplayerController(_appServices, message);

                    new SynchroDBController(_appServices, message).SynchronizeWithDB(); //update user (username, names etc.) in DB
                    new SynchroDBController(_appServices, message).SynchronizeMPWithDB(); //update chatMP (name) in DB for MP

                    multiplayerController.CommandHandler(botUsername);

                    return;
                }
            }
            void OnCallbackGroup(CallbackQuery callbackQuery)
            {
                if (_appServices.UserService.Get(callbackQuery.From.Id) == null
                    || _appServices.PetService.Get(callbackQuery.From.Id) == null)
                {
                    _appServices.BotControlService.AnswerCallbackQueryAsync(callbackQuery?.Id, callbackQuery.From.Id, Resources.Resources.MPNoPetCallbackAlert, true);
                    return;
                }

                if (callbackQuery.Data == null)
                    return;

                new SynchroDBController(_appServices, callback: callbackQuery).SynchronizeWithDB(); //update user (username, names etc.) in DB
                new SynchroDBController(_appServices, callback: callbackQuery).SynchronizeMPWithDB(); //update chatMP (name) in DB for MP
                MultiplayerController multiplayerController = new MultiplayerController(_appServices, callback: callbackQuery);

                multiplayerController.CallbackHandler();
                return;
            }

            async void OnMessagePrivate(Message message)
            {
                if (!IsUserAndPetRegisteredChecking(userId))
                {
                    RegisterUserAndPet(message); //CreatorController
                    return;
                }

                if (!DidUserChoseLanguage(userId))
                {
                    await new CreatorController(_appServices, message).ApplyNewLanguage(true);
                    return;
                }

                if (DidUserConfirmNewPetName(userId) ?? false)
                {
                    new CreatorController(_appServices, message).ToRenamingAnswer();
                    return;
                }

                if (DidUserChoseNewPetName(userId) ?? false)
                {
                    await new CreatorController(_appServices, message).AskToConfirmNewName();
                    return;
                }

                new SynchroDBController(_appServices, message).SynchronizeWithDB(); //update user (username, names etc.) in DB
                CreatorController creatorController = new CreatorController(_appServices, message);
                creatorController.UpdateIndicators(); //update all pet's statistics

                if (creatorController.CheckIsPetGone())
                {
                    creatorController.ToResurrectAnswer();
                    return;
                }

                if (creatorController.CheckIsPetZeroHP())
                {
                    creatorController.AfterDeath();
                    return;
                }

                // call this method wherever you want to show an ad,
                // for example your bot just made its job and
                // it's a great time to show an ad to a user
                if (_appServices.PetService.Get(message.From.Id)?.Name != null)
                    await SendPostToChat(message.From.Id);

                if (_appServices.UserService.Get(message.From.Id)?.IsInAppleGame ?? false)
                    await new AppleGameController(_appServices, message).Menu();
                else
                    await new MenuController(_appServices, message).ProcessMessage();
            }
            async void OnCallbackPrivate(CallbackQuery callbackQuery)
            {
                var petDB = _appServices.PetService.Get(userId);
                var userDB = _appServices.UserService.Get(userId);
                if (userDB == null || petDB == null)
                    return;

                if (callbackQuery.Data == null)
                    return;

                if (userDB.IsInAppleGame)
                    return;

                if (callbackQuery.Data == new CallbackButtons.GameroomCommand().GameroomCommandInlineAppleGame.CallbackData)
                {
                    await new AppleGameController(_appServices, callbackQuery).PreStart();
                    return;
                }

                // call this method wherever you want to show an ad,
                // for example your bot just made its job and
                // it's a great time to show an ad to a user

                await SendPostToChat(callbackQuery.From.Id);

                new SynchroDBController(_appServices, callback: callbackQuery).SynchronizeWithDB(); //update user (username, names etc.) in DB
                CreatorController creatorController = new CreatorController(_appServices, callback: callbackQuery);
                creatorController.UpdateIndicators(); //update all pet's statistics

                var controller = new MenuController(_appServices, callbackQuery);
                controller.CallbackHandler();
            }
        }

        /// <returns>true == handled update is acceptable to continue, otherwise must to stop</returns>
        private bool ToContinueHandlingUpdateChecking(Update update)
        {
            if (update.Type == UpdateType.Message)
                if (update.Message.Type == MessageType.Text || update.Message.Type == MessageType.ChatMembersAdded || update.Message.Type == MessageType.ChatMemberLeft)
                    if (update.Message.From != null)
                        if (update.Message.Chat?.Id != null && !_envs.ChatsToDevNotify.Any(c => update.Message.Chat.Id.ToString() == c))
                            if (update.Message.ForwardDate == null)
                                return true;

            if (update.Type == UpdateType.CallbackQuery)
                if (update.CallbackQuery.Message != null)
                    if (update.CallbackQuery.Message.Chat?.Id != null && !_envs.ChatsToDevNotify.Any(c => update.CallbackQuery.Message.Chat.Id.ToString() == c))
                        if (update.CallbackQuery.Message.ForwardDate == null)
                            return true;

            return false;
        }
        private bool DidUserChoseLanguage(long userId)
        {
            var userDB = _appServices.UserService.Get(userId);
            if (userDB == null)
                return false;

            if (userDB.Culture == null)
                return false;

            return true;
        }
        private bool? DidUserConfirmNewPetName(long userId)
        {
            var metauserDB = _appServices.MetaUserService.Get(userId);
            if (metauserDB == null)
                return null;

            return metauserDB.IsAskedToConfirmRenaming;
        }
        private bool? DidUserChoseNewPetName(long userId)
        {
            var metauserDB = _appServices.MetaUserService.Get(userId);
            if (metauserDB == null)
                return null;

            return metauserDB.IsPetNameAskedOnRename;
        }
        private bool IsUserAndPetRegisteredChecking(long userId)
        {
            var userDB = _appServices.UserService.Get(userId);
            var petDB = _appServices.PetService.Get(userId);
            if (userDB == null)
                return false;

            if (userDB.IsLanguageAskedOnCreate)
                return false;

            if (userDB.IsPetNameAskedOnCreate)
                return false;

            if (petDB == null)
                return false;

            return true;
        }

        private async void RegisterUserAndPet(Message message)
        {
            CreatorController creatorController;
            var userId = message.From.Id;
            var userDB = _appServices.UserService.Get(userId);
            if (userDB == null)
            {
                creatorController = new CreatorController(_appServices, message);
                creatorController.CreateUser();
                creatorController.AskALanguage();
            }
            else if (userDB.IsLanguageAskedOnCreate)
            {
                creatorController = new CreatorController(_appServices, message);
                if (!await creatorController.ApplyNewLanguage())
                    return;
                creatorController.SendWelcomeText();
                await Task.Delay(1000);
                creatorController.AskForAPetName();
            }
            else if (userDB.IsPetNameAskedOnCreate)
            {
                creatorController = new CreatorController(_appServices, message);
                if (!await creatorController.IsNicknameAcceptable())
                    return;

                if (!await creatorController.CreatePet())
                    return;
            }
        }
        private async Task SendPostToChat(long chatId)
        {
#if DEBUG
            return;
#endif

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxNTIiLCJqdGkiOiJkOTYzYTZiYy1mNTc3LTQyZjYtYTkyOS02NzRhZTAwYjRlOWEiLCJuYW1lIjoi8J-QviDQotCw0LzQsNCz0L7Rh9C4IHwgVmlydHVhbCBQZXQg8J-QviIsImJvdGlkIjoiMjQxIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZWlkZW50aWZpZXIiOiIxNTIiLCJuYmYiOjE2OTAzMTE5MDAsImV4cCI6MTY5MDUyMDcwMCwiaXNzIjoiU3R1Z25vdiIsImF1ZCI6IlVzZXJzIn0.hByX6S4UoV9J9G559wvvJUrid-_GZe4KLtbog7AV7HU");

            var sendPostDto = new { SendToChatId = chatId };
            var json = JsonConvert.SerializeObject(sendPostDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.gramads.net/ad/SendPost", content);

            // or you can use the extension method "PostAsJson", for example:
            // var response = await client.PostAsJsonAsync("https://api.gramads.net/ad/SendPost", sendPostDto);

            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Log.Warning("==> Gramads:" + result);
                return;
            }

            Log.Information("==> Gramads: " + result);
        }

        public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                  => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Log.Fatal(ErrorMessage);
            await Task.Delay(1000, cancellationToken);

            if (exception is ApiRequestException apiEx)
            {
                if (apiEx.ErrorCode == 409)
                    Environment.Exit(-1);
            }

            int msToWait = 5000;
            Log.Warning($"Waiting {msToWait / 1000}s before restart...");
            await Task.Delay(msToWait, cancellationToken);
            Environment.Exit(-1);
        }
    }
}
