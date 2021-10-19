using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using TamagotchiBot.Controllers;
using TamagotchiBot.Extensions;
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

        public UpdateHandler(UserService userService,
                             PetService petService)
        {
            this.userService = userService;
            this.petService = petService;
        }

        public UpdateType[] AllowedUpdates => new[] { UpdateType.Message };

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
                _ => Task.CompletedTask
            };
            return task;

            async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message)
            {
                var controller = new GameController(userService, petService, message);
                string toSend = controller.Start();

                if (toSend == null)
                    return;

                Log.Information($"Message send to @{message.From.Username}: {toSend.Substring(0, toSend.Length > 10 ? 10 : toSend.Length)}");

                if (toSend == Resources.Resources.ChangeLanguage)
                {
                    await botClient.SendTextMessageAsync(message.From.Id, toSend, replyMarkup: Constants.LanguagesMarkup);
                    return;
                }

                if (toSend == Resources.Resources.ConfirmedLanguage)
                {
                    await botClient.SendTextMessageAsync(message.From.Id, toSend, replyMarkup: new ReplyKeyboardRemove());
                    return;
                }

                await botClient.SendTextMessageAsync(message.From.Id, toSend);

                static async Task<Message> SendReplyKeyboard(ITelegramBotClient botClient, Message message, Tuple<string, ReplyKeyboardMarkup, string> toSend)
                {
                    return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                                text: toSend.Item1,
                                                                replyMarkup: toSend.Item2);
                }

                static async Task<Message> SendInlineKeyboard(ITelegramBotClient botClient, Message message)
                {
                    await botClient.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

                    // Simulate longer running task
                    await Task.Delay(500);

                    var inlineKeyboard = new InlineKeyboardMarkup(new[]
                    {
                    // first row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("1.1", "11"),
                        InlineKeyboardButton.WithCallbackData("1.2", "12"),
                    },
                    // second row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("2.1", "21"),
                        InlineKeyboardButton.WithCallbackData("2.2", "22"),
                    },
                });

                    return await botClient.SendTextMessageAsync(chatId: message.Chat.Id,
                                                                text: "Choose",
                                                                replyMarkup: inlineKeyboard);
                }

            }

            // Testing new feature
            Task Echo(ITelegramBotClient bot, Message message)
            {
                if (message.Text == null)
                    return Task.CompletedTask;

                Log.Information($"Sending to @{message.Chat.Username}: {message.Text}");
                return bot.SendTextMessageAsync(message.Chat.Id, message.Text);
            }
        }
    }
}
