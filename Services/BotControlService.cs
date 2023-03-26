using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TamagotchiBot.Models.Answers;
using TamagotchiBot.Models.Mongo;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace TamagotchiBot.Services
{
    public class BotControlService
    {
        private ITelegramBotClient _botClient;
        private UserService _userService;
        private PetService _petService;
        public BotControlService(ITelegramBotClient bot, UserService userService, PetService petService)
        {
            _botClient = bot;
            _userService = userService;
            _petService = petService;
        }

        public async void SendTextMessageAsync(long userId, string text, IReplyMarkup replyMarkup = default, CancellationToken cancellationToken = default)
        {
            var userDB = _userService.Get(userId);

            if (userDB == null)
            {
                Log.Warning("There is no user with id:" + userId + "\nNo stickerId has been sent");
                return;
            }

            try
            {
                await _botClient.SendTextMessageAsync(userId,
                                     text,
                                     replyMarkup: replyMarkup,
                                     cancellationToken: cancellationToken);

                Log.Information($"Message send to @{userDB.Username ?? userDB.FirstName}: {text.Replace("\r\n", " ")}");
            }
            catch (ApiRequestException ex)
            {
                Log.Error($"{ex.ErrorCode}: {ex.Message}, user: {userDB.Username ?? userDB.FirstName}");
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}, user: {userDB.Username ?? userDB.FirstName}");
            }
        }

        public async void SendStickerAsync(long userId, string stickerId, CancellationToken cancellationToken = default)
        {
            var userDB = _userService.Get(userId);

            if (userDB == null)
            {
                Log.Warning("There is no user with id:" + userId + "\nNo sticker has been sent");
                return;
            }

            try
            {
                await _botClient.SendStickerAsync(userId,
                                     stickerId,
                                     cancellationToken: cancellationToken);

                Log.Information("Sticker sent for @" + userDB.Username ?? userDB.FirstName);
            }
            catch (ApiRequestException ex)
            {
                Log.Error($"{ex.ErrorCode}: {ex.Message}, user: {userDB.Username ?? userDB.FirstName}");
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}, user: {userDB.Username ?? userDB.FirstName}");
            }
        }

        public async void EditMessageTextAsync(long userId, int messageId, string text, InlineKeyboardMarkup replyMarkup = default, CancellationToken cancellationToken = default)
        {
            var userDB = _userService.Get(userId);

            if (userDB == null)
            {
                Log.Warning("There is no user with id:" + userId + "\nMessage has not been modified");
                return;
            }

            try
            {
                await _botClient.EditMessageTextAsync(userId,
                                               messageId,
                                               text,
                                               replyMarkup: replyMarkup,
                                               cancellationToken: cancellationToken);

                Log.Information($"Message edited for @{userDB.Username ?? userDB.FirstName}: {text.Replace("\r\n", " ")}");
            }
            catch (ApiRequestException ex)
            {
                Log.Error($"{ex.ErrorCode}: {ex.Message}, user: {userDB.Username ?? userDB.FirstName}");
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}, user: {userDB.Username ?? userDB.FirstName}");
            }
        }
        public async void EditMessageReplyMarkupAsync(ChatId chatId, long userId, int messageId, InlineKeyboardMarkup replyMarkup = default, CancellationToken cancellationToken = default)
        {
            var userDB = _userService.Get(userId);

            if (userDB == null)
            {
                Log.Warning("There is no user with id:" + userId + "\nMessage reply has not been modified");
                return;
            }

            try
            {
                await _botClient.EditMessageReplyMarkupAsync(chatId, messageId, replyMarkup: replyMarkup, cancellationToken: cancellationToken);

                Log.Information("Message reply edited for @" + userDB.Username ?? userDB.FirstName);
            }
            catch (ApiRequestException ex)
            {
                Log.Error($"{ex.ErrorCode}: {ex.Message}, user: {userDB.Username ?? userDB.FirstName}");
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}, user: {userDB.Username ?? userDB.FirstName}");
            }
        }
    
        public async void AnswerCallbackQueryAsync(string callbackQueryId, long userId, string text = default, bool showAlert = false, CancellationToken cancellationToken = default)
        {
            var userDB = _userService.Get(userId);

            if (userDB == null)
            {
                Log.Warning("There is no user with id:" + userId + "\nCallback has not been answered");
                return;
            }

            try
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQueryId,
                                               text: text,
                                               showAlert: showAlert,
                                               cancellationToken: cancellationToken);

                Log.Information($"Answered callback for @{userDB.Username ?? userDB.FirstName}: {text.Replace("\r\n", " ")}");
            }
            catch (ApiRequestException ex)
            {
                Log.Error($"{ex.ErrorCode}: {ex.Message}, user: {userDB.Username ?? userDB.FirstName}");
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}, user: {userDB.Username ?? userDB.FirstName}");
            }

        }

        public async void SetMyCommandsAsync(long userId, IEnumerable<BotCommand> commands, BotCommandScope scope = default, CancellationToken cancellationToken = default)
        {
            var userDB = _userService.Get(userId);

            if (userDB == null)
            {
                Log.Warning("There is no user with id:" + userId + "\nCommands have not been set");
                return;
            }

            try
            {
                await _botClient.SetMyCommandsAsync(commands,
                                                    scope: scope,
                                                    cancellationToken: cancellationToken);
                //Log.Information("Commands updated for @" + userDB.Username ?? userDB.FirstName);
            }
            catch (ApiRequestException ex)
            {
                Log.Error($"{ex.ErrorCode}: {ex.Message}, user: {userDB.Username ?? userDB.FirstName}");
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}, user: {userDB.Username ?? userDB.FirstName}");
            }
        }


    }
}
