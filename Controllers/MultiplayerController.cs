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
using TamagotchiBot.Models.Mongo;
using TamagotchiBot.UserExtensions;

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
                var personalLink = Extensions.GetPersonalLink(_userId, _userName);
                if (userDB?.Gold < Constants.Costs.Duel)
                {
                    Culture = new CultureInfo(userDB?.Culture ?? "ru");
                    AnswerMessage notEnoughGoldMsg = new AnswerMessage()
                    {
                        ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                        Text = string.Format(NotEnoughGoldForDuel, personalLink, userDB?.Gold ?? 0, Constants.Costs.Duel)
                    };
                    await _appServices.BotControlService.SendAnswerMessageGroupAsync(notEnoughGoldMsg, _chatId, false);
                    return;
                }

                var metaUserDB = _appServices.MetaUserService.Get(_userId);
                var timeToWaitNextDuel = metaUserDB?.NextPossibleDuelTime - DateTime.UtcNow;
                if (timeToWaitNextDuel != null && timeToWaitNextDuel > TimeSpan.Zero)
                {
                    Culture = new CultureInfo(userDB?.Culture ?? "ru");
                    AnswerMessage waitForDuelText = new AnswerMessage()
                    {
                        ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                        Text = string.Format(DuelMPCooldown, new DateTime(timeToWaitNextDuel?.Ticks ?? 0).ToString("HH:mm:ss"), personalLink)
                    };
                    await _appServices.BotControlService.SendAnswerMessageGroupAsync(waitForDuelText, _chatId, false);
                    return;
                }

                Culture = new CultureInfo(userDB?.Culture ?? "ru");
                var customCallback = new DuelMuliplayerCommand().StartDuelMultiplayerButton;
                customCallback.CallbackData += $"_{userDB.UserId}_{_message.MessageId}";
                AnswerMessage answerMessage = new AnswerMessage()
                {
                    InlineKeyboardMarkup = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
                    {
                        customCallback
                    }),
                    ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                    Text = string.Format(DuelMPStartCommand, personalLink, petDB.Name)
                };

                var metaUser = _appServices.MetaUserService.Get(_userId);
                if (metaUser?.MsgDuelId > 0)
                {
                    await _appServices.BotControlService.DeleteMessageAsync(metaUser.ChatDuelId, metaUser.MsgDuelId, false);
                    await _appServices.BotControlService.DeleteMessageAsync(metaUser.ChatDuelId, metaUser.MsgCreatorDuelId, false);
                }

                var sentMsg = await _appServices.BotControlService.SendAnswerMessageGroupAsync(answerMessage, _chatId, false);
                _appServices.UserService.UpdateGold(_userId, userDB.Gold - Constants.Costs.Duel);
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
        public void CallbackHandler()
        {
            var userDB = _appServices.UserService.Get(_userId);
            var petDB = _appServices.PetService.Get(_userId);

            if (userDB == null || petDB == null)
                return;

            if (_callback.Data.Contains(new DuelMuliplayerCommand().StartDuelMultiplayerButton.CallbackData))
            {
                AcceptDuel();
            }

            async void AcceptDuel()
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

                if(_userId == duelCreatorId)
                {
                    Culture = new CultureInfo(userDB?.Culture ?? "ru");
                    string cannotYourselfText = string.Format(DuelMPErrorYourself);
                    _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, cannotYourselfText, true);
                    return;
                }

                if (userDB?.Gold < Constants.Costs.Duel)
                {
                    Culture = new CultureInfo(userDB?.Culture ?? "ru");
                    string notEnoughGoldTextCallback = string.Format(NotEnoughGoldForDuelCallback, userDB?.Gold ?? 0, Constants.Costs.Duel);
                    _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, notEnoughGoldTextCallback, true);
                    return;
                }

                var metaUserDB = _appServices.MetaUserService.Get(_userId);
                var timeToWaitNextDuel = metaUserDB?.NextPossibleDuelTime - DateTime.UtcNow;
                if (timeToWaitNextDuel != null && timeToWaitNextDuel > TimeSpan.Zero)
                {
                    Culture = new CultureInfo(userDB?.Culture ?? "ru");
                    string waitForDuelText = string.Format(DuelMPCooldownCallback, new DateTime(timeToWaitNextDuel?.Ticks ?? 0).ToString("HH:mm:ss"));
                    _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, waitForDuelText, true);
                    return;
                }

                var metaUserDBCreator = _appServices.MetaUserService.Get(duelCreatorId);
                await _appServices.BotControlService.DeleteMessageAsync(_chatId, metaUserDBCreator?.MsgDuelId ?? -1);
                await _appServices.BotControlService.DeleteMessageAsync(_chatId, duelMsgId);
                _appServices.MetaUserService.UpdateMsgDuelId(duelCreatorId, -1);
                _appServices.MetaUserService.UpdateChatDuelId(duelCreatorId, -1);
                _appServices.MetaUserService.UpdateMsgCreatorDuelId(duelCreatorId, -1);
                _appServices.UserService.UpdateGold(_userId, userDB.Gold - Constants.Costs.Duel);

                var petAttacker = _appServices.PetService.Get(_userId);
                var petDefender = _appServices.PetService.Get(duelCreatorId);

                var personalLinkCreatorDuel = Extensions.GetPersonalLink(duelCreatorId, _appServices.UserService.Get(duelCreatorId)?.FirstName ?? "🌝");
                var personalLinkDuelAccepted = Extensions.GetPersonalLink(_userId, _userName);
                var petAttackerName = "<b>" + HttpUtility.HtmlEncode(petAttacker?.Name ?? "attacker") + "</b>";
                var petDefenderName = "<b>" + HttpUtility.HtmlEncode(petDefender?.Name ?? "defender") + "</b>";

                var userDBCreator = _appServices.UserService.Get(duelCreatorId);
                Culture = new CultureInfo(userDBCreator?.Culture ?? "ru");
                AnswerMessage answerMessage = new AnswerMessage()
                {
                    StickerId = Constants.StickersId.MPDuelStarted,
                    ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                    Text = string.Format(DuelMPFighting1, personalLinkCreatorDuel, personalLinkDuelAccepted, petDefenderName, petAttackerName)
                };

                Log.Debug($"MP: duel accepted by {_userLogInfo}");
                Culture = new CultureInfo(userDBCreator?.Culture ?? "ru");
                var fightingMsg = await _appServices.BotControlService.SendAnswerMessageGroupAsync(answerMessage, _chatId, false);

                await Task.Delay(3000);
                Culture = new CultureInfo(userDBCreator?.Culture ?? "ru");
                var fight2 = string.Format(DuelMPFighting2, personalLinkCreatorDuel, personalLinkDuelAccepted, petDefenderName, petAttackerName);
                await _appServices.BotControlService.EditMessageTextAsync(_chatId, fightingMsg.MessageId, fight2, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);

                await Task.Delay(2000);
                Culture = new CultureInfo(userDBCreator?.Culture ?? "ru");
                var fight3 = string.Format(DuelMPFighting3, personalLinkCreatorDuel, personalLinkDuelAccepted, petDefenderName, petAttackerName);
                await _appServices.BotControlService.EditMessageTextAsync(_chatId, fightingMsg.MessageId, fight3, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);

                await Task.Delay(2000);
                Culture = new CultureInfo(userDBCreator?.Culture ?? "ru");
                var fight4 = string.Format(DuelMPFighting4, personalLinkCreatorDuel, personalLinkDuelAccepted, petDefenderName, petAttackerName);
                await _appServices.BotControlService.EditMessageTextAsync(_chatId, fightingMsg.MessageId, fight4, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);

                await Task.Delay(2000);
                var random = new Random();
                bool isDefenderWin = random.Next(2) == 1;
                string fightingEndExplanation = isDefenderWin
                    ? string.Format(DuelMPFightingEndingDefenderWin, petDefenderName, petAttackerName)
                    : string.Format(DuelMPFightingEndingAttackerWin, petDefenderName, petAttackerName);
                string ownerWinnerName = isDefenderWin ? personalLinkCreatorDuel : personalLinkDuelAccepted;
                if (isDefenderWin)
                {
                    _appServices.UserService.UpdateGold(userDBCreator.UserId, userDBCreator.Gold + Constants.Rewards.WonDuel);
                    _appServices.MetaUserService.UpdateNextPossibleDuelTime(_userId, DateTime.UtcNow + new Constants.TimesToWait().DuelCDToWait);
                }
                else
                {
                    _appServices.UserService.UpdateGold(_userId, userDB.Gold + Constants.Rewards.WonDuel);
                    _appServices.MetaUserService.UpdateNextPossibleDuelTime(userDBCreator.UserId, DateTime.UtcNow + new Constants.TimesToWait().DuelCDToWait);
                }

                var fightEnd = string.Format(DuelMPFightingEnd, personalLinkCreatorDuel, personalLinkDuelAccepted, ownerWinnerName, fightingEndExplanation, Constants.Rewards.WonDuel);
                await _appServices.BotControlService.EditMessageTextAsync(_chatId, fightingMsg.MessageId, fightEnd, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
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
