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
using TamagotchiBot.Models;

namespace TamagotchiBot.Handlers
{
    public class UpdateHandler : IUpdateHandler
    {
        private readonly IApplicationServices _appServices;
        private readonly IEnvsSettings _envs;
        private readonly ConcurrentDictionary<(long userId, long chatId), DateTime> lastMsgList = new ConcurrentDictionary<(long, long), DateTime>();
        private static string _subgramKey = null;

        public UpdateHandler(IApplicationServices services, IEnvsSettings envs)
        {
            _appServices = services;
            _envs = envs;
#if !DEBUG && !DEBUG_NOTIFY && !STAGING && !STAGING_LOCAL
            EmergencyUpdatePets();
#endif
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var handler = update.Type switch
            {
                UpdateType.Message => BotOnMessageReceived(botClient, update.Message!),
                UpdateType.CallbackQuery => BotOnCallbackQueryReceived(botClient, update.CallbackQuery!),
                UpdateType.PreCheckoutQuery => OnPreCheckoutQuery(update.PreCheckoutQuery!),
                _ => UnknownUpdateHandlerAsync(botClient, update)
            };

            try
            {
                await handler;
            }
            catch (Exception exception)
            {
                await HandleErrorAsync(botClient, exception, HandleErrorSource.HandleUpdateError, cancellationToken);
            }
        }

        private async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
        {
            if (message.Type == MessageType.SuccessfulPayment)
            {
                await OnSuccessfulPayment(message);
                return;
            }

            if (message.Type != MessageType.Text && message.Type != MessageType.NewChatMembers && message.Type != MessageType.LeftChatMember)
                return;

            if (message.From == null)
                return;

            if (message.Chat?.Id != null && _envs.ChatsToDevNotify.Any(c => message.Chat.Id.ToString() == c))
                return;

            if (message.ForwardDate != null)
                return;

            var userId = message.From.Id;
            var chatId = message.Chat.Id;
            var msgAudience = chatId > 0 ? MessageAudience.Private : MessageAudience.Group;

            Log.Information($"Received message type: {message.Type} from userId: {userId} in chatId: {chatId}, msgAudience: {msgAudience}");

            new SetCommandController(_appServices, _envs, userId, chatId).UpdateCommands(msgAudience, _appServices.UserService.Get(userId)?.Culture ?? "ru");

            _appServices.SInfoService.UpdateLastGlobalUpdate();

            if (msgAudience == MessageAudience.Private)
                await OnMessagePrivate(message);
            else
                await OnMessageGroup(message, (await botClient.GetMe(cancellationToken: default)).Id);
        }

        private async Task BotOnCallbackQueryReceived(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            if (callbackQuery.Message == null)
                return;

            if (callbackQuery.Message.Chat?.Id != null && _envs.ChatsToDevNotify.Any(c => callbackQuery.Message.Chat.Id.ToString() == c))
                return;

            if (callbackQuery.Message.ForwardDate != null)
                return;

            var userId = callbackQuery.From.Id;
            var chatId = callbackQuery.Message.Chat.Id;
            var msgAudience = chatId > 0 ? MessageAudience.Private : MessageAudience.Group;

            Log.Information($"Received callback type: {callbackQuery.Message.Type} from userId: {userId} in chatId: {chatId}, msgAudience: {msgAudience}");

            new SetCommandController(_appServices, _envs, userId, chatId).UpdateCommands(msgAudience, _appServices.UserService.Get(userId)?.Culture ?? "ru");

            _appServices.SInfoService.UpdateLastGlobalUpdate();

            if (msgAudience == MessageAudience.Private)
                await OnCallbackPrivate(callbackQuery);
            else
                await OnCallbackGroup(callbackQuery);
        }

        private Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
        {
            Log.Information($"Unknown update type: {update.Type}");
            return Task.CompletedTask;
        }

        private async Task<bool> IsUserRegisteredSubgramCheck(long userId, long chatId, string name, string username, string languageCode)
        {
#if DEBUG || DEBUG_NOTIFY || STAGING
            return true; //true on DEBUG
#endif
            if (string.IsNullOrEmpty(_subgramKey))
                _subgramKey = _appServices.SInfoService.GetSubgramKey();

            if (string.IsNullOrEmpty(_subgramKey))
                return true;

            const int TIMEOUT_SECONDS = 3;

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(TIMEOUT_SECONDS);
                client.DefaultRequestHeaders.Add("Auth", _subgramKey);

                var sendPostDto = new
                {
                    user_id = userId,
                    chat_id = chatId,
                    first_name = name,
                    username = username,
                    language_code = languageCode
                };
                var json = JsonConvert.SerializeObject(sendPostDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://api.subgram.org/get-sponsors", content);

                var result = await response.Content.ReadAsStringAsync();

                Log.Information($"Subgram response: {result}");

                if (!response.IsSuccessStatusCode)
                {
                    Log.Error("==> ERROR ON SUBGRAM RESPONSE:" + result);
                    return true;
                }

                var subgramResponse = JsonConvert.DeserializeObject<SubgramResponse>(result);

                if (subgramResponse?.status == "warning")
                {
                    Log.Information($"Subgram ==> tasks not done, userId: {userId}");
                    return false;
                }

                return true;
            }
            catch (TaskCanceledException)
            {
                Log.Error($"TIMEOUT SUBGRAM - {TIMEOUT_SECONDS}s");
                return true; //true on error
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error SUBGRAM HTTP: ");
                return true; //true on error
            }
        }

