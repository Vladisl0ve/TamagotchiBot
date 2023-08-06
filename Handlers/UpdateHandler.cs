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
using Telegram.Bot.Types.ReplyMarkups;
using Newtonsoft.Json;
using System.Globalization;

namespace TamagotchiBot.Handlers
{
    public class UpdateHandler : IUpdateHandler
    {
        private readonly UserService userService;
        private readonly PetService petService;
        private readonly ChatService chatService;
        private readonly SInfoService sinfoService;
        private readonly AppleGameDataService appleGameDataService;
        private readonly AllUsersDataService allUsersService;
        private readonly BotControlService bcService;
        private readonly BannedUsersService bannedService;
        private readonly AdsProducersService adsProducersService;

        public UpdateHandler(UserService userService,
                             PetService petService,
                             ChatService chatService,
                             SInfoService sinfoService,
                             AppleGameDataService appleGameDataService,
                             AllUsersDataService allUsersService,
                             BannedUsersService bannedService,
                             AdsProducersService adsProducersService,
                             BotControlService botControlService)
        {
            this.userService = userService;
            this.petService = petService;
            this.chatService = chatService;
            this.sinfoService = sinfoService;
            this.appleGameDataService = appleGameDataService;
            this.allUsersService = allUsersService;
            this.bannedService = bannedService;
            this.adsProducersService = adsProducersService;
            this.bcService = botControlService;
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

        public Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken token)
        {
            var userId = update.Message?.From.Id ?? update.CallbackQuery?.From.Id ?? default;
            CultureInfo culture = CultureInfo.GetCultureInfo(userService.Get(userId)?.Culture ?? "ru");

            Task task = update.Type switch
            {
                UpdateType.Message => BotOnMessageReceived(bot, update.Message),
                UpdateType.CallbackQuery => BotOnCallbackQueryReceived(bot, update.CallbackQuery),
                _ => Task.CompletedTask
            };

            if (petService.Get(userId) is not null && petService.Get(userId).Name is not null && (!userService.Get(userId)?.IsInAppleGame ?? false))
            {
                bcService.SetMyCommandsAsync(userId,
                                             Extensions.GetCommands(true),
                                             cancellationToken: token,
                                             scope: new BotCommandScopeChat() { ChatId = userId });
            }
            else if (userService.Get(userId)?.IsInAppleGame ?? false)
            {
                bcService.SetMyCommandsAsync(userId,
                                             Extensions.GetIngameCommands(),
                                             cancellationToken: token,
                                             scope: new BotCommandScopeChat() { ChatId = userId });
            }
            else
            {
                bcService.SetMyCommandsAsync(userId,
                                             Extensions.GetCommands(false),
                                             cancellationToken: token,
                                             scope: new BotCommandScopeChat() { ChatId = userId });
            }

            sinfoService.UpdateLastGlobalUpdate();
            return task;

            async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
            {
                var menuController = new MenuController(botClient, userService, petService, chatService, bcService, allUsersService, bannedService, appleGameDataService, adsProducersService, message);
                var gameController = new AppleGameController(botClient, userService, petService, chatService, appleGameDataService, allUsersService, bcService, message);
                Answer toSend = null;

                if (userService.Get(message.From.Id) == null)
                    toSend = menuController.CreateUser();
                else
                    try
                    {
                        // call this method wherever you want to show an ad,
                        // for example your bot just made its job and
                        // it's a great time to show an ad to a user
                        if (petService.Get(message.From.Id)?.Name != null)
                            await SendPostToChat(message.From.Id);


                        if (userService.Get(message.From.Id).IsInAppleGame)
                            toSend = gameController.Menu(message);
                        else
                            toSend = menuController.Process();

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

                if (toSend == null)
                    return;

                SendMessage(toSend, message.From.Id);

                //if new player starts the game
                if (petService.Get(message.From.Id) == null
                    && (chatService.Get(message.Chat.Id)?.LastMessage == null)
                    && toSend.StickerId != null
                    && toSend.StickerId != Constants.StickersId.ChangeLanguageSticker
                    && toSend.StickerId != Constants.StickersId.PetGone_Cat
                    && toSend.StickerId != Constants.StickersId.HelpCommandSticker)
                    await BotOnMessageReceived(botClient, message);
            }

            async Task BotOnCallbackQueryReceived(ITelegramBotClient bot, CallbackQuery callbackQuery)
            {
                if (userService.Get(callbackQuery.From.Id) == null || petService.Get(callbackQuery.From.Id) == null)
                    return;

                if (callbackQuery.Data == null)
                    return;

                if (userService.Get(userId)?.IsInAppleGame ?? false)
                    return;

                if (callbackQuery.Data == "gameroomCommandInlineCard")
                {
                    var petDB = petService.Get(userId);
                    if (petDB?.Fatigue >= 100)
                    {
                        string anwser = string.Format(Resources.Resources.tooTiredText);
                        bcService.AnswerCallbackQueryAsync(callbackQuery.Id,
                                                           callbackQuery.From.Id,
                                                           anwser,
                                                           true,
                                                           cancellationToken: token);
                        return;
                    }

                    if (petDB?.Gold - 20 < 0)
                    {
                        string anwser = string.Format(Resources.Resources.goldNotEnough);
                        bcService.AnswerCallbackQueryAsync(callbackQuery.Id,
                                                           callbackQuery.From.Id,
                                                           anwser,
                                                           true,
                                                           cancellationToken: token);
                        return;
                    }

                    petService.UpdateGold(callbackQuery.From.Id, petService.Get(callbackQuery.From.Id).Gold - 20);

                    userService.UpdateAppleGameStatus(callbackQuery.From.Id, true);
                    bcService.SetMyCommandsAsync(callbackQuery.From.Id,
                                                 Extensions.GetIngameCommands(),
                                                 cancellationToken: token,
                                                 scope: new BotCommandScopeChat() { ChatId = userId });
                    var appleData = appleGameDataService.Get(callbackQuery.From.Id);

                    if (appleData == null)
                        appleGameDataService.Create(new Models.Mongo.Games.AppleGameData()
                        {
                            UserId = callbackQuery.From.Id,
                            CurrentAppleCounter = 24,
                            TotalDraws = 0,
                            TotalLoses = 0,
                            TotalWins = 0,
                            IsGameOvered = false,
                        });

                    var gameController = new AppleGameController(bot, userService, petService, chatService, appleGameDataService, allUsersService, bcService, callbackQuery);
                    Answer toSendAnswer = gameController.StartGame();

                    SendMessage(toSendAnswer, callbackQuery.From.Id);
                    return;
                }

                // call this method wherever you want to show an ad,
                // for example your bot just made its job and
                // it's a great time to show an ad to a user

                await SendPostToChat(callbackQuery.From.Id);

                var controller = new MenuController(bot, userService, petService, chatService, bcService, allUsersService, bannedService, appleGameDataService, callbackQuery);
                AnswerCallback toSend = controller.CallbackHandler();

                if (toSend == null)
                    return;

                bcService.EditMessageTextAsync(callbackQuery.From.Id,
                                               callbackQuery.Message.MessageId,
                                               toSend.Text,
                                               replyMarkup: toSend.InlineKeyboardMarkup,
                                               cancellationToken: token,
                                               parseMode: toSend.ParseMode);
            }

            async void SendMessage(Answer toSend, long userId)
            {
                culture ??= CultureInfo.GetCultureInfo(userService.Get(userId)?.Culture ?? "ru");
                if (toSend.StickerId != null)
                {
                    bcService.SendStickerAsync(userId,
                                               toSend.StickerId,
                                               cancellationToken: token);

                    await Task.Delay(50, token);

                    if (toSend.ReplyMarkup == null && toSend.InlineKeyboardMarkup == null)
                        bcService.SendTextMessageAsync(userId,
                                                       toSend.Text,
                                                       cancellationToken: token);
                }

                if (toSend.ReplyMarkup != null)
                {
                    if (toSend.ReplyMarkup is ReplyKeyboardRemove reply && reply.RemoveKeyboard && chatService.Get(userId)?.LastMessage == "/quitApple")
                    {
                        bcService.SendTextMessageAsync(userId,
                                                       Resources.Resources.backToGameroomText,
                                                       replyMarkup: toSend.ReplyMarkup,
                                                       cancellationToken: token);
                    }
                    else
                        bcService.SendTextMessageAsync(userId,
                                                       toSend.Text,
                                                       replyMarkup: toSend.ReplyMarkup,
                                                       cancellationToken: token);
                }

                if (toSend.InlineKeyboardMarkup != null)
                    bcService.SendTextMessageAsync(userId,
                                                   toSend.Text,
                                                   replyMarkup: toSend.InlineKeyboardMarkup,
                                                   cancellationToken: token,
                                                   parseMode: toSend.ParseMode);
                if (toSend.IsPetGoneMessage)
                {
                    Resources.Resources.Culture = culture;
                    await Task.Delay(TimeSpan.FromSeconds(1), token);
                    bcService.SendChatActionAsync(userId, ChatAction.Typing, token);
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                    Resources.Resources.Culture = culture;
                    bcService.SendStickerAsync(userId,
                                               Constants.StickersId.PetEpilogue_Cat,
                                               cancellationToken: token);

                    Resources.Resources.Culture = culture;
                    bcService.SendTextMessageAsync(userId,
                                                   Resources.Resources.EpilogueText,
                                                   cancellationToken: token,
                                                   parseMode: toSend.ParseMode);
                }
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
    }
}
