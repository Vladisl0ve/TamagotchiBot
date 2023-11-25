using System.Globalization;
using Extensions = TamagotchiBot.UserExtensions.Extensions;
using TamagotchiBot.Services.Interfaces;
using Telegram.Bot.Types;
using static TamagotchiBot.Resources.Resources;
using TamagotchiBot.Models.Answers;
using Telegram.Bot.Types.ReplyMarkups;
using static TamagotchiBot.UserExtensions.CallbackButtons;
using Serilog;
using System.Threading.Tasks;
using System.Web;
using System.Linq;
using System.Collections.Generic;
using TamagotchiBot.Models;
using System;

namespace TamagotchiBot.Controllers
{
    public class MultiplayerController
    {
        private IApplicationServices _appServices;
        private readonly Message _message = null;
        private readonly CallbackQuery _callback = null;
        private readonly long _userId;
        private readonly long _chatId;
        private readonly CultureInfo _userCulture;

        private readonly string _chatName;
        private readonly string _userName;
        private readonly string _userLogInfo;

        public MultiplayerController(IApplicationServices services, Message message = null, CallbackQuery callback = null)
        {
            _callback = callback;
            _message = message;
            _userId = callback?.From.Id ?? message.From.Id;
            _chatId = callback?.Message?.Chat?.Id ?? message.Chat.Id;
            _appServices = services;

            _userName = message?.From?.FirstName ?? callback?.From?.FirstName;
            _chatName = message?.Chat?.Title ?? callback?.Message.Chat.Title;
            _userLogInfo = Extensions.GetLogUser(_appServices.UserService.Get(_userId) ?? new Models.Mongo.User() { UserId = _userId });

            Culture = _userCulture = new CultureInfo(_appServices.UserService.Get(_userId)?.Culture ?? "ru");
        }
        public Task CommandHandler(string customText = null)
        {
            string textReceived = customText ?? _message.Text;
            if (textReceived == null)
                return Task.CompletedTask;

            textReceived = textReceived.ToLower();
            if (textReceived.Contains('@'))
                textReceived = textReceived.Split('@').First();

            var petDB = _appServices.PetService.Get(_userId);
            var userDB = _appServices.UserService.Get(_userId);

            if (textReceived == "/show_pet")
            {
                ShowPetMP(petDB, userDB);
            }
            if (textReceived == "/start_duel")
            {
                StartDuel(petDB, userDB);
            }

            return Task.CompletedTask;

            async void StartDuel(Models.Mongo.Pet petDB, Models.Mongo.User userDB)
            {
                var customCallback = new DuelMuliplayerCommand().StartDuelMultiplayerButton;
                customCallback.CallbackData += $"_{userDB.UserId}_{_message.MessageId}";
                AnswerMessage answerMessage = new AnswerMessage()
                {
                    InlineKeyboardMarkup = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
                    {
                        customCallback
                    }),
                    ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                    Text = "🥊" + Extensions.GetPersonalLink(_userId, _userName) + ": GO PIZDITSA!"
                };

                var metaUser = _appServices.MetaUserService.Get(_userId);
                if (metaUser?.MsgDuelId > 0)
                {
                    await _appServices.BotControlService.DeleteMessageAsync(metaUser.ChatDuelId, metaUser.MsgDuelId, false);
                    await _appServices.BotControlService.DeleteMessageAsync(metaUser.ChatDuelId, metaUser.MsgCreatorDuelId, false);
                }

                var sentMsg = await _appServices.BotControlService.SendAnswerMessageGroupAsync(answerMessage, _chatId, false);
                _appServices.MetaUserService.UpdateMsgDuelId(_userId, sentMsg?.MessageId ?? -1);
                _appServices.MetaUserService.UpdateChatDuelId(_userId, _chatId);
                _appServices.MetaUserService.UpdateMsgCreatorDuelId(_userId, _message?.MessageId ?? -1);
                _appServices.MetaUserService.UpdateDuelStartTime(_userId, DateTime.UtcNow);
                Log.Debug($"MP: started duel by {_userLogInfo}");
            }
            async void ShowPetMP(Models.Mongo.Pet petDB, Models.Mongo.User userDB)
            {
                var botUsername = (await _appServices.SInfoService.GetBotUserInfo()).Username;
                var encodedPetName = HttpUtility.HtmlEncode(petDB.Name);
                string toSendText = string.Format(MultiplayerShowPet,
                                                  encodedPetName,
                                                  petDB.HP,
                                                  petDB.Satiety,
                                                  petDB.Hygiene,
                                                  petDB.Fatigue,
                                                  petDB.Joy,
                                                  petDB.Level,
                                                  userDB.Gold,
                                                  "`personalLink`");

                toSendText = toSendText.Replace("`personalLink`", Extensions.GetPersonalLink(_userId, _userName));

                AnswerMessage answerMessage = new AnswerMessage()
                {
                    InlineKeyboardMarkup = new (
                        InlineKeyboardButton.WithUrl(
                            new InviteMuliplayerCommand().InviteReferalMultiplayerButton(userDB.FirstName).Text,
                            Extensions.GetReferalLink(_userId, botUsername)
                            )),
                    ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                    Text = toSendText
                };

                await _appServices.BotControlService.SendAnswerMessageGroupAsync(answerMessage, _chatId, false);
                Log.Debug($"MP: called showPet by {_userLogInfo}");
            }
        }
        public async Task CallbackHandler()
        {
            var userDb = _appServices.UserService.Get(_userId);
            var petDb = _appServices.PetService.Get(_userId);

            if (userDb == null || petDb == null)
                return;

            if (_callback.Data.Contains(new DuelMuliplayerCommand().StartDuelMultiplayerButton.CallbackData))
            {
                await AcceptDuel();
            }

            async Task AcceptDuel()
            {
                var splittedData = _callback.Data.Split("_").ToList();
                if (splittedData.Count != 3)
                    return;

                var duelCreatorStr = splittedData[1];
                var duelMsgStr = splittedData[2];

                if (!int.TryParse(duelCreatorStr, out var duelCreatorId))
                    return;
                if (!int.TryParse(duelMsgStr, out int duelMsgId))
                    return;

                var metaUserDBCreator = _appServices.MetaUserService.Get(duelCreatorId);
                await _appServices.BotControlService.DeleteMessageAsync(_chatId, metaUserDBCreator?.MsgDuelId ?? -1);
                await _appServices.BotControlService.DeleteMessageAsync(_chatId, duelMsgId);
                _appServices.MetaUserService.UpdateMsgDuelId(duelCreatorId, -1);
                _appServices.MetaUserService.UpdateChatDuelId(duelCreatorId, -1);

                var personalLinkCreatorDuel = Extensions.GetPersonalLink(duelCreatorId, _appServices.UserService.Get(duelCreatorId)?.FirstName ?? "🌝");
                AnswerMessage answerMessage = new AnswerMessage()
                {
                    ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                    Text = $"{Extensions.GetPersonalLink(_userId, _userName)} started duel with {personalLinkCreatorDuel}"
                };

                await _appServices.BotControlService.SendAnswerMessageGroupAsync(answerMessage, _chatId, false);
                Log.Debug($"MP: duel accepted by {_userLogInfo}");
                return;
            }
        }

