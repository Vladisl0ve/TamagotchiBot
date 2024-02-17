using Serilog;
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
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace TamagotchiBot.Handlers
{
    public class UpdateHandler : IUpdateHandler
    {
        private readonly IApplicationServices _appServices;
        private readonly IEnvsSettings _envs;
        private readonly ConcurrentDictionary<(long userId, long chatId), DateTime> lastMsgList = new ConcurrentDictionary<(long, long), DateTime>();

        public UpdateHandler(IApplicationServices services, IEnvsSettings envs)
        {
            _appServices = services;
            _envs = envs;
#if !DEBUG && !DEBUG_NOTIFY
            EmergencyUpdatePets();
#endif
        }

        public Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            new ForwardController(_appServices, update).StartForwarding();

            if (!ToContinueHandlingUpdateChecking(update))
                return Task.CompletedTask;

            var messageFromUser = update.Message;
            var callbackFromUser = update.CallbackQuery;
            var userId = messageFromUser?.From.Id ?? callbackFromUser?.From.Id ?? default;
            var chatId = messageFromUser?.Chat.Id ?? callbackFromUser?.Message.Chat.Id ?? default;
            var msgAudience = chatId > 0 ? MessageAudience.Private : MessageAudience.Group;

            HandleUpdate(update, msgAudience);

            new SetCommandController(_appServices, _envs, userId, chatId).UpdateCommands(msgAudience, _appServices.UserService.Get(userId)?.Culture ?? "ru");

            _appServices.SInfoService.UpdateLastGlobalUpdate();
            return Task.CompletedTask;

            async void HandleUpdate(Update update, MessageAudience messageAudience)
            {
                switch (messageAudience)
                {
                    case MessageAudience.Private:
                        {
                            switch (update.Type)
                            {
                                case UpdateType.Message:
                                    await OnMessagePrivate(update.Message);
                                    return;
                                case UpdateType.CallbackQuery:
                                    await OnCallbackPrivate(update.CallbackQuery);
                                    return;
                                default:
                                    return;
                            }
                        }
                    case MessageAudience.Group:
                        {
                            switch (update.Type)
                            {
                                case UpdateType.Message:
                                    await OnMessageGroup(update.Message, (await botClient.GetMeAsync(cancellationToken: cancellationToken)).Id);
                                    return;
                                case UpdateType.CallbackQuery:
                                    await OnCallbackGroup(update.CallbackQuery);
                                    return;
                                default:
                                    return;
                            }
                        }
                    default:
                        return;
                }
            }
        }

        private async Task<bool> IsUserRegisteredFlyerCheck(long userId)
        {
            const int TIMEOUT_SECONDS = 2;
#if DEBUG || DEBUG_NOTIFY
            return true; //true on DEBUG
#endif
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(TIMEOUT_SECONDS);

                var sendPostDto = new { key = "FL-eWYKid-AEeWAG-ElIsea-FunlRj", user_id = userId };
                var json = JsonConvert.SerializeObject(sendPostDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://api.flyerservice.io/check", content);

                var result = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error("==> ERROR ON FLYER RESPONSE:" + result);
                    return true;
                }

                if (result.Contains("\"skip\":false"))
                {
                    Log.Information($"Flyer ==> tasks not done, userId: {userId}");
                    return false;
                }
                else if (result.Contains("\"skip\":true"))
                {
                    Log.Information($"Flyer ==> tasks done, userId: {userId}, result: {result}");
                    return true;
                }

                Log.Fatal($"Flyer ==> wrong result: {result}");
                return true; //true on error
            }
            catch (TaskCanceledException)
            {
                Log.Error($"TIMEOUT FLYER - {TIMEOUT_SECONDS}s");
                return true; //true on error
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error FLYER HTTP: ");
                return true; //true on error
            }
        }

        private async Task OnMessagePrivate(Message message)
        {
            var userId = message.From.Id;
            if (IsAdminMessage(userId))
            {
                var adminController = new AdminController(_appServices, _envs, message);
                if (await adminController.ProcessMessage())
                    return;
            }

            if (IsCooldown(userId, userId, message.Date))
            {
                Log.Debug($"Cooldown for userId: {userId}");
                return;
            }

            if (!IsUserAndPetRegisteredChecking(userId))
            {
                await RegisterUserAndPet(message); //CreatorController
                return;
            }

            if (!DidUserChoseLanguage(userId))
            {
                await new CreatorController(_appServices, message).ApplyNewLanguage(true);
                return;
            }

            if (DidUserConfirmNewPetName(userId) ?? false)
            {
                await new CreatorController(_appServices, message).ToRenamingAnswer();
                return;
            }

            if (DidUserChoseNewPetName(userId) ?? false)
            {
                await new CreatorController(_appServices, message).AskToConfirmNewName();
                return;
            }

            new SynchroDBController(_appServices, message.From, userId, message.Chat.Title).SynchronizeWithDB(); //update user (username, names etc.) in DB
            CreatorController creatorController = new CreatorController(_appServices, message);
            creatorController.UpdateIndicators(); //update all pet's statistics

            if (creatorController.CheckIsPetGone())
            {
                await creatorController.ToResurrectAnswer();
                return;
            }

            if (creatorController.CheckIsPetZeroHP())
            {
                await creatorController.AfterDeath();
                return;
            }

            if (!await IsUserRegisteredFlyerCheck(userId))//FLYER
            {
                await _appServices.BotControlService.SendStickerAsync(userId, StickersId.FlyerADSSticker);
                return;
            }

            if (_appServices.PetService.Get(message.From.Id)?.Name != null)
                SendGramadsPostToChat(message.From.Id);

            if (_appServices.UserService.Get(message.From.Id)?.IsInAppleGame ?? false)
                await new AppleGameController(_appServices, message).Menu();
            else
                await new MenuController(_appServices, _envs, message).ProcessMessage();
        }
        private async Task OnCallbackPrivate(CallbackQuery callbackQuery)
        {
            var userId = callbackQuery.From.Id;
            var petDB = _appServices.PetService.Get(userId);
            var userDB = _appServices.UserService.Get(userId);
            if (userDB == null || petDB == null)
                return;

            if (callbackQuery.Data == null)
                return;

            if (userDB.IsInAppleGame)
                return;

            if (callbackQuery.Data == CallbackButtons.GameroomCommand.GameroomCommandInlineAppleGame.CallbackData)
            {
                await new AppleGameController(_appServices, callbackQuery).PreStart();
                return;
            }

            if (!await IsUserRegisteredFlyerCheck(userId))//FLYER
            {
                await _appServices.BotControlService.SendStickerAsync(userId, StickersId.FlyerADSSticker);
                return;
            }

            // call this method wherever you want to show an ad,
            // for example your bot just made its job and
            // it's a great time to show an ad to a user

            SendGramadsPostToChat(callbackQuery.From.Id);

            new SynchroDBController(_appServices, callbackQuery.From, userId, callbackQuery.Message.Chat.Title).SynchronizeWithDB(); //update user (username, names etc.) in DB
            CreatorController creatorController = new CreatorController(_appServices, callback: callbackQuery);
            creatorController.UpdateIndicators(); //update all pet's statistics

            var controller = new MenuController(_appServices, _envs, callbackQuery);
            await controller.CallbackHandler();
        }
        private async Task OnCallbackGroup(CallbackQuery callbackQuery)
        {
            if (_appServices.UserService.Get(callbackQuery.From.Id) == null
                || _appServices.PetService.Get(callbackQuery.From.Id) == null)
            {
                await _appServices.BotControlService.AnswerCallbackQueryAsync(callbackQuery.Id, callbackQuery.From.Id, nameof(Resources.Resources.MPNoPetCallbackAlert).UseCulture(callbackQuery.From.LanguageCode), true);
                return;
            }

            if (callbackQuery.Data == null)
                return;

            var synchroController = new SynchroDBController(_appServices, callbackQuery.From, callbackQuery.Message.Chat.Id, callbackQuery.Message.Chat.Title);
            synchroController.SynchronizeWithDB(); //update user (username, names etc.) in DB
            synchroController.SynchronizeMPWithDB(); //update chatMP (name) in DB for MP
            MultiplayerController multiplayerController = new MultiplayerController(_appServices, callback: callbackQuery);

            await multiplayerController.CallbackHandler();
        }
        private Task OnMessageGroup(Message message, long botId)
        {
            var userId = message.From.Id;
            return message.Type switch
            {
                MessageType.ChatMembersAdded => OnChatMemberAdded(botId),
                MessageType.ChatMemberLeft => OnChatMemberLeft(botId),
                _ => OnTextMessageGroup(message)
            };

            async Task OnChatMemberAdded(long botId)
            {
                if (message.NewChatMembers.Any(u => u.Id == botId))
                {
                    _appServices.ChatsMPService.Create(new Models.Mongo.ChatsMP()
                    {
                        ChatId = message.Chat.Id,
                        Name = message.Chat.Title
                    });

                    MultiplayerController multiplayerController = new MultiplayerController(_appServices, message);
                    await multiplayerController.SendWelcomeMessageOnStart();

                    await new SetCommandController(_appServices, _envs, userId, message.Chat.Id).UpdateCommandsForThisChat("ru");
                    Log.Information($"Bot has been added to new chat #{message.Chat.Title}, ID:{message.Chat.Id} #");
                }
            }
            Task OnChatMemberLeft(long botId)
            {
                if (message.LeftChatMember.Id == botId)
                {
                    _appServices.ChatsMPService.Remove(message.Chat.Id);

                    Log.Information($"Bot has been deleted from chat #{message.Chat.Title}, ID:{message.Chat.Id} #");
                }

                return Task.CompletedTask;
            }
            async Task OnTextMessageGroup(Message message)
            {
                var botUsername = (await _appServices.SInfoService.GetBotUserInfo()).Username.ToLower();
                if (!message.Text.Contains($"@{botUsername}") && message.EntityValues == null)
                    return;

                MultiplayerController multiplayerController = new MultiplayerController(_appServices, message);

                new CreatorController(_appServices, message).UpdateIndicators();
                var synchroController =  new SynchroDBController(_appServices, message.From, message.Chat.Id, message.Chat.Title);
                synchroController.SynchronizeWithDB(); //update user (username, names etc.) in DB
                synchroController.SynchronizeMPWithDB(); //update chatMP (name) in DB for MP

                await multiplayerController.CommandHandler(botUsername);
            }
        }


        /// <returns>true == handled update is acceptable to continue, otherwise must to stop</returns>
        private bool ToContinueHandlingUpdateChecking(Update update)
        {
            if ((update.Type == UpdateType.Message)
                && (update.Message.Type == MessageType.Text || update.Message.Type == MessageType.ChatMembersAdded || update.Message.Type == MessageType.ChatMemberLeft)
                && (update.Message.From != null)
                && (update.Message.Chat?.Id != null && !_envs.ChatsToDevNotify.Any(c => update.Message.Chat.Id.ToString() == c))
                && (update.Message.ForwardDate == null))
                return true;

            if ((update.Type == UpdateType.CallbackQuery)
                && (update.CallbackQuery.Message != null)
                && (update.CallbackQuery.Message.Chat?.Id != null && !_envs.ChatsToDevNotify.Any(c => update.CallbackQuery.Message.Chat.Id.ToString() == c))
                && (update.CallbackQuery.Message.ForwardDate == null))
                return true;

            return false;
        }

        private bool IsCooldown(long userId, long chatId, DateTime sentMsgTime)
        {
            DateTime timeNow = DateTime.UtcNow;
            if (!lastMsgList.ContainsKey((userId, chatId)))
            {
                lastMsgList.TryAdd((userId, chatId), timeNow);
                return false;
            }

            if ((sentMsgTime + TimesToWait.OldMessageDelta) < timeNow)
                return true;

            if (timeNow < (lastMsgList[(userId, chatId)] + TimesToWait.CooldownOnMessage))
                return true;

            lastMsgList[(userId, chatId)] = timeNow;
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
        private bool IsAdminMessage(long userId)
        {
            if (_envs?.AlwaysNotifyUsers == null)
                return false;

            var chatsToNotify = new List<string>(_envs.AlwaysNotifyUsers);
            return chatsToNotify.Exists(c => c == userId.ToString());
        }
        private void EmergencyUpdatePets()
        {
            DateTime lastUpdateTime = _appServices.SInfoService.GetLastAppChangeTime();
            if (lastUpdateTime == DateTime.MinValue)
            {
                Log.Debug("No EmergencyUpdate: lastUpdateTime is null");
                return;
            }

            var deltaTime = DateTime.UtcNow - lastUpdateTime;
            if (deltaTime < new TimeSpan(0, 5, 0)) // delay 5 minutes
            {
                Log.Debug($"No EmergencyUpdate: delta time is {deltaTime}");
                return;
            }

            var petsToUpdate = _appServices.PetService.GetAll();
            Log.Fatal($"EmergencyUpdate STARTED!    delta time is {deltaTime}");
            foreach (var pet in petsToUpdate)
            {
                if (pet == null)
                    continue;

                pet.LastUpdateTime += deltaTime;
                _appServices.PetService.Update(pet.UserId, pet);
            }
            Log.Fatal($"EmergencyUpdate: delta time is {deltaTime}, updated {petsToUpdate.Count} pets");
        }

        private async Task RegisterUserAndPet(Message message)
        {
            CreatorController creatorController;
            var userId = message.From.Id;
            var userDB = _appServices.UserService.Get(userId);
            if (userDB == null)
            {
                creatorController = new CreatorController(_appServices, message);
                creatorController.CreateUser();
                await creatorController.AskALanguage();
            }
            else if (userDB.IsLanguageAskedOnCreate)
            {
                creatorController = new CreatorController(_appServices, message);
                if (!await creatorController.ApplyNewLanguage())
                    return;
                await creatorController.SendWelcomeText();
                await Task.Delay(1000);
                await creatorController.AskForAPetName();
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
        private async void SendGramadsPostToChat(long chatId)
        {
            const int TIMEOUT_SECONDS = 5;
#if DEBUG
            return;
#endif
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxNTIiLCJqdGkiOiJkOTYzYTZiYy1mNTc3LTQyZjYtYTkyOS02NzRhZTAwYjRlOWEiLCJuYW1lIjoi8J-QviDQotCw0LzQsNCz0L7Rh9C4IHwgVmlydHVhbCBQZXQg8J-QviIsImJvdGlkIjoiMjQxIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZWlkZW50aWZpZXIiOiIxNTIiLCJuYmYiOjE2OTAzMTE5MDAsImV4cCI6MTY5MDUyMDcwMCwiaXNzIjoiU3R1Z25vdiIsImF1ZCI6IlVzZXJzIn0.hByX6S4UoV9J9G559wvvJUrid-_GZe4KLtbog7AV7HU");
                client.Timeout = TimeSpan.FromSeconds(TIMEOUT_SECONDS);

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
            catch (TaskCanceledException)
            {
                Log.Error($"TIMEOUT GRAMADS - {TIMEOUT_SECONDS}s");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error GRAMADS HTTP: ");
            }
        }

        public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                  => $"Telegram API Error: [{apiRequestException.ErrorCode}]{Environment.NewLine}{apiRequestException.Message}",
                RequestException TGRequestException
                  => $"TG RequestException Error: [{TGRequestException.HttpStatusCode}]{Environment.NewLine}{TGRequestException.Message}",
                _ => exception.ToString()
            };


            Log.Fatal(exception, $"UH => {ErrorMessage}");

            await Task.Delay(1000);

            if (exception is ApiRequestException apiEx && apiEx.ErrorCode == 409)
                Environment.Exit(-1);

            int msToWait = 5000;
            Log.Warning($"Waiting {msToWait / 1000}s before restart...");
            await Task.Delay(msToWait);
            Environment.Exit(-1);
        }
    }
}