        private async Task OnMessagePrivate(Message message)
        {
            if (message.Type == MessageType.SuccessfulPayment)
            {
                await OnSuccessfulPayment(message);
                return;
            }

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
                await new CreatorController(_appServices, message).ApplyNewLanguage();
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
            await creatorController.UpdateIndicators(); //update all pet's statistics

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

            #region buying checks

            if (IsUserAskedOnBuying7daysVIP(userId) ?? false)
            {
                await new CreatorController(_appServices, message).TryApplyVIP7days();
                return;
            }

            //if (DidUserConfirmBuyingWithTgStars(userId) ?? false)
            //{
            //    await new CreatorController(_appServices, message).ToRenamingAnswer();
            //    return;
            //}

            #endregion


            if (_appServices.UserService.Get(userId)?.Created < DateTime.UtcNow.AddDays(-1))
            {
                try
                {
                    if (!await IsUserRegisteredSubgramCheck(userId, message.Chat.Id, message.From.FirstName, message.From.Username, message.From.LanguageCode))
                    {
                        return;
                    }

                }
                catch
                {
                    Log.Error($"Error on Subgram check for userId: {userId}");
                }
            }

            if (_appServices.PetService.Get(message.From.Id)?.Name != null)
                SendGramadsPostToChat(message.From.Id);

            if (_appServices.UserService.Get(message.From.Id)?.IsInAppleGame ?? false)
            {
                await new AppleGameController(_appServices, message).Menu();
            }
            else if (_appServices.UserService.Get(message.From.Id)?.IsInTicTacToeGame ?? false)
            {
                await new TicTacToeGameController(_appServices, message).Menu();
            }
            else if (_appServices.UserService.Get(message.From.Id)?.IsInHangmanGame ?? false)
            {
                await new HangmanGameController(_appServices, message).Menu();
            }
            else
                await new MenuController(_appServices, _envs, message).ProcessMessage();

            async Task OnRewardMsg(Message message)
            {
                var appleDataToUpdate = _appServices.AppleGameDataService.Get(message.From.Id);
                if (appleDataToUpdate == null || appleDataToUpdate.IsGameOvered)
                {
                    await _appServices.UserService.UpdateAppleGameStatus(message.From.Id, false);
                    await new MenuController(_appServices, null, message).ProcessMessage("/reward");
                }
                else
                {
                    appleDataToUpdate.TotalDraws += 1;
                    appleDataToUpdate.IsGameOvered = true;
                    _appServices.AppleGameDataService.Update(appleDataToUpdate);
                    await _appServices.UserService.UpdateAppleGameStatus(message.From.Id, false);
                    await new MenuController(_appServices, null, message).ProcessMessage("/reward");
                }
            }
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

            if (userDB.IsInTicTacToeGame)
                return;

            if (userDB.IsInHangmanGame)
                return;

            if (callbackQuery.Data == CallbackButtons.GameroomCommand.GameroomCommandInlineAppleGame.CallbackData)
            {
                await new AppleGameController(_appServices, callbackQuery).PreStart();
                return;
            }

            if (callbackQuery.Data == CallbackButtons.GameroomCommand.GameroomCommandInlineTicTacToe.CallbackData)
            {
                await new TicTacToeGameController(_appServices, null, callbackQuery).PreStart();
                return;
            }

            if (callbackQuery.Data == CallbackButtons.GameroomCommand.GameroomCommandInlineHangman.CallbackData)
            {
                await new HangmanGameController(_appServices, null, callbackQuery).PreStart();
                return;
            }

            if (_appServices.UserService.Get(userId)?.Created < DateTime.UtcNow.AddDays(-1))
                if (!await IsUserRegisteredSubgramCheck(userId, callbackQuery.Message.Chat.Id, callbackQuery.From.FirstName, callbackQuery.From.Username, callbackQuery.From.LanguageCode))
                {
                    return;
                }

            // call this method wherever you want to show an ad,
            // for example your bot just made its job and
            // it's a great time to show an ad to a user

            SendGramadsPostToChat(callbackQuery.From.Id);

            new SynchroDBController(_appServices, callbackQuery.From, userId, callbackQuery.Message.Chat.Title).SynchronizeWithDB(); //update user (username, names etc.) in DB
            CreatorController creatorController = new CreatorController(_appServices, callback: callbackQuery);
            await creatorController.UpdateIndicators(); //update all pet's statistics

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
                MessageType.NewChatMembers => OnChatMemberAdded(botId),
                MessageType.LeftChatMember => OnChatMemberLeft(botId),
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
                var botUsername = (await _appServices.BotControlService.GetBotUserInfo()).Username.ToLower();
                if (!message.Text.Contains($"@{botUsername}") && message.EntityValues == null)
                    return;

                MultiplayerController multiplayerController = new MultiplayerController(_appServices, message);

                await new CreatorController(_appServices, message).UpdateIndicators();
                var synchroController = new SynchroDBController(_appServices, message.From, message.Chat.Id, message.Chat.Title);
                synchroController.SynchronizeWithDB(); //update user (username, names etc.) in DB
                synchroController.SynchronizeMPWithDB(); //update chatMP (name) in DB for MP

                await multiplayerController.CommandHandler(botUsername);
            }
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
        private bool? IsUserAskedOnBuying7daysVIP(long userId)
        {
            var metauserDB = _appServices.MetaUserService.Get(userId);
            if (metauserDB == null)
                return null;

            return metauserDB.IsConfirmAskedOnVIP7daysBuying;
        }
        private bool IsUserAndPetRegisteredChecking(long userId)
        {
            var userDB = _appServices.UserService.Get(userId);
            var petDB = _appServices.PetService.Get(userId);
            if (userDB == null)
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
            CreatorController creatorController = new CreatorController(_appServices, message);
            var userId = message.From.Id;
            var userDB = _appServices.UserService.Get(userId);
            if (userDB == null)
            {
                try
                {
                    userDB = creatorController.CreateUser();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error creating userId: {userId}");
                    return;
                }

                if (userDB == null)
                {
                    Log.Error($"Error creating userId: {userId}");
                    return;
                }
            }

            await creatorController.SendWelcomeText();
            await creatorController.CreatePet(userDB, Extensions.GetRandomPetName(userDB.Culture));
        }

