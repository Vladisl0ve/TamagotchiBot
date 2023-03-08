using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using TamagotchiBot.Controllers;
using TamagotchiBot.Models.Anwsers;
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

        public UpdateHandler(UserService userService,
                             PetService petService,
                             ChatService chatService)
        {
            this.userService = userService;
            this.petService = petService;
            this.chatService = chatService;
        }


        public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Log.Error(exception, exception.Message);
            Log.Warning("App restarts in 10 seconds...");

            Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            var startExe = AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName + ".exe";
            // Starts a new instance of the program itself
            System.Diagnostics.Process.Start(startExe);

            // Closes the current process
            Environment.Exit(-1);
            return Task.CompletedTask;
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
            bot.SetMyCommandsAsync(Extensions.GetCommands(petService.Get(userId)));
            return task;

            async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
            {
                var controller = new GameController(botClient, userService, petService, chatService, message);
                Answer toSend = null;
                if (userService.Get(message.From.Id) != null)
                {
                    try
                    {
                        toSend = controller.Process();
                    }
                    catch { }
                }
                else
                    toSend = controller.CreateUser();

                if (toSend == null)
                    return;

                if (toSend.StickerId != null)
                {
                    await botClient.SendStickerAsync(message.From.Id, toSend.StickerId);

                    if (toSend.ReplyMarkup == null && toSend.InlineKeyboardMarkup == null)
                        await botClient.SendTextMessageAsync(message.From.Id, toSend.Text);
                }

                if (toSend.ReplyMarkup != null)
                {
                    await botClient.SendTextMessageAsync(message.From.Id, toSend.Text, replyMarkup: toSend.ReplyMarkup);
                }

                if (toSend.InlineKeyboardMarkup != null)
                {
                    await botClient.SendTextMessageAsync(message.From.Id, toSend.Text, replyMarkup: toSend.InlineKeyboardMarkup);
                }

                if (petService.Get(message.From.Id) == null
                    && chatService.Get(message.Chat.Id)?.LastMessage == null
                    && toSend.StickerId != null
                    && toSend.StickerId != Constants.ChangeLanguageSticker)
                {
                    await BotOnMessageReceived(botClient, message);
                }

                Log.Information($"Message send to @{message.From.Username}: {toSend.Text.Substring(0, toSend.Text.Length > 10 ? 10 : toSend.Text.Length)}");


            }

            async Task BotOnCallbackQueryReceived(ITelegramBotClient bot, CallbackQuery callbackQuery)
            {
                var controller = new GameController(bot, userService, petService, chatService, callbackQuery);
                var toSend = controller.CallbackHandler();

                if (toSend == null)
                    return;

                Log.Information($"Message send to @{callbackQuery.From.Username}: {toSend.Text.Substring(0, toSend.Text.Length > 10 ? 10 : toSend.Text.Length)}");

                await bot.EditMessageTextAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId, toSend.Text, replyMarkup: toSend.InlineKeyboardMarkup);

            }
        }


    }
}
