using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


namespace MaintanceAlertBot.Handlers
{
    public class UpdateHandler : IUpdateHandler
    {
        public UpdateHandler()
        {
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

            Log.Information($"Received a '{messageText}' message in chat {chatId}.");

            // Echo received message text
            // await botClient.SendTextMessageAsync(
            //     chatId: chatId,
            //     text: "You said:\n" + messageText
            // );
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
        }
    }
}
