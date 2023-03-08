using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TamagotchiBot.Controllers;
using TamagotchiBot.Models;
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

        public UpdateHandler(UserService userService,
                             PetService petService,
                             ChatService chatService)
        {
            this.userService = userService;
            this.petService = petService;
            this.chatService = chatService;
        }

        public UpdateType[] AllowedUpdates => new[] { UpdateType.Message, UpdateType.CallbackQuery };

        public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is ApiRequestException apiRequestException)
            {
                var warningMessages = new[] { "bot was blocked by the user", "bot was kicked from the supergroup", "have no rights to send a message" };

                if (warningMessages.Any(x => apiRequestException.Message.Contains(x)))
                {
                    Log.Warning(apiRequestException.Message);
                }
                else
                {
                    Log.Error(apiRequestException, apiRequestException.Message);
                }

                return Task.CompletedTask;
            }

            Log.Error(exception, exception.Message);
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
