using Serilog;
using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TamagotchiBot.Controllers;
using TamagotchiBot.Models.Answers;
using TamagotchiBot.Services;
using TamagotchiBot.Services.Mongo;
using TamagotchiBot.UserExtensions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Newtonsoft.Json;
using TamagotchiBot.Services.Interfaces;

namespace TamagotchiBot.Handlers
{
    public class UpdateHandler : IUpdateHandler
    {
        private readonly IApplicationServices _appServices;
        private readonly UserService userService;
        private readonly PetService petService;
        private readonly ChatService chatService;
        private readonly SInfoService sinfoService;
        private readonly AppleGameDataService appleGameDataService;
        private readonly BotControlService bcService;

        public UpdateHandler(IApplicationServices services)
        {
            _appServices = services;
            userService = services.UserService;
            petService = services.PetService;
            chatService = services.ChatService;
            sinfoService = services.SInfoService;
            appleGameDataService = services.AppleGameDataService;
            bcService = services.BotControlService;
        }

        public Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken token)
        {
            if (!ToContinueHandlingUpdateChecking(update))
                return Task.CompletedTask;

            var messageFromUser = update.Message;
            var callbackFromUser = update.CallbackQuery;
            var userId = messageFromUser?.From.Id ?? callbackFromUser?.From.Id ?? default;
            Task task = update.Type switch
            {
                UpdateType.Message => OnMessagePrivate(update.Message),
                UpdateType.CallbackQuery => OnCallbackPrivate(update.CallbackQuery),
                _ => Task.CompletedTask
            };

            new SetCommandController(_appServices, messageFromUser, callbackFromUser).UpdateCommands();

            sinfoService.UpdateLastGlobalUpdate();
            return task;

            async Task OnMessagePrivate(Message message)
            {
                AnswerMessage toSend = null;

                if (!IsUserAndPetRegisteredChecking(userId))
                {
                    RegisterUserAndPet(message); //CreatorController
                    return;
                }

                if (!DidUserChoseLanguage(userId))
                {
                    new CreatorController(_appServices, message).ApplyNewLanguage(true);
                    return;
                }

                if (DidUserConfirmNewPetName(userId) ?? false)
                {
                    new CreatorController(_appServices, message).ToRenamingAnswer();
                    return;
                }

                if (DidUserChoseNewPetName(userId) ?? false)
                { 
                    new CreatorController(_appServices, message).AskToConfirmNewName();
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

                try
                {
                    // call this method wherever you want to show an ad,
                    // for example your bot just made its job and
                    // it's a great time to show an ad to a user
                    if (_appServices.PetService.Get(message.From.Id)?.Name != null)
                        await SendPostToChat(message.From.Id);

                    if (userService.Get(message.From.Id).IsInAppleGame)
                        toSend = new AppleGameController(_appServices, message).Menu();
                    else
                        toSend = new MenuController(_appServices, message).ProcessMessage();

                }
                catch (ApiRequestException apiEx)
                {
                    Log.Error($"{apiEx.ErrorCode}: {apiEx.Message}, user: {message.From.Username ?? message.From.FirstName}");
                }
                catch (RequestException recEx)
                {
                    Log.Error($"{recEx.Source}: {recEx.Message}, user: {message.From.Username ?? message.From.FirstName}");
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message + Environment.NewLine + ex.StackTrace);
                }

                _appServices.BotControlService.SendAnswerMessageAsync(toSend, message.From.Id);
            }

            async Task OnCallbackPrivate(CallbackQuery callbackQuery)
            {
                if (userService.Get(callbackQuery.From.Id) == null || petService.Get(callbackQuery.From.Id) == null)
                    return;

                if (callbackQuery.Data == null)
                    return;

                if (userService.Get(userId)?.IsInAppleGame ?? false)
                    return;

                if (callbackQuery.Data == new CallbackButtons.GameroomCommand().GameroomCommandInlineAppleGame.CallbackData) 
                {
                    new AppleGameController(_appServices, callbackQuery).PreStart();
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
                if (update.Message.Type == MessageType.Text)
                    if (update.Message.From != null)
                        if (update.Message.Chat.Id == update.Message.From.Id)
                            if (update.Message.ForwardDate == null)
                                return true;

            if (update.Type == UpdateType.CallbackQuery)
                if (update.CallbackQuery.Message != null)
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
                if (!creatorController.ApplyNewLanguage())
                    return;
                creatorController.SendWelcomeText();
                await Task.Delay(1000);
                creatorController.AskForAPetName();
            }
            else if (userDB.IsPetNameAskedOnCreate)
            {
                creatorController = new CreatorController(_appServices, message);
                if (!creatorController.CreatePet())
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
            Log.Error(exception, exception.Message);
            Log.Warning("App restarts in 10 seconds...");
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

            if (OperatingSystem.IsWindows())
            {
                var startWinExe = AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName + ".exe";
                // Starts a new instance of the program itself
                Process.Start(startWinExe);
            }
            else if (OperatingSystem.IsLinux())
            {
                var processInfo = new ProcessStartInfo()
                {
                    FileName = "bash",
                    Arguments = $"-c /home/vladislove/Tamagotchi/wrapper.sh",
                    UseShellExecute = true,
                };

                // Starts a new instance of the program itself
                Process.Start(processInfo);
            }

            // Closes the current process
            Environment.Exit(-1);
        }
    }
}
