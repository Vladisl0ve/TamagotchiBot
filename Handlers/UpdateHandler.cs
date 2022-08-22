using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using TamagotchiBot.Controllers;
using TamagotchiBot.UserExtensions;
using TamagotchiBot.Services;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
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

        public Task HandleError(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
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

        public Task HandleUpdate(ITelegramBotClient bot, Update update, CancellationToken token)
        {
            Task task = update.Type switch
            {
                UpdateType.Message => BotOnMessageReceived(bot, update.Message),
                UpdateType.CallbackQuery => BotOnCallbackQueryReceived(bot, update.CallbackQuery),
                _ => Task.CompletedTask
            };

            var userId = update.Message?.From.Id ?? update.CallbackQuery.From.Id;
            bot.SetMyCommandsAsync(Extensions.GetCommands(petService.Get(userId)), token);
            return task;

            async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
            {
                var controller = new GameController(botClient, userService, petService, chatService, message);
                Tuple<string, string, IReplyMarkup, InlineKeyboardMarkup> toSend;
                if (userService.Get(message.From.Id) != null)
                    toSend = controller.Process();
                else
                    toSend = controller.CreateUser();

                if (toSend == null)
                    return;

                if (toSend.Item2 != null)
                {
                    await botClient.SendStickerAsync(message.From.Id, toSend.Item2);

                    if (toSend.Item3 == null && toSend.Item4 == null)
                        await botClient.SendTextMessageAsync(message.From.Id, toSend.Item1);
                }

                if (toSend.Item3 != null)
                {
                    await botClient.SendTextMessageAsync(message.From.Id, toSend.Item1, replyMarkup: toSend.Item3);
                }

                if (toSend.Item4 != null)
                {
                    await botClient.SendTextMessageAsync(message.From.Id, toSend.Item1, replyMarkup: toSend.Item4);
                }

                if ((petService.Get(message.From.Id) == null && chatService.Get(message.Chat.Id).LastMessage == null && toSend.Item2 != null && toSend.Item2 != Constants.ChangeLanguageSticker))
                {
                    await BotOnMessageReceived(botClient, message);
                }
                
                Log.Information($"Message send to @{message.From.Username}: {toSend.Item1.Substring(0, toSend.Item1.Length > 10 ? 10 : toSend.Item1.Length)}");


            }

            async Task BotOnCallbackQueryReceived(ITelegramBotClient bot, CallbackQuery callbackQuery)
            {
                var controller = new GameController(bot, userService, petService, chatService, callbackQuery);
                var toSend = controller.CallbackHandler();

                if (toSend == null)
                    return;

                Log.Information($"Message send to @{callbackQuery.From.Username}: {toSend.Item1.Substring(0, toSend.Item1.Length > 10 ? 10 : toSend.Item1.Length)}");

                await bot.EditMessageTextAsync(callbackQuery.From.Id, callbackQuery.Message.MessageId, toSend.Item1, replyMarkup: toSend.Item2);

            }
        }


    }
}
