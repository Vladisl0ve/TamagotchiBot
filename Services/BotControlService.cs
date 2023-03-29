﻿using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TamagotchiBot.Models.Answers;
using TamagotchiBot.Models.Mongo;
using TamagotchiBot.Services.Mongo;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Formats.Asn1.AsnWriter;
using static TamagotchiBot.UserExtensions.Constants;

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
                Log.Warning("There is no user with id:" + userId);


            try
            {
                await _botClient.SendTextMessageAsync(userId,
                                     text,
                                     replyMarkup: replyMarkup,
                                     cancellationToken: cancellationToken);

                Log.Information($"Message sent to @{userDB?.Username ?? userDB?.FirstName ?? userId.ToString()}: {text.Replace("\r\n", " ")}");
            }
            catch (ApiRequestException ex)
            {
                Log.Error($"{ex.ErrorCode}: {ex.Message}, user: {userDB?.Username ?? userDB?.FirstName ?? userId.ToString()}");
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}, user: {userDB?.Username ?? userDB?.FirstName ?? userId.ToString()}");
            }
        }

        public async void SendStickerAsync(long userId, string stickerId, CancellationToken cancellationToken = default)
        {
            var userDB = _userService.Get(userId);

            if (userDB == null)
                Log.Warning("There is no user with id:" + userId);

            try
            {
                await _botClient.SendStickerAsync(userId,
                                     stickerId,
                                     cancellationToken: cancellationToken);

                Log.Information("Sticker sent for @" + userDB?.Username ?? userDB?.FirstName ?? userId.ToString());
            }
            catch (ApiRequestException ex)
            {
                Log.Error($"{ex.ErrorCode}: {ex.Message}, user: {userDB?.Username ?? userDB?.FirstName ?? userId.ToString()}");
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}, user: {userDB?.Username ?? userDB?.FirstName ?? userId.ToString()}");
            }
        }

        public async void EditMessageTextAsync(long userId, int messageId, string text, InlineKeyboardMarkup replyMarkup = default, CancellationToken cancellationToken = default)
        {
            var userDB = _userService.Get(userId);

            if (userDB == null)
                Log.Warning("There is no user with id:" + userId);

            try
            {
                await _botClient.EditMessageTextAsync(userId,
                                               messageId,
                                               text,
                                               replyMarkup: replyMarkup,
                                               cancellationToken: cancellationToken);

                Log.Information($"Message edited for @{userDB?.Username ?? userDB?.FirstName ?? userId.ToString()}: {text.Replace("\r\n", " ")}");
            }
            catch (ApiRequestException ex)
            {
                Log.Error($"{ex.ErrorCode}: {ex.Message}, user: {userDB?.Username ?? userDB?.FirstName ?? userId.ToString()}");
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}, user: {userDB?.Username ?? userDB?.FirstName ?? userId.ToString()}");
            }
        }
        public async void EditMessageReplyMarkupAsync(ChatId chatId, long userId, int messageId, InlineKeyboardMarkup replyMarkup = default, CancellationToken cancellationToken = default)
        {
            var userDB = _userService.Get(userId);

            if (userDB == null)
                Log.Warning("There is no user with id:" + userId);

            try
            {
                await _botClient.EditMessageReplyMarkupAsync(chatId, messageId, replyMarkup: replyMarkup, cancellationToken: cancellationToken);

                Log.Information("Message reply edited for @" + userDB.Username ?? userDB.FirstName ?? userId.ToString());
            }
            catch (ApiRequestException ex)
            {
                Log.Error($"{ex.ErrorCode}: {ex.Message}, user: {userDB?.Username ?? userDB?.FirstName ?? userId.ToString()}");
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}, user: {userDB?.Username ?? userDB?.FirstName ?? userId.ToString()}");
            }
        }

        public async void AnswerCallbackQueryAsync(string callbackQueryId, long userId, string text = default, bool showAlert = false, CancellationToken cancellationToken = default)
        {
            var userDB = _userService.Get(userId);

            if (userDB == null)
                Log.Warning("There is no user with id:" + userId);

            try
            {
                await _botClient.AnswerCallbackQueryAsync(callbackQueryId,
                                               text: text,
                                               showAlert: showAlert,
                                               cancellationToken: cancellationToken);

                Log.Information($"Answered callback for @{userDB?.Username ?? userDB?.FirstName ?? userId.ToString()}: {text.Replace("\r\n", " ")}");
            }
            catch (ApiRequestException ex)
            {
                Log.Error($"{ex.ErrorCode}: {ex.Message}, user: {userDB?.Username ?? userDB?.FirstName ?? userId.ToString()}");
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}, user: {userDB?.Username ?? userDB?.FirstName ?? userId.ToString()}");
            }

        }

        public async void SetMyCommandsAsync(long userId, IEnumerable<BotCommand> commands, BotCommandScope scope = default, CancellationToken cancellationToken = default)
        {
            var userDB = _userService.Get(userId);

            if (userDB == null)
                Log.Warning("There is no user with id:" + userId);

            try
            {
                await _botClient.SetMyCommandsAsync(commands,
                                                    scope: scope,
                                                    cancellationToken: cancellationToken);
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

        public async void SendChatActionAsync(ChatId chatId, ChatAction chatAction, CancellationToken cancellationToken = default)
        {
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