        public async void SendInviteForUnregistered()
        {
            string toSendText = string.Format(InviteGlobalMultiplayerText, "`personalLink`");
            toSendText = HttpUtility.HtmlEncode(toSendText);
            toSendText = toSendText.Replace("`personalLink`", Extensions.GetPersonalLink(_userId, _userName));
            var botUsername = (await _appServices.SInfoService.GetBotUserInfo()).Username;

            AnswerMessage answerMessage = new AnswerMessage()
            {
                InlineKeyboardMarkup = new ( InlineKeyboardButton.WithUrl(new InviteMuliplayerCommand().InviteGlobalMultiplayerButton.Text, $"https://t.me/{botUsername}")),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                Text = toSendText
            };

            await _appServices.BotControlService.SendAnswerMessageGroupAsync(answerMessage, _chatId, false);
            Log.Debug($"MP: called SendInviteForUnregistered by unregistered ID: {_userId}");
        }
        public async void SendWelcomeMessageOnStart()
        {
            var encodedChatName = HttpUtility.HtmlEncode(_chatName);
            string toSendText = string.Format(ShowWelcomeMessageMultiplayer, encodedChatName);
            var botUsername = (await _appServices.SInfoService.GetBotUserInfo()).Username;

            AnswerMessage answerMessage = new AnswerMessage()
            {
                InlineKeyboardMarkup = new ( InlineKeyboardButton.WithUrl(new InviteMuliplayerCommand().InviteGlobalMultiplayerButton.Text, Extensions.GetReferalLink(_userId, botUsername))),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                Text = toSendText
            };

            await _appServices.BotControlService.SendAnswerMessageGroupAsync(answerMessage, _chatId, false);
            Log.Debug($"MP: called SendWelcomeMessageOnStart, invited by ID: {_userId}");
        }
    }
}
