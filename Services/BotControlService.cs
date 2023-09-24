using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using TamagotchiBot.Models.Answers;
using TamagotchiBot.Services.Mongo;
using TamagotchiBot.UserExtensions;
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
                    Log.Information($"Message sent to {Extensions.GetLogUser(user)}");

                Log.Verbose($"Message sent to {Extensions.GetLogUser(user)}: {text.Replace("\r\n", " ")}");
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
                Log.Warning($"{ex.Message} : {Extensions.GetLogUser(user)}");
            }
            catch (Exception ex)
            {
                Log.Error($"MSG: {ex.Message}, InnerExeption: {ex.InnerException.Message}, USER: {Extensions.GetLogUser(user)}");
            }
        }

        public async void SendStickerAsync(long userId,
                                           string stickerId,
                                           bool toRemoveKeyboard = false,
                                           CancellationToken cancellationToken = default,
                                           bool toLog = true)
        {
            var user = _userService.Get(userId);

            try
            {
                if (toLog)
                    Log.Information($"Sticker sent for {Extensions.GetLogUser(user)}");

                Log.Verbose($"Sticker sent for {Extensions.GetLogUser(user)}");
                if (toRemoveKeyboard)
                {
                    await _botClient.SendStickerAsync(userId,
                     stickerId,
                     replyMarkup: new ReplyKeyboardRemove(),
                     cancellationToken: cancellationToken);
                }
                else
                    await _botClient.SendStickerAsync(userId,
                                         stickerId,
                                         cancellationToken: cancellationToken);
            }
            catch (ApiRequestException ex)
            {
                if (ex.ErrorCode == 403) //Forbidden by user
                {
                    Log.Warning($"{ex.Message} {Extensions.GetLogUser(user)}");

                    //remove all data about user
                    _chatService.Remove(userId);
                    _petService.Remove(userId);
                    _userService.Remove(userId);
                    _appleGameDataService.Delete(userId);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"MSG: {ex.Message}, InnerExeption: {ex.InnerException.Message}, USER: {Extensions.GetLogUser(user)}");
            }
        }

        public async void EditMessageTextAsync(long userId, int messageId, string text, InlineKeyboardMarkup replyMarkup = default, CancellationToken cancellationToken = default, ParseMode? parseMode = null, bool toLog = true)
        {
            var userDB = _userService.Get(userId);
            if (userDB == null)
                Log.Warning("There is no user with id:" + userId);

            try
            {
                if (toLog)
                    Log.Information($"Message edited for {Extensions.GetLogUser(userDB)}");

                Log.Verbose($"Message edited for {Extensions.GetLogUser(userDB)}: {text.Replace("\r\n", " ")}");

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
                    Log.Error($"MSG: {ex.Message}, InnerExeption: {ex.InnerException.Message}, USER: {Extensions.GetLogUser(userDB)}");
            }
            catch (Exception ex)
            {
                Log.Error($"MSG: {ex.Message}, InnerExeption: {ex.InnerException.Message}, USER: {Extensions.GetLogUser(userDB)}");
            }
        }
        public async void EditMessageReplyMarkupAsync(ChatId chatId, long userId, int messageId, InlineKeyboardMarkup replyMarkup = default, CancellationToken cancellationToken = default)
        {
            var userDB = _userService.Get(userId);
            if (userDB == null)
                Log.Warning("There is no user with id:" + userId);

            try
            {
                Log.Information($"Message reply edited for {Extensions.GetLogUser(userDB)}");
                await _botClient.EditMessageReplyMarkupAsync(chatId, messageId, replyMarkup: replyMarkup, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error($"MSG: {ex.Message}, InnerExeption: {ex.InnerException.Message}, USER: {Extensions.GetLogUser(userDB)}");
            }
        }

        public async void AnswerCallbackQueryAsync(string callbackQueryId, long userId, string text = default, bool showAlert = false, CancellationToken cancellationToken = default)
        {
            var userDB = _userService.Get(userId);
            if (userDB == null)
                Log.Warning("There is no user with id:" + userId);

            if (string.IsNullOrEmpty(callbackQueryId))
            {
                Log.Error($"No callbackQueryId for answer. UserId: {userId}");
                return;
            }

            try
            {
                Log.Verbose($"Answered callback for {Extensions.GetLogUser(userDB)}: {text.Replace("\r\n", " ")}");
                await _botClient.AnswerCallbackQueryAsync(callbackQueryId,
                                               text: text,
                                               showAlert: showAlert,
                                               cancellationToken: cancellationToken);

            }
            catch (Exception ex)
            {
                Log.Error($"MSG: {ex.Message}, InnerExeption: {ex.InnerException.Message}, USER: {Extensions.GetLogUser(userDB)}");
            }
        }

        public async void SetMyCommandsAsync(long userId, IEnumerable<BotCommand> commands, BotCommandScope scope = default, CancellationToken cancellationToken = default)
        {
            var userDB = _userService.Get(userId);
            if (userDB == null)
                Log.Warning("There is no user with id:" + userId + ", setCommands");

            try
            {
                await _botClient.SetMyCommandsAsync(commands,
                                                    scope: scope,
                                                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error($"MSG: {ex.Message}, InnerExeption: {ex.InnerException.Message}, USER: {Extensions.GetLogUser(userDB)}");
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

        public async void SendAnswerMessageAsync(AnswerMessage toSend, long userId, bool toLog = true)
        {
            if (toSend == null)
            {
                //Log.Warning($"Nothing to send (null), userID: {userId}");
                return;
            }

            if (toSend.StickerId != null)
            {
                SendStickerAsync(userId,
                                 toSend.StickerId,
                                 toSend.ReplyMarkup?.GetType() == typeof(ReplyKeyboardRemove),
                                 toLog: toLog);
                await Task.Delay(50);
            }

            if (toSend.ReplyMarkup != null && toSend.ReplyMarkup?.GetType() != typeof(ReplyKeyboardRemove))
            {
                SendTextMessageAsync(userId,
                                     toSend.Text,
                                     replyMarkup: toSend.ReplyMarkup,
                                     toLog: toLog);

                return;
            }

            if (toSend.InlineKeyboardMarkup != null)
            {
                SendTextMessageAsync(userId,
                     toSend.Text,
                     replyMarkup: toSend.InlineKeyboardMarkup,
                     parseMode: toSend.ParseMode,
                     toLog: toLog);
                return;
            }

            if (!string.IsNullOrEmpty(toSend.Text))
            {
                SendTextMessageAsync(userId,
                                     toSend.Text,
                                     toLog: toLog);
            }
        }
        public void SendAnswerCallback(long userId, int messageToAnswerId, AnswerCallback toSend, bool toLog = true)
            => EditMessageTextAsync(userId, messageToAnswerId, toSend.Text, toSend.InlineKeyboardMarkup, parseMode: toSend.ParseMode, toLog: toLog);
    }
}
