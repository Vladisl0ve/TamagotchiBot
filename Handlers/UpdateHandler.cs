using Serilog;
using System;
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

        public UpdateHandler(UserService userService,
                             PetService petService,
                             ChatService chatService,
                             SInfoService sinfoService,
                             AppleGameDataService appleGameDataService,
                             AllUsersDataService allUsersService,
                             BotControlService botControlService)
        {
            this.userService = userService;
            this.petService = petService;
            this.chatService = chatService;
            this.sinfoService = sinfoService;
            this.appleGameDataService = appleGameDataService;
            this.allUsersService = allUsersService;
            this.bcService = botControlService;
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
            var userId = update.Message?.From.Id ?? update.CallbackQuery?.From.Id ?? default;

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
                var menuController = new MenuController(botClient, userService, petService, chatService, bcService, allUsersService, message);
                var gameController = new AppleGameController(botClient, userService, petService, chatService, appleGameDataService, allUsersService, bcService, message);
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

            Task BotOnCallbackQueryReceived(ITelegramBotClient bot, CallbackQuery callbackQuery)
            {
                if (callbackQuery.Data == null)
                    return Task.CompletedTask;

                if (userService.Get(userId).IsInAppleGame)
                    return Task.CompletedTask;

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
                        return Task.CompletedTask;
                    }

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
                    return Task.CompletedTask;
                }

                var controller = new MenuController(bot, userService, petService, chatService, bcService, allUsersService, callbackQuery);
                AnswerCallback toSend = controller.CallbackHandler();

                if (toSend == null)
                    return Task.CompletedTask;

                bcService.EditMessageTextAsync(callbackQuery.From.Id,
                                               callbackQuery.Message.MessageId,
                                               toSend.Text,
                                               replyMarkup: toSend.InlineKeyboardMarkup,
                                               cancellationToken: token);
                return Task.CompletedTask;
            }

            async void SendMessage(Answer toSend, long userId)
            {
                if (toSend.StickerId != null)
                {
                    bcService.SendStickerAsync(userId,
                                               toSend.StickerId,
                                               cancellationToken: token);

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
                                                   cancellationToken: token);
                if (toSend.IsPetGoneMessage)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), token);
                    bcService.SendChatActionAsync(userId, ChatAction.Typing, token);
                    await Task.Delay(TimeSpan.FromSeconds(5), token);
                    bcService.SendStickerAsync(userId,
                                               Constants.StickersId.PetEpilogue_Cat,
                                               cancellationToken: token);

                    bcService.SendTextMessageAsync(userId,
                                                   Resources.Resources.EpilogueText,
                                                   cancellationToken: token);
                }


            }
        }


    }
}
