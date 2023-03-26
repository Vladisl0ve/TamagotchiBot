using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using TamagotchiBot.Controllers;
using TamagotchiBot.Models.Answers;
using TamagotchiBot.Services;
using TamagotchiBot.UserExtensions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TamagotchiBot.Handlers
{
    public class UpdateHandler : IUpdateHandler
    {
        private readonly UserService userService;
        private readonly PetService petService;
        private readonly ChatService chatService;
        private readonly SInfoService sinfoService;
        private readonly AppleGameDataService appleGameDataService;

        public UpdateHandler(UserService userService,
                             PetService petService,
                             ChatService chatService,
                             SInfoService sinfoService,
                             AppleGameDataService appleGameDataService)
        {
            this.userService = userService;
            this.petService = petService;
            this.chatService = chatService;
            this.sinfoService = sinfoService;
            this.appleGameDataService = appleGameDataService;
        }


        public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Log.Error(exception, exception.Message);
            Log.Warning("App restarts in 10 seconds...");

            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            var startExe = AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName + ".exe";
            // Starts a new instance of the program itself
            System.Diagnostics.Process.Start(startExe);

            // Closes the current process
            Environment.Exit(-1);
        }

        public Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken token)
        {
            var userId = update.Message?.From.Id ?? update.CallbackQuery.From.Id;

            Task task = update.Type switch
            {
                UpdateType.Message => BotOnMessageReceived(bot, update.Message),
                UpdateType.CallbackQuery => BotOnCallbackQueryReceived(bot, update.CallbackQuery),
                _ => Task.CompletedTask
            };

            if (petService.Get(userId) is not null && petService.Get(userId).Name is not null && (!userService.Get(userId)?.IsInAppleGame ?? false))
            {
                bot.SetMyCommandsAsync(Extensions.GetCommands(true),
                                       cancellationToken: token,
                                       scope: new BotCommandScopeChat() { ChatId = userId });
            }
            else if (userService.Get(userId)?.IsInAppleGame ?? false)
            {
                bot.SetMyCommandsAsync(Extensions.GetIngameCommands(),
                       cancellationToken: token,
                       scope: new BotCommandScopeChat() { ChatId = userId });
            }
            else
            {
                bot.SetMyCommandsAsync(Extensions.GetCommands(false),
                       cancellationToken: token,
                       scope: new BotCommandScopeChat() { ChatId = userId });
            }

            sinfoService.UpdateLastGlobalUpdate();
            return task;

            async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
            {
                var menuController = new MenuController(botClient, userService, petService, chatService, message);
                var gameController = new AppleGameController(botClient, userService, petService, chatService, appleGameDataService, message);
                Answer toSend = null;

                if (userService.Get(message.From.Id) == null)
                    toSend = menuController.CreateUser();
                else
                    try
                    {
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
                        Log.Error(ex.Message, ex.StackTrace);
                    }


                if (toSend == null)
                    return;

                SendMessage(botClient, toSend, message.From.Id);

                //if new player has started the game
                if (petService.Get(message.From.Id) == null
                    && (chatService.Get(message.Chat.Id)?.LastMessage == null)
                    && toSend.StickerId != null
                    && toSend.StickerId != Constants.StickersId.ChangeLanguageSticker
                    && toSend.StickerId != Constants.StickersId.PetGone_Cat
                    && toSend.StickerId != Constants.StickersId.HelpCommandSticker)
                    await BotOnMessageReceived(botClient, message);

                Log.Information($"Message send to @{message.From.Username}: {toSend.Text.Replace("\r\n", " ")}");


            }

            async Task BotOnCallbackQueryReceived(ITelegramBotClient bot, CallbackQuery callbackQuery)
            {
                if (callbackQuery.Data == null)
                    return;

                if (userService.Get(userId).IsInAppleGame)
                    return;

                if (callbackQuery.Data == "gameroomCommandInlineCard")
                {
                    var petDB = petService.Get(userId);
                    if (petDB?.Fatigue >= 100)
                    {

                        string anwser = string.Format(Resources.Resources.tooTiredText);
                        await bot.AnswerCallbackQueryAsync(callbackQuery.Id, anwser, true, cancellationToken: token);
                        return;
                    }

                    userService.UpdateAppleGameStatus(callbackQuery.From.Id, true);
                    await bot.SetMyCommandsAsync(Extensions.GetIngameCommands(),
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

                    var gameController = new AppleGameController(bot, userService, petService, chatService, appleGameDataService, callbackQuery);
                    Answer toSendAnswer = gameController.StartGame();

                    SendMessage(bot, toSendAnswer, callbackQuery.From.Id);
                    return;
                }

                var controller = new MenuController(bot, userService, petService, chatService, callbackQuery);
                AnswerCallback toSend = controller.CallbackHandler();

                if (toSend == null)
                    return;

                Log.Information($"Message send to @{callbackQuery.From.Username}: {toSend.Text.Replace("\r\n", " ")}");

                await bot.EditMessageTextAsync(callbackQuery.From.Id,
                                               callbackQuery.Message.MessageId,
                                               toSend.Text,
                                               replyMarkup: toSend.InlineKeyboardMarkup,
                                               cancellationToken: token);

            }

            async void SendMessage(ITelegramBotClient botClient, Answer toSend, long userId)
            {
                if (toSend.StickerId != null)
                {
                    await botClient.SendStickerAsync(userId,
                                                     toSend.StickerId,
                                                     cancellationToken: token);

                    if (toSend.ReplyMarkup == null && toSend.InlineKeyboardMarkup == null)
                        await botClient.SendTextMessageAsync(userId,
                                                             toSend.Text,
                                                             cancellationToken: token);
                }

                if (toSend.ReplyMarkup != null)
                {
                    if (toSend.ReplyMarkup is ReplyKeyboardRemove reply && reply.RemoveKeyboard && chatService.Get(userId)?.LastMessage == "/quitApple")
                    {
                        await botClient.SendTextMessageAsync(userId,
                                                             Resources.Resources.backToGameroomText,
                                                             replyMarkup: toSend.ReplyMarkup,
                                                             cancellationToken: token);
                    }
                    else
                        await botClient.SendTextMessageAsync(userId,
                                                             toSend.Text,
                                                             replyMarkup: toSend.ReplyMarkup,
                                                             cancellationToken: token);
                }


                if (toSend.InlineKeyboardMarkup != null)
                    await botClient.SendTextMessageAsync(userId,
                                                         toSend.Text,
                                                         replyMarkup: toSend.InlineKeyboardMarkup,
                                                         cancellationToken: token);
                if (toSend.IsPetGoneMessage)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), token);
                    await botClient.SendStickerAsync(userId,
                                                     Constants.StickersId.PetEpilogue_Cat,
                                                     cancellationToken: token);

                    await botClient.SendTextMessageAsync(userId,
                                     Resources.Resources.EpilogueText,
                                     cancellationToken: token);
                }


            }
        }


    }
}
