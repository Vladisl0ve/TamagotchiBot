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

        public async Task DeleteMessageAsync(long chatId, int msgId, bool toLog = true)
        {
            try
            {
                if (toLog)
                    Log.Information($"Deleting message {msgId} from chat {chatId}");

                Log.Verbose($"Deleting message {msgId} from chat {chatId}");
                await _botClient.DeleteMessageAsync(chatId, msgId);
            }
            catch (Exception ex)
            {
                Log.Error($"MSG: {ex.Message}, InnerExeption: {ex.InnerException?.Message}");
            }
        }

        public async Task<Message> SendDiceMessageAsync(long chatId,
                                                        int msgThreadId,
                                                        Emoji emoji,
                                                        bool toLog = true)
        {
            string logInfo;
            if (chatId < 0)
            {
                logInfo = $"chatId: {chatId}";
            }
            else
            {
                var userDB = _userService.Get(chatId);
                if (userDB == null)
                    Log.Warning("There is no user with id:" + chatId);

                logInfo = Extensions.GetLogUser(userDB);
            }

            try
            {
                if (toLog)
                    Log.Information($"Dice {emoji} sent to {logInfo}");

                Log.Verbose($"Dice {emoji} sent to {logInfo}");
                return await _botClient.SendDiceAsync(chatId, messageThreadId: msgThreadId, emoji: emoji);
            }
            catch (ApiRequestException ex)
            {
                Log.Warning($"{ex.Message} : {logInfo}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error($"DICE MSG: {ex.Message}, InnerExeption: {ex.InnerException?.Message}, USER/CHAT: {logInfo}");
                return null;
            }
        }
        public async Task<Message> SendTextMessageAsync(long chatId,
                                               string text,
                                               int? msgThreadId = null,
                                               IReplyMarkup replyMarkup = default,
                                               CancellationToken cancellationToken = default,
                                               ParseMode? parseMode = null,
                                               bool toLog = true,
                                               int? replyToMsgId = null)
        {
            string logInfo;
            Models.Mongo.User user = null;
            if (chatId < 0)
                logInfo = $"chat id: {chatId}";
            else
            {
                user = _userService.Get(chatId);
                logInfo = $"{Extensions.GetLogUser(user)}";
            }

            Resources.Resources.Culture = new CultureInfo(user?.Culture ?? "ru");

            try
            {
                if (toLog)
                    Log.Information($"Message sent to {logInfo}");

                Log.Verbose($"Message sent to {logInfo}: {text.Replace("\r\n", " ")}");
                return await _botClient.SendTextMessageAsync(chatId: chatId,
                                     text: text,
                                     messageThreadId: msgThreadId,
                                     replyMarkup: replyMarkup,
                                     cancellationToken: cancellationToken,
                                     parseMode: parseMode,
                                     replyToMessageId: replyToMsgId);
            }
            catch (ApiRequestException ex)
            {
                if (ex.ErrorCode == 403) //Forbidden by user
                {
                    //remove all data about user
                    _chatService.Remove(chatId);
                    _petService.Remove(chatId);
                    _userService.Remove(chatId);
                    _appleGameDataService.Delete(chatId);
                }
                Log.Warning($"{ex.Message} : {logInfo}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error($"MSG: {ex.Message}, InnerExeption: {ex.InnerException?.Message}, USER/CHAT: {logInfo}");
                return null;
            }
        }

        public async void SendStickerAsync(long chatId,
                                           string stickerId,
                                           int? msgThreadId = null,
                                           bool toRemoveKeyboard = false,
                                           CancellationToken cancellationToken = default,
                                           bool toLog = true)
        {
            string logInfo;
            if (chatId < 0)
                logInfo = $"chat id: {chatId}";
            else
            {
                var user = _userService.Get(chatId);
                logInfo = $"{Extensions.GetLogUser(user)}";
            }

            try
            {
                if (toLog)
                    Log.Information($"Sticker sent for {logInfo}");

                Log.Verbose($"Sticker sent for {logInfo}");
                if (toRemoveKeyboard)
                {
                    await _botClient.SendStickerAsync(chatId: chatId,
                                                      sticker: new InputFileId(stickerId),
                                                      messageThreadId: msgThreadId,
                                                      replyMarkup: new ReplyKeyboardRemove(),
                                                      cancellationToken: cancellationToken);
                }
                else
                    await _botClient.SendStickerAsync(chatId: chatId,
                                                      sticker: new InputFileId(stickerId),
                                                      messageThreadId: msgThreadId,
                                                      cancellationToken: cancellationToken);
            }
            catch (ApiRequestException ex)
            {
                if (ex.ErrorCode == 403) //Forbidden by user
                {
                    Log.Warning($"{ex.Message} {logInfo}");

                    //remove all data about user
                    _chatService.Remove(chatId);
                    _petService.Remove(chatId);
                    _userService.Remove(chatId);
                    _appleGameDataService.Delete(chatId);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"MSG: {ex.Message}, InnerExeption: {ex.InnerException?.Message}, USER/CHAT: {logInfo}");
            }
        }

        public async Task EditMessageTextAsync(long chatId, int messageId, string text, InlineKeyboardMarkup replyMarkup = default, CancellationToken cancellationToken = default, ParseMode? parseMode = null, bool toLog = true)
        {
            string logInfo;
            if (chatId < 0)
            {
                logInfo = $"chatId: {chatId}, messageId: {messageId}";
            }
            else
            {
                var userDB = _userService.Get(chatId);
                if (userDB == null)
                    Log.Warning("There is no user with id:" + chatId);

                logInfo = Extensions.GetLogUser(userDB);
            }

            try
            {
                if (toLog)
                    Log.Information($"Message edited for {logInfo}");

                Log.Verbose($"Message edited for {logInfo}: {text.Replace("\r\n", " ")}");

                await _botClient.EditMessageTextAsync(chatId,
                                               messageId,
                                               text,
                                               replyMarkup: replyMarkup,
                                               cancellationToken: cancellationToken,
                                               parseMode: parseMode);
            }
            catch (ApiRequestException ex)
            {
                if (ex.ErrorCode != 400)
                    Log.Error($"MSG: {ex.Message}, InnerExeption: {ex.InnerException?.Message}, USER/CHAT: {logInfo}");
            }
            catch (Exception ex)
            {
                Log.Error($"MSG: {ex.Message}, InnerExeption: {ex.InnerException?.Message}, USER/CHAT: {logInfo}");
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
                Log.Error($"MSG: {ex.Message}, InnerExeption: {ex.InnerException?.Message}, USER: {Extensions.GetLogUser(userDB)}");
            }
        }

        public async void AnswerCallbackQueryAsync(string callbackQueryId, long userId, string text = default, bool showAlert = false, CancellationToken cancellationToken = default, string url = default)
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
                                               url: url,
                                               cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error($"MSG: {ex.Message}, InnerExeption: {ex.InnerException?.Message}, USER: {Extensions.GetLogUser(userDB)}");
            }
        }

        public async void SetMyCommandsAsync(IEnumerable<BotCommand> commands, BotCommandScope scope = default, CancellationToken cancellationToken = default)
        {
            try
            {
                await _botClient.SetMyCommandsAsync(commands,
                                                    scope: scope,
                                                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error($"MSG: {ex.Message}, InnerExeption: {ex.InnerException?.Message}");
            }
        }

        public async void SendChatActionAsync(ChatId chatId, ChatAction chatAction, CancellationToken cancellationToken = default)
        {
            var userDB = _allUsersDataService.Get(chatId.Identifier ?? -1);

            if (userDB == null)
                Log.Warning("There is no user with id:" + chatId + ", setActions");

            try
            {
                await _botClient.SendChatActionAsync(chatId, chatAction, cancellationToken: cancellationToken);
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

        public async Task<Message> SendAnswerMessageAsync(AnswerMessage toSend, long userId, bool toLog = true)
        {
            if (toSend == null)
            {
                //Log.Warning($"Nothing to send (null), userID: {userId}");
                return null;
            }

            if (toSend.StickerId != null)
            {
                SendStickerAsync(userId,
                                 toSend.StickerId,
                                 toSend.msgThreadId,
                                 toSend.ReplyMarkup?.GetType() == typeof(ReplyKeyboardRemove),
                                 toLog: toLog);
                await Task.Delay(50);
            }

            if (toSend.ReplyMarkup != null && toSend.ReplyMarkup?.GetType() != typeof(ReplyKeyboardRemove))
            {
                return await SendTextMessageAsync(userId,
                                     toSend.Text,
                                     replyMarkup: toSend.ReplyMarkup,
                                     toLog: toLog,
                                     parseMode: toSend.ParseMode);
            }

            if (toSend.InlineKeyboardMarkup != null)
            {
                return await SendTextMessageAsync(userId,
                     toSend.Text,
                     replyMarkup: toSend.InlineKeyboardMarkup,
                     parseMode: toSend.ParseMode,
                     toLog: toLog);
            }

            if (!string.IsNullOrEmpty(toSend.Text))
            {
                return await SendTextMessageAsync(userId,
                                     toSend.Text,
                                     toLog: toLog,
                                     parseMode: toSend.ParseMode);
            }
            return null;
        }
        public async Task<Message> SendAnswerMessageGroupAsync(AnswerMessage toSend, long chatId, bool toLog = true)
        {
            if (toSend == null)
            {
                //Log.Warning($"Nothing to send (null), userID: {userId}");
                return null;
            }

            if (toSend.StickerId != null)
            {
                SendStickerAsync(chatId,
                                 toSend.StickerId,
                                 msgThreadId: toSend.msgThreadId,
                                 toSend.ReplyMarkup?.GetType() == typeof(ReplyKeyboardRemove),
                                 toLog: toLog);
                await Task.Delay(50);
            }

            if (toSend.ReplyMarkup != null && toSend.ReplyMarkup?.GetType() != typeof(ReplyKeyboardRemove))
            {
                return await SendTextMessageAsync(chatId,
                                                  toSend.Text,
                                                  msgThreadId: toSend.msgThreadId,
                                                  replyMarkup: toSend.ReplyMarkup,
                                                  toLog: toLog,
                                                  replyToMsgId: toSend.replyToMsgId);
            }

            if (toSend.InlineKeyboardMarkup != null)
            {
                return await SendTextMessageAsync(chatId: chatId,
                                                  text: toSend.Text,
                                                  msgThreadId: toSend.msgThreadId,
                                                  replyMarkup: toSend.InlineKeyboardMarkup,
                                                  parseMode: toSend.ParseMode,
                                                  toLog: toLog,
                                                  replyToMsgId: toSend.replyToMsgId);
            }

            if (!string.IsNullOrEmpty(toSend.Text))
            {
                return await SendTextMessageAsync(chatId: chatId,
                                                  text: toSend.Text,
                                                  msgThreadId: toSend.msgThreadId,
                                                  parseMode: toSend.ParseMode,
                                                  toLog: toLog,
                                                  replyToMsgId: toSend.replyToMsgId);
            }
            return null;
        }

        public async void SendAnswerCallback(long userId, int messageToAnswerId, AnswerCallback toSend, bool toLog = true)
            => await EditMessageTextAsync(userId, messageToAnswerId, toSend.Text, toSend.InlineKeyboardMarkup, parseMode: toSend.ParseMode, toLog: toLog);
    }
}
