using Serilog;
using System.Globalization;
using TamagotchiBot.Models.Answers;
using TamagotchiBot.Services.Mongo;
using TamagotchiBot.UserExtensions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static TamagotchiBot.Resources.Resources;
using static TamagotchiBot.UserExtensions.Constants;

namespace MaintanceAlertBot.Handlers
{
    public class UpdateHandler : IUpdateHandler
    {
        private readonly UserService _userService;

        public UpdateHandler(UserService userService)
        {
            _userService = userService;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var handler = update.Type switch
            {
                UpdateType.Message => BotOnMessageReceived(botClient, update.Message!),
                UpdateType.EditedMessage => BotOnMessageReceived(botClient, update.EditedMessage!),
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
            Log.Information($"Receive message type: {message.Type}");
            if (message.Type != MessageType.Text)
                return;

            var chatId = message.Chat.Id;
            var messageText = message.Text;
            var userId = message.From?.Id;

            Log.Information($"Received a '{messageText}' message in chat {chatId}.");

            var userCulture = GetUserCulture(userId ?? -1);

            string linkToDiscussChat = "https://t.me/news_virtualpetbot";
            string toSendText = string.Format(
                nameof(changelogCommand).UseCulture(userCulture),
                linkToDiscussChat);

            string toSendAdditionalText = nameof(BotOnMaintanceWarning).UseCulture(userCulture);

            var toSend = new AnswerMessage()
            {
                Text = toSendAdditionalText + toSendText,
                StickerId = StickersId.ChangelogCommandSticker,
                InlineKeyboardMarkup = new InlineKeyboardButton(nameof(ChangelogGoToDicussChannelButton).UseCulture(userCulture))
                {
                    Url = linkToDiscussChat
                },
                ReplyMarkup = ReplyKeyboardItems.MenuKeyboardMarkup(userCulture)
            };

            await botClient.SendSticker(chatId: chatId,
                            sticker: new InputFileId(toSend.StickerId));


            await botClient.SendMessage(chatId: chatId,
                                        text: toSend.Text,
                                        replyMarkup: toSend.ReplyMarkup,
                                        linkPreviewOptions: linkToDiscussChat);
        }

        protected virtual CultureInfo GetUserCulture(long userId)
        {
            return new CultureInfo(_userService.Get(userId)?.Culture ?? "ru");
        }

        private Task UnknownUpdateHandlerAsync(ITelegramBotClient botClient, Update update)
        {
            Log.Information($"Unknown update type: {update.Type}");
            return Task.CompletedTask;
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

            Log.Error(exception, ErrorMessage);
            await Task.Delay(1000, cancellationToken);

            if (exception is ApiRequestException)
                Environment.Exit(-1);
        }
    }
}
