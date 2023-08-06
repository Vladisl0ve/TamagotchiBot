using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using TamagotchiBot.Services.Mongo;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TamagotchiBot.Services
{
    public class BotControlService
    {
        private ITelegramBotClient _botClient;
        private UserService _userService;
        private PetService _petService;
        private AllUsersDataService _allUsersDataService;
        private ChatService _chatService;
        private AppleGameDataService _appleGameDataService;
        public BotControlService(ITelegramBotClient bot,
                                 UserService userService,
                                 PetService petService,
                                 ChatService chatService,
                                 AppleGameDataService appleGameDataService,
                                 AllUsersDataService allUsersDataService)
        {
            _botClient = bot;
            _userService = userService;
            _petService = petService;
            _chatService = chatService;
            _appleGameDataService = appleGameDataService;
            _allUsersDataService = allUsersDataService;
        }

        public async void SendTextMessageAsync(long userId,
                                               string text,
                                               IReplyMarkup replyMarkup = default,
                                               CancellationToken cancellationToken = default,
                                               ParseMode? parseMode = null,
                                               bool toLog = true)
        {
            var user = _userService.Get(userId);
            Resources.Resources.Culture = new CultureInfo(user?.Culture ?? "ru");

            try
            {
                if (toLog)
                    Log.Information($"Message sent to @{user?.Username ?? userId.ToString()}");

                Log.Verbose($"Message sent to @{user?.Username ?? userId.ToString()}: {text.Replace("\r\n", " ")}");
                await _botClient.SendTextMessageAsync(userId,
                                     text,
                                     replyMarkup: replyMarkup,
                                     cancellationToken: cancellationToken,
                                     parseMode: parseMode);
            }
            catch (ApiRequestException ex)
            {
                if (ex.ErrorCode == 403) //Forbidden by user
                {
                    //remove all data about user
                    _chatService.Remove(userId);
                    _petService.Remove(userId);
                    _userService.Remove(userId);
                    _appleGameDataService.Delete(userId);
                }
                Log.Warning($"{ex.Message} @{user?.Username}, id: {userId}");
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}, user: {user?.Username ?? userId.ToString()}");
            }
        }

        public async void SendStickerAsync(long userId,
                                           string stickerId,
                                           CancellationToken cancellationToken = default,
                                           bool toLog = true)
        {
            var user = _userService.Get(userId);

            try
            {
                if (toLog)
                    Log.Information("Sticker sent for @" + user?.Username ?? userId.ToString());

                Log.Verbose("Sticker sent for @" + user?.Username ?? userId.ToString());
                await _botClient.SendStickerAsync(userId,
                                     stickerId,
                                     cancellationToken: cancellationToken);
            }
            catch (ApiRequestException ex)
            {
                if (ex.ErrorCode == 403) //Forbidden by user
                {
                    Log.Warning($"{ex.Message} @{user?.Username}, id: {userId}");

                    //remove all data about user
                    _chatService.Remove(userId);
                    _petService.Remove(userId);
                    _userService.Remove(userId);
                    _appleGameDataService.Delete(userId);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}, user: {user?.Username ?? userId.ToString()}");
            }
        }

        public async void EditMessageTextAsync(long userId, int messageId, string text, InlineKeyboardMarkup replyMarkup = default, CancellationToken cancellationToken = default, ParseMode? parseMode = null)
        {
            var userDB = _allUsersDataService.Get(userId);
            if (userDB == null)
                Log.Warning("There is no user with id:" + userId);

            try
            {
                Log.Information($"Message edited for @{userDB?.Username ?? userId.ToString()}");
                Log.Verbose($"Message edited for @{userDB?.Username ?? userId.ToString()}: {text.Replace("\r\n", " ")}");

                await _botClient.EditMessageTextAsync(userId,
                                               messageId,
                                               text,
                                               replyMarkup: replyMarkup,
                                               cancellationToken: cancellationToken,
                                               parseMode: parseMode);
            }
            catch (ApiRequestException ex)
            {
                if (ex.ErrorCode != 400)
                    Log.Error($"{ex.ErrorCode}: {ex.Message}, user: {userDB?.Username ?? userId.ToString()}");
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}, user: {userDB?.Username ?? userId.ToString()}");
            }
        }
        public async void EditMessageReplyMarkupAsync(ChatId chatId, long userId, int messageId, InlineKeyboardMarkup replyMarkup = default, CancellationToken cancellationToken = default)
        {
            var userDB = _allUsersDataService.Get(userId);
            if (userDB == null)
                Log.Warning("There is no user with id:" + userId);

            try
            {
                Log.Information("Message reply edited for @" + userDB.Username ?? userId.ToString());
                await _botClient.EditMessageReplyMarkupAsync(chatId, messageId, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
            }
            catch (ApiRequestException ex)
            {
                Log.Error($"{ex.ErrorCode}: {ex.Message}, user: {userDB?.Username ?? userId.ToString()}");
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}, user: {userDB?.Username ?? userId.ToString()}");
            }
        }

        public async void AnswerCallbackQueryAsync(string callbackQueryId, long userId, string text = default, bool showAlert = false, CancellationToken cancellationToken = default)
        {
            var userDB = _allUsersDataService.Get(userId);
            if (userDB == null)
                Log.Warning("There is no user with id:" + userId);

            try
            {
                Log.Information($"Answered callback for @{userDB?.Username ?? userId.ToString()}");
                Log.Verbose($"Answered callback for @{userDB?.Username ?? userId.ToString()}: {text.Replace("\r\n", " ")}");
                await _botClient.AnswerCallbackQueryAsync(callbackQueryId,
                                               text: text,
                                               showAlert: showAlert,
                                               cancellationToken: cancellationToken);

            }
            catch (ApiRequestException ex)
            {
                Log.Error($"{ex.ErrorCode}: {ex.Message}, user: {userDB?.Username ?? userId.ToString()}");
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}, user: {userDB?.Username ?? userId.ToString()}");
            }
        }

        public async void SetMyCommandsAsync(long userId, IEnumerable<BotCommand> commands, BotCommandScope scope = default, CancellationToken cancellationToken = default)
        {
            var userDB = _allUsersDataService.Get(userId);
            if (userDB == null)
                Log.Warning("There is no user with id:" + userId + ", setCommands");

            try
            {
                await _botClient.SetMyCommandsAsync(commands,
                                                    scope: scope,
                                                    cancellationToken: cancellationToken);
            }
            catch (ApiRequestException ex)
            {
                Log.Error($"{ex.ErrorCode}: {ex.Message}, user: {userDB?.Username ?? userId.ToString()}");
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}, user: {userDB?.Username ?? userId.ToString()}");
            }
        }

        public async void SendChatActionAsync(ChatId chatId, ChatAction chatAction, CancellationToken cancellationToken = default)
        {
            var userDB = _allUsersDataService.Get(chatId.Identifier ?? -1);

            if (userDB == null)
                Log.Warning("There is no user with id:" + chatId + ", setActions");

            try
            {
                await _botClient.SendChatActionAsync(chatId, chatAction, cancellationToken);
            }
            catch (ApiRequestException ex)
            {
                Log.Error($"{ex.ErrorCode}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}");
            }
        }

    }
}