        private async void SendGramadsPostToChat(long chatId)
        {
            const int TIMEOUT_SECONDS = 5;
#if DEBUG || STAGING || DEBUG_NOTIFY
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

        private async Task OnPreCheckoutQuery(Telegram.Bot.Types.Payments.PreCheckoutQuery preCheckoutQuery)
        {
            Log.Information($"Received PreCheckoutQuery: {preCheckoutQuery.Id} from {preCheckoutQuery.From.Id}");
            await _appServices.BotControlService.AnswerPreCheckoutQueryAsync(preCheckoutQuery.Id, true);
        }

        private async Task OnSuccessfulPayment(Message message)
        {
            var successfulPayment = message.SuccessfulPayment;
            var payload = successfulPayment.InvoicePayload;
            var userId = message.From.Id;

            int amountDiamonds = 0;
            if (payload == Constants.PaymentItems.DiamondPack1.Name) amountDiamonds = Constants.PaymentItems.DiamondPack1.Amount;
            else if (payload == Constants.PaymentItems.DiamondPack2.Name) amountDiamonds = Constants.PaymentItems.DiamondPack2.Amount;
            else if (payload == Constants.PaymentItems.DiamondPack3.Name) amountDiamonds = Constants.PaymentItems.DiamondPack3.Amount;
            else if (payload == Constants.PaymentItems.DiamondPack4.Name) amountDiamonds = Constants.PaymentItems.DiamondPack4.Amount;

            if (amountDiamonds > 0)
            {
                var starPayment = new Models.Mongo.StarPayment
                {
                    UserId = userId,
                    TelegramPaymentChargeId = successfulPayment.TelegramPaymentChargeId,
                    ProviderPaymentChargeId = successfulPayment.ProviderPaymentChargeId,
                    Amount = successfulPayment.TotalAmount,
                    Currency = successfulPayment.Currency,
                    PaymentDate = DateTime.UtcNow
                };
                await _appServices.PaymentService.Create(starPayment);

                var user = _appServices.UserService.Get(userId);
                _appServices.UserService.UpdateDiamonds(userId, (user?.Diamonds ?? 0) + amountDiamonds);

                var culture = user?.Culture ?? "ru";
                await _appServices.BotControlService.SendTextMessageAsync(userId,
                                                                        string.Format(nameof(TamagotchiBot.Resources.Resources.payment_success_message).UseCulture(culture), amountDiamonds)
                                                                        );
            }

            Log.Information($"User {userId} successful payment: {successfulPayment.TotalAmount} {successfulPayment.Currency}, payload: {payload}");
        }

        public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
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
