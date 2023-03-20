using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using TamagotchiBot.Controllers;
using TamagotchiBot.Models.Answers;
using TamagotchiBot.Services;
using TamagotchiBot.UserExtensions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TamagotchiBot.Handlers
{
    public class UpdateHandler : IUpdateHandler
    {
        private readonly UserService userService;
        private readonly PetService petService;
        private readonly ChatService chatService;
        private readonly SInfoService sinfoService;

        public UpdateHandler(UserService userService,
                             PetService petService,
                             ChatService chatService,
                             SInfoService sinfoService)
        {
            this.userService = userService;
            this.petService = petService;
            this.chatService = chatService;
            this.sinfoService = sinfoService;
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
            Task task = update.Type switch
            {
                UpdateType.Message => BotOnMessageReceived(bot, update.Message),
                UpdateType.CallbackQuery => BotOnCallbackQueryReceived(bot, update.CallbackQuery),
                _ => Task.CompletedTask
            };

            var userId = update.Message?.From.Id ?? update.CallbackQuery.From.Id;
            bot.SetMyCommandsAsync(Extensions.GetCommands(petService.Get(userId) is not null), cancellationToken: token);
            sinfoService.UpdateLastGlobalUpdate();
            return task;

            async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
            {
                var controller = new GameController(botClient, userService, petService, chatService, message);
                Answer toSend = null;

                if (userService.Get(message.From.Id) == null)
                    toSend = controller.CreateUser();
                else
                    try
                    {
                        toSend = controller.Process();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message, ex.StackTrace);
                    }


                if (toSend == null)
                    return;

                SendMessage(botClient, toSend, message);

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
                var controller = new GameController(bot, userService, petService, chatService, callbackQuery);
                var toSend = controller.CallbackHandler();

                if (toSend == null)
                    return;

                Log.Information($"Message send to @{callbackQuery.From.Username}: {toSend.Text.Replace("\r\n", " ")}");

                await bot.EditMessageTextAsync(callbackQuery.From.Id,
                                               callbackQuery.Message.MessageId,
                                               toSend.Text,
                                               replyMarkup: toSend.InlineKeyboardMarkup,
                                               cancellationToken: token);

            }

            async void SendMessage(ITelegramBotClient botClient, Answer toSend, Message messageFromUser)
            {
                if (toSend.StickerId != null)
                {
                    await botClient.SendStickerAsync(messageFromUser.From.Id,
                                                     toSend.StickerId,
                                                     cancellationToken: token);

                    if (toSend.ReplyMarkup == null && toSend.InlineKeyboardMarkup == null)
                        await botClient.SendTextMessageAsync(messageFromUser.From.Id,
                                                             toSend.Text,
                                                             cancellationToken: token);
                }

                if (toSend.ReplyMarkup != null)
                    await botClient.SendTextMessageAsync(messageFromUser.From.Id,
                                                         toSend.Text,
                                                         replyMarkup: toSend.ReplyMarkup,
                                                         cancellationToken: token);


                if (toSend.InlineKeyboardMarkup != null)
                    await botClient.SendTextMessageAsync(messageFromUser.From.Id,
                                                         toSend.Text,
                                                         replyMarkup: toSend.InlineKeyboardMarkup,
                                                         cancellationToken: token);
                if (toSend.IsPetGoneMessage)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), token);
                    await botClient.SendStickerAsync(messageFromUser.From.Id,
                                                     Constants.StickersId.PetEpilogue_Cat,
                                                     cancellationToken: token);

                    await botClient.SendTextMessageAsync(messageFromUser.From.Id,
                                     Resources.Resources.EpilogueText,
                                     cancellationToken: token);
                }


            }
        }


    }
}
