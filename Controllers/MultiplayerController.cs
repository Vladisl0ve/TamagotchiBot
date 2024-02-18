using System.Globalization;
using TamagotchiBot.Services.Interfaces;
using Telegram.Bot.Types;
using TamagotchiBot.Models.Answers;
using Telegram.Bot.Types.ReplyMarkups;
using Serilog;
using System.Threading.Tasks;
using System.Web;
using System.Linq;
using System.Collections.Generic;
using TamagotchiBot.Models;
using System;
using TamagotchiBot.Models.Mongo;
using TamagotchiBot.UserExtensions;
using System.Text;
using Extensions = TamagotchiBot.UserExtensions.Extensions;
using User = TamagotchiBot.Models.Mongo.User;
using static TamagotchiBot.Resources.Resources;
using static TamagotchiBot.UserExtensions.CallbackButtons;
using static TamagotchiBot.UserExtensions.Constants;

namespace TamagotchiBot.Controllers
{
    public class MultiplayerController
    {
        private readonly IApplicationServices _appServices;
        private readonly Message _message = null;
        private readonly CallbackQuery _callback = null;
        private readonly long _userId;
        private readonly long _chatId;
        private readonly int? _msgThreadId;
        private readonly CultureInfo _userCulture;
        private readonly PetType _userPetType;

        private readonly string _chatName;
        private readonly string _userName;
        private readonly string _userLogInfo;
        private readonly string _userPetEmoji;

        public MultiplayerController(IApplicationServices services, Message message = null, CallbackQuery callback = null)
        {
            _callback = callback;
            _message = message;
            _userId = callback?.From.Id ?? message.From.Id;
            _chatId = callback?.Message?.Chat?.Id ?? message.Chat.Id;
            _msgThreadId = callback?.Message?.MessageThreadId ?? message?.MessageThreadId;

            _appServices = services;

            _userName = message?.From?.FirstName ?? callback?.From?.FirstName;
            _chatName = message?.Chat?.Title ?? callback?.Message.Chat.Title;
            _userLogInfo = Extensions.GetLogUser(_appServices.UserService.Get(_userId) ?? new User() { UserId = _userId });

            _userCulture = new CultureInfo(_appServices.UserService.Get(_userId)?.Culture ?? "ru");
            _userPetType = Extensions.GetEnumPetType(_appServices.PetService.Get(_userId)?.Type ?? -1);
            _userPetEmoji = Extensions.GetTypeEmoji(_userPetType);
        }
        public async Task CommandHandler(string botUsername, string customText = null)
        {
            string textReceived = customText ?? _message.Text;
            if (textReceived == null)
                return;

            textReceived = textReceived.ToLower();
            if (textReceived.Contains($"@{botUsername}"))
                textReceived = textReceived.Split($"@{botUsername}").FirstOrDefault();

            if (textReceived == null)
                return;

            if (textReceived.FirstOrDefault() != '/' && !IsMessageHasMentions())
                return;

            var petDB = _appServices.PetService.Get(_userId);
            var userDB = _appServices.UserService.Get(_userId);

            if (textReceived.FirstOrDefault() == '/')
                textReceived = textReceived.Remove(0, 1);

            if (textReceived == Constants.CommandsMP.ShowPetCommand)
            {
                if (!IsUserAndPetRegisteredChecking(petDB, userDB))
                {
                    await SendInviteForUnregistered();
                    return;
                }

                await ShowPetMP(petDB, userDB);
                return;
            }

            if (textReceived == Constants.CommandsMP.StartDuelCommand)
            {
                if (!IsUserAndPetRegisteredChecking(petDB, userDB))
                {
                    await SendInviteForUnregistered();
                    return;
                }

                await StartDuel(petDB, userDB);
                return;
            }

            if (textReceived == Constants.CommandsMP.ShowChatRanksMPCommand)
            {
                if (!IsUserAndPetRegisteredChecking(petDB, userDB))
                {
                    await SendInviteForUnregistered();
                    return;
                }

                await ShowRanksMP(userDB);
                return;
            }

            if (textReceived == Constants.CommandsMP.FeedMPCommand)
            {
                if (!IsUserAndPetRegisteredChecking(petDB, userDB))
                {
                    await SendInviteForUnregistered();
                    return;
                }

                await SendFeedMPMessage(userDB);
                return;
            }

            if (IsMessageHasMentions())
            {
                await FeedByMentionPetMP(petDB, userDB);
            }
        }
        public async Task CallbackHandler()
        {
            var userDB = _appServices.UserService.Get(_userId);
            var petDB = _appServices.PetService.Get(_userId);

            if (userDB == null || petDB == null || petDB.IsGone || petDB.HP <= 0)
                return;

            if (_callback.Data.Contains(DuelMuliplayerCommand.StartDuelMultiplayerButton(_userCulture).CallbackData))
            {
                await AcceptDuel();
            }

            if (_callback.Data.Contains(RanksMultiplayerCommand.ShowChatRanksMP(_userCulture).CallbackData))
            {
                await ShowRanks();
            }

            if (_callback.Data.Contains(RanksMultiplayerCommand.ShowGlobalRanksMP(_userCulture).CallbackData))
            {
                await ShowRanks(true);
            }

            async Task AcceptDuel()
            {
                var splittedData = _callback.Data.Split("_").ToList();
                if (splittedData.Count != 3)
                {
                    Log.Debug($"MP: called callback AcceptDuel, but callback data is wrong (data doesnt have 3 parts) by {_userLogInfo}");
                    return;
                }

                var duelCreatorStr = splittedData[1];
                var duelMsgStr = splittedData[2];

                if (!long.TryParse(duelCreatorStr, out var duelCreatorId))
                {
                    Log.Debug($"MP: called callback AcceptDuel, but callback data is wrong (duelCreatorId is not long) by {_userLogInfo}");
                    return;
                }
                if (!int.TryParse(duelMsgStr, out int duelMsgId))
                {
                    Log.Debug($"MP: called callback AcceptDuel, but callback data is wrong (duelMsgId is not long) by {_userLogInfo}");
                    return;
                }

                if (petDB?.HP < Constants.Costs.DuelHP)
                {
                    string notEnoughHPTextCallback = string.Format(nameof(NotEnoughHPForDuelCallback).UseCulture(_userCulture), Constants.Costs.DuelHP, petDB?.HP ?? 0);
                    await _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, notEnoughHPTextCallback, true);
                    Log.Debug($"MP: called callback AcceptDuel, but not enough HP by {_userLogInfo}");
                    return;
                }

                if (_userId == duelCreatorId)
                {
                    string cannotYourselfText = string.Format(nameof(DuelMPErrorYourself).UseCulture(_userCulture));
                    await _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, cannotYourselfText, true);
                    Log.Debug($"MP: called callback AcceptDuel, but can not accept own duel by {_userLogInfo}");
                    return;
                }

                if (userDB.Gold < Constants.Costs.DuelGold)
                {
                    string notEnoughGoldTextCallback = string.Format(nameof(NotEnoughGoldForDuelCallback).UseCulture(_userCulture), userDB.Gold, Constants.Costs.DuelGold);
                    await _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, notEnoughGoldTextCallback, true);
                    Log.Debug($"MP: called callback AcceptDuel, but not enough gold by {_userLogInfo}");
                    return;
                }

                var metaUserDB = _appServices.MetaUserService.Get(_userId);
                var timeToWaitNextDuel = metaUserDB?.NextPossibleDuelTime - DateTime.UtcNow;
                if (timeToWaitNextDuel != null && timeToWaitNextDuel > TimeSpan.Zero)
                {
                    var timeToWaitString = new DateTime(timeToWaitNextDuel.GetValueOrDefault().Ticks, DateTimeKind.Utc).ToString("HH:mm:ss");
                    string waitForDuelText = string.Format(nameof(DuelMPCooldownCallback).UseCulture(_userCulture), timeToWaitString);
                    await _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, waitForDuelText, true);
                    Log.Debug($"MP: called callback AcceptDuel, but has to wait {timeToWaitString} by {_userLogInfo}");
                    return;
                }

                var metaUserDBCreator = _appServices.MetaUserService.Get(duelCreatorId);
                await _appServices.BotControlService.DeleteMessageAsync(_chatId, metaUserDBCreator?.MsgDuelId ?? -1);
                await _appServices.BotControlService.DeleteMessageAsync(_chatId, duelMsgId);
                _appServices.MetaUserService.UpdateMsgDuelId(duelCreatorId, -1);
                _appServices.MetaUserService.UpdateChatDuelId(duelCreatorId, -1);
                _appServices.MetaUserService.UpdateMsgCreatorDuelId(duelCreatorId, -1);
                _appServices.UserService.UpdateGold(_userId, userDB.Gold - Constants.Costs.DuelGold);

                var audCreator = _appServices.AllUsersDataService.Get(duelCreatorId);
                audCreator.DuelsStartedCounter++;
                _appServices.AllUsersDataService.Update(audCreator);

                var audAccepter = _appServices.AllUsersDataService.Get(_userId);
                audAccepter.DuelsAcceptedCounter++;
                _appServices.AllUsersDataService.Update(audAccepter);

                var petAttacker = _appServices.PetService.Get(_userId);
                var petDefender = _appServices.PetService.Get(duelCreatorId);

                var personalLinkCreatorDuel = Extensions.GetPersonalLink(duelCreatorId, _appServices.UserService.Get(duelCreatorId)?.FirstName ?? "🌝");
                var personalLinkDuelAccepted = Extensions.GetPersonalLink(_userId, _userName);
                var petAttackerName = "<b>" + HttpUtility.HtmlEncode(petAttacker?.Name ?? "attacker") + "</b>";
                var petDefenderName = "<b>" + HttpUtility.HtmlEncode(petDefender?.Name ?? "defender") + "</b>";
                var petDefenderEmoji = Extensions.GetTypeEmoji(petDefender?.Type ?? -1);
                var petAttackerEmoji = Extensions.GetTypeEmoji(petAttacker?.Type ?? -1);

                var userDBCreator = _appServices.UserService.Get(duelCreatorId);
                AnswerMessage answerMessage = new AnswerMessage()
                {
                    StickerId = Constants.StickersId.MPDuelStarted,
                    ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                    Text = string.Format(
                        nameof(DuelMPFighting1).UseCulture(_userCulture),
                        personalLinkCreatorDuel,
                        personalLinkDuelAccepted,
                        petDefenderName,
                        petAttackerName,
                        petDefenderEmoji,
                        petAttackerEmoji),
                    msgThreadId = _msgThreadId
                };

                Log.Debug($"MP: duel accepted by {_userLogInfo}");
                var fightingMsg = await _appServices.BotControlService.SendAnswerMessageGroupAsync(answerMessage, _chatId, false);
                if (fightingMsg == null)
                {
                    Log.Fatal("ERROR ON FIGHTING_MP #1");
                    return;
                }

                await Task.Delay(3000);
                var fight2 = string.Format(
                    nameof(DuelMPFighting2).UseCulture(_userCulture),
                    personalLinkCreatorDuel,
                    personalLinkDuelAccepted,
                    petDefenderName,
                    petAttackerName,
                    petDefenderEmoji,
                    petAttackerEmoji);
                await _appServices.BotControlService.EditMessageTextAsync(_chatId, fightingMsg.MessageId, fight2, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);

                await Task.Delay(2000);
                var fight3 = string.Format(
                    nameof(DuelMPFighting3).UseCulture(_userCulture),
                    personalLinkCreatorDuel,
                    personalLinkDuelAccepted,
                    petDefenderName,
                    petAttackerName,
                    petDefenderEmoji,
                    petAttackerEmoji);
                await _appServices.BotControlService.EditMessageTextAsync(_chatId, fightingMsg.MessageId, fight3, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);

                await Task.Delay(2000);
                var fight4 = string.Format(nameof(DuelMPFighting4).UseCulture(_userCulture),
                    personalLinkCreatorDuel,
                    personalLinkDuelAccepted,
                    petDefenderName,
                    petAttackerName,
                    petDefenderEmoji,
                    petAttackerEmoji);
                await _appServices.BotControlService.EditMessageTextAsync(_chatId, fightingMsg.MessageId, fight4, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);

                await Task.Delay(2000);
                var random = new Random();
                bool isDefenderWin = random.Next(2) == 1;
                string fightingEndExplanation = isDefenderWin
                    ? RandomDuelEndExplanation(petDefenderName, petAttackerName, petDefenderEmoji, petAttackerEmoji)
                    : RandomDuelEndExplanation(petAttackerName, petDefenderName, petAttackerEmoji, petDefenderEmoji);
                string ownerWinnerName = isDefenderWin ? personalLinkCreatorDuel : personalLinkDuelAccepted;
                if (isDefenderWin)
                {
                    var audDefender = _appServices.AllUsersDataService.Get(duelCreatorId);
                    audDefender.DuelsWinCounter++;
                    _appServices.AllUsersDataService.Update(audDefender);

                    _appServices.ChatsMPService.AddDuelResult(_chatId, new DuelResultModel()
                    {
                        AttackerUserId = _userId,
                        DefenderUserId = duelCreatorId,
                        Revision = DateTime.UtcNow,
                        WinnerUserId = duelCreatorId
                    });

                    _appServices.UserService.UpdateGold(userDBCreator.UserId, userDBCreator.Gold + Constants.Rewards.WonDuel);
                    _appServices.MetaUserService.UpdateNextPossibleDuelTime(_userId, DateTime.UtcNow + Constants.TimesToWait.DuelCDToWait);
                }
                else
                {
                    var audAttacker = _appServices.AllUsersDataService.Get(_userId);
                    audAttacker.DuelsWinCounter++;
                    _appServices.AllUsersDataService.Update(audAttacker);

                    _appServices.ChatsMPService.AddDuelResult(_chatId, new DuelResultModel()
                    {
                        AttackerUserId = _userId,
                        DefenderUserId = duelCreatorId,
                        Revision = DateTime.UtcNow,
                        WinnerUserId = _userId
                    });

                    _appServices.UserService.UpdateGold(_userId, userDB.Gold + Constants.Rewards.WonDuel);
                    _appServices.MetaUserService.UpdateNextPossibleDuelTime(userDBCreator.UserId, DateTime.UtcNow + Constants.TimesToWait.DuelCDToWait);
                }

                var fightEnd = string.Format(
                    nameof(DuelMPFightingEnd).UseCulture(_userCulture),
                    personalLinkCreatorDuel,
                    personalLinkDuelAccepted,
                    ownerWinnerName,
                    fightingEndExplanation,
                    Constants.Rewards.WonDuel);
                await _appServices.BotControlService.EditMessageTextAsync(_chatId, fightingMsg.MessageId, fightEnd, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
            }
            async Task ShowRanks(bool isGlobal = false)
            {
                string msgSenderId = _callback.Data.Split("_").LastOrDefault();
                if (msgSenderId == null || msgSenderId != _userId.ToString())
                    return;

                var botUsername = (await _appServices.SInfoService.GetBotUserInfo()).Username;
                string toShowText = isGlobal ? GetGlobalRanks() : GetChatRanks();
                var replyMarkupToShow = new InlineKeyboardMarkup(
                    new List<List<InlineKeyboardButton>>()
                    {
                        new List<InlineKeyboardButton>()
                        {
                            InlineKeyboardButton.WithCallbackData(RanksMultiplayerCommand.ShowChatRanksMP(_userCulture).Text,  RanksMultiplayerCommand.ShowChatRanksMP(_userCulture).CallbackData + $"_{_userId}"),
                            InlineKeyboardButton.WithCallbackData(RanksMultiplayerCommand.ShowGlobalRanksMP(_userCulture).Text,  RanksMultiplayerCommand.ShowGlobalRanksMP(_userCulture).CallbackData + $"_{_userId}"),
                        },
                        new List<InlineKeyboardButton>()
                        {
                            InlineKeyboardButton.WithUrl(
                                InviteMuliplayerCommand.InviteReferalMultiplayerButton(userDB.FirstName, _userCulture).Text,
                                Extensions.GetReferalLink(_userId, botUsername)
                                )
                        }
                    }
                    );

                await _appServices.BotControlService.EditMessageTextAsync(_chatId, _callback.Message.MessageId,
                                                                          toShowText,
                                                                          replyMarkup: replyMarkupToShow,
                                                                          parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);

            }
        }
        private string RandomDuelEndExplanation(string winnerName, string loserName, string emojiWinner, string emojiLoser)
        {
            int rand = new Random().Next(11);
            try
            {
                return rand switch
                {
                    0 => string.Format(nameof(DuelMPFightingEnding1).UseCulture(_userCulture),
                    winnerName,
                    loserName,
                    emojiWinner,
                    emojiLoser),
                    1 => string.Format(nameof(DuelMPFightingEnding2).UseCulture(_userCulture),
                    loserName,
                    winnerName,
                    emojiLoser,
                    emojiWinner),
                    2 => string.Format(nameof(DuelMPFightingEnding3).UseCulture(_userCulture),
                    winnerName,
                    loserName,
                    emojiWinner,
                    emojiLoser),
                    3 => string.Format(nameof(DuelMPFightingEnding4).UseCulture(_userCulture),
                    winnerName,
                    loserName,
                    emojiWinner,
                    emojiLoser),
                    4 => string.Format(nameof(DuelMPFightingEnding5).UseCulture(_userCulture),
                    winnerName,
                    loserName,
                    emojiWinner,
                    emojiLoser),
                    5 => string.Format(nameof(DuelMPFightingEnding6).UseCulture(_userCulture),
                    winnerName,
                    loserName,
                    emojiWinner,
                    emojiLoser),
                    6 => string.Format(nameof(DuelMPFightingEnding7).UseCulture(_userCulture),
                    winnerName,
                    loserName,
                    emojiWinner,
                    emojiLoser),
                    7 => string.Format(nameof(DuelMPFightingEnding8).UseCulture(_userCulture),
                    winnerName,
                    loserName,
                    emojiWinner,
                    emojiLoser),
                    8 => string.Format(nameof(DuelMPFightingEnding9).UseCulture(_userCulture),
                    winnerName,
                    loserName,
                    emojiWinner,
                    emojiLoser),
                    9 => string.Format(nameof(DuelMPFightingEnding10).UseCulture(_userCulture),
                    winnerName,
                    loserName,
                    emojiWinner,
                    emojiLoser),
                    _ => string.Format(nameof(DuelMPFightingEnding1).UseCulture(_userCulture),
                    winnerName,
                    loserName,
                    emojiWinner,
                    emojiLoser)
                };
            }
            catch (Exception ex)
            {
                Log.Fatal("Duel string proebal!", ex);
                return string.Format(nameof(DuelMPFightingEnding1).UseCulture(_userCulture),
                winnerName,
                loserName,
                emojiWinner,
                emojiLoser);
            }
        }
        public async Task SendInviteForUnregistered()
        {
            string toSendText = string.Format(nameof(InviteGlobalMultiplayerText).UseCulture(_userCulture), "`personalLink`");
            toSendText = HttpUtility.HtmlEncode(toSendText);
            toSendText = toSendText.Replace("`personalLink`", Extensions.GetPersonalLink(_userId, _userName));
            var botUsername = (await _appServices.SInfoService.GetBotUserInfo()).Username;

            AnswerMessage answerMessage = new AnswerMessage()
            {
                InlineKeyboardMarkup = new ( InlineKeyboardButton.WithUrl(InviteMuliplayerCommand.InviteGlobalMultiplayerButton(_userCulture).Text, $"https://t.me/{botUsername}")),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                Text = toSendText,
                msgThreadId = _msgThreadId
            };

            await _appServices.BotControlService.SendAnswerMessageGroupAsync(answerMessage, _chatId, false);
            Log.Debug($"MP: called SendInviteForUnregistered by unregistered ID: {_userId}");
        }
        public async Task SendWelcomeMessageOnStart()
        {
            var encodedChatName = HttpUtility.HtmlEncode(_chatName);
            string toSendText = string.Format(nameof(ShowWelcomeMessageMultiplayer).UseCulture(_userCulture), encodedChatName);
            var botUsername = (await _appServices.SInfoService.GetBotUserInfo()).Username;

            AnswerMessage answerMessage = new AnswerMessage()
            {
                InlineKeyboardMarkup = new ( InlineKeyboardButton.WithUrl(InviteMuliplayerCommand.InviteGlobalMultiplayerButton(_userCulture).Text, Extensions.GetReferalLink(_userId, botUsername))),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                Text = toSendText,
                msgThreadId = _msgThreadId
            };

            await _appServices.BotControlService.SendAnswerMessageGroupAsync(answerMessage, _chatId, false);
            Log.Debug($"MP: called SendWelcomeMessageOnStart, invited by ID: {_userId}");
        }

        private async Task ShowRanksMP(User userDB)
        {
            string msgToShow = GetChatRanks();

            var botUsername = (await _appServices.SInfoService.GetBotUserInfo()).Username;
            AnswerMessage answerMessage = new AnswerMessage()
            {
                InlineKeyboardMarkup =
                new InlineKeyboardMarkup(
                    new List<List<InlineKeyboardButton>>()
                    {
                        new List<InlineKeyboardButton>()
                        {
                            InlineKeyboardButton.WithCallbackData(RanksMultiplayerCommand.ShowChatRanksMP(_userCulture).Text,  RanksMultiplayerCommand.ShowChatRanksMP(_userCulture).CallbackData + $"_{_userId}"),
                            InlineKeyboardButton.WithCallbackData(RanksMultiplayerCommand.ShowGlobalRanksMP(_userCulture).Text,  RanksMultiplayerCommand.ShowGlobalRanksMP(_userCulture).CallbackData + $"_{_userId}"),
                        },
                        new List<InlineKeyboardButton>()
                        {
                            InlineKeyboardButton.WithUrl(
                                InviteMuliplayerCommand.InviteReferalMultiplayerButton(userDB.FirstName, _userCulture).Text,
                                Extensions.GetReferalLink(_userId, botUsername)
                                )
                        }
                    }
                    ),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                Text = msgToShow,
                msgThreadId = _msgThreadId
            };

            await _appServices.BotControlService.SendAnswerMessageGroupAsync(answerMessage, _chatId, false);
            Log.Debug($"MP: called showChatRanks by {_userLogInfo}");
        }
        private async Task StartDuel(Pet petDB, User userDB)
        {
            var personalLink = Extensions.GetPersonalLink(_userId, _userName);
            if (userDB?.Gold < Constants.Costs.DuelGold)
            {
                AnswerMessage notEnoughGoldMsg = new AnswerMessage()
                {
                    ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                    Text = string.Format(nameof(NotEnoughGoldForDuel).UseCulture(_userCulture), personalLink, userDB.Gold, Constants.Costs.DuelGold),
                    msgThreadId = _msgThreadId
                };
                await _appServices.BotControlService.SendAnswerMessageGroupAsync(notEnoughGoldMsg, _chatId, false);
                Log.Debug($"MP: called command StartDuel, but not enough gold by {_userLogInfo}");
                return;
            }

            if (petDB?.HP < Constants.Costs.DuelHP)
            {
                AnswerMessage notEnoughHPMsg = new AnswerMessage()
                {
                    ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                    Text = string.Format(nameof(NotEnoughHPForDuel).UseCulture(_userCulture), personalLink, Constants.Costs.DuelHP, petDB.HP),
                    msgThreadId = _msgThreadId
                };
                await _appServices.BotControlService.SendAnswerMessageGroupAsync(notEnoughHPMsg, _chatId, false);
                Log.Debug($"MP: called command StartDuel, but not enough HP by {_userLogInfo}");
                return;
            }

            var metaUserDB = _appServices.MetaUserService.Get(_userId);
            var timeToWaitNextDuel = metaUserDB?.NextPossibleDuelTime - DateTime.UtcNow;
            if (timeToWaitNextDuel != null && timeToWaitNextDuel > TimeSpan.Zero)
            {
                var waitTimeString = new DateTime(timeToWaitNextDuel.GetValueOrDefault().Ticks, DateTimeKind.Utc).ToString("HH:mm:ss");
                AnswerMessage waitForDuelText = new AnswerMessage()
                {
                    ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                    Text = string.Format(nameof(DuelMPCooldown).UseCulture(_userCulture), waitTimeString, personalLink),
                    msgThreadId = _msgThreadId
                };
                await _appServices.BotControlService.SendAnswerMessageGroupAsync(waitForDuelText, _chatId, false);
                Log.Debug($"MP: called command StartDuel, but have to wait {waitTimeString} by {_userLogInfo}");
                return;
            }

            var customCallback = DuelMuliplayerCommand.StartDuelMultiplayerButton(_userCulture);
            customCallback.CallbackData += $"_{userDB?.UserId}_{_message.MessageId}";
            AnswerMessage answerMessage = new AnswerMessage()
            {
                InlineKeyboardMarkup = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
                    {
                        customCallback
                    }),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                Text = string.Format(nameof(DuelMPStartCommand).UseCulture(_userCulture), personalLink, HttpUtility.HtmlEncode(petDB?.Name), Extensions.GetLongTypeEmoji(_userPetType, _userCulture)),
                msgThreadId = _msgThreadId
            };

            var metaUser = _appServices.MetaUserService.Get(_userId);
            if (metaUser?.MsgDuelId > 0)
            {
                await _appServices.BotControlService.DeleteMessageAsync(metaUser.ChatDuelId, metaUser.MsgDuelId, false);
                await _appServices.BotControlService.DeleteMessageAsync(metaUser.ChatDuelId, metaUser.MsgCreatorDuelId, false);
            }

            var sentMsg = await _appServices.BotControlService.SendAnswerMessageGroupAsync(answerMessage, _chatId, false);
            if (sentMsg == null)
            {
                Log.Fatal("ERROR ON StartDuel #1");
                return;
            }
            _appServices.UserService.UpdateGold(_userId, userDB.Gold - Constants.Costs.DuelGold);
            _appServices.MetaUserService.UpdateMsgDuelId(_userId, sentMsg.MessageId);
            _appServices.MetaUserService.UpdateChatDuelId(_userId, _chatId);
            _appServices.MetaUserService.UpdateMsgCreatorDuelId(_userId, _message?.MessageId ?? -1);
            _appServices.MetaUserService.UpdateDuelStartTime(_userId, DateTime.UtcNow);
            Log.Debug($"MP: started duel by {_userLogInfo}");
        }
        private async Task ShowPetMP(Pet petDB, User userDB)
        {
            var botUsername = (await _appServices.SInfoService.GetBotUserInfo()).Username;
            var encodedPetName = HttpUtility.HtmlEncode(petDB.Name);
            string toSendText = string.Format(nameof(MultiplayerShowPet).UseCulture(_userCulture),
                                                  encodedPetName,
                                                  petDB.HP,
                                                  petDB.Satiety,
                                                  petDB.Hygiene,
                                                  petDB.Fatigue,
                                                  petDB.Joy,
                                                  petDB.Level,
                                                  userDB.Gold,
                                                  "`personalLink`",
                                                  Extensions.GetLongTypeEmoji(_userPetType, _userCulture));

            toSendText = toSendText.Replace("`personalLink`", Extensions.GetPersonalLink(_userId, _userName));

            AnswerMessage answerMessage = new AnswerMessage()
            {
                InlineKeyboardMarkup = new (
                        InlineKeyboardButton.WithUrl(
                            InviteMuliplayerCommand.InviteReferalMultiplayerButton(userDB.FirstName, _userCulture).Text,
                            Extensions.GetReferalLink(_userId, botUsername)
                            )),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                Text = toSendText,
                msgThreadId = _msgThreadId
            };

            await _appServices.BotControlService.SendAnswerMessageGroupAsync(answerMessage, _chatId, false);
            Log.Debug($"MP: called showPet by {_userLogInfo}");
        }
        public async Task FeedByMentionPetMP(Pet petDB, User userDB)
        {
            bool isError = false;
            var metaUserDB = _appServices.MetaUserService.Get(_userId);

            if (!metaUserDB?.IsFeedingMPStarted ?? true)
            {
                Log.Debug($"MP: called command FeedByMentionPetMP, but feeding not started yet by {_userLogInfo}");
                return;
            }

            if (!await CheckAndSendOnFailIsEnoughGoldAtFeeder(userDB))
            {
                Log.Debug($"MP: called command FeedByMentionPetMP, but not enough gold by {_userLogInfo}");
                goto ending;
            }

            var msgEntity = _message.Entities?.FirstOrDefault();
            if (msgEntity == default)
            {
                isError = true;
                goto ending;
            }

            var userToFeedDB = msgEntity.Type switch
            {
                Telegram.Bot.Types.Enums.MessageEntityType.TextMention => GetUserFromEntity(msgEntity),
                _ => GetUserByUsername(_message.EntityValues.FirstOrDefault())
            };

            if (userToFeedDB == null)
            {
                isError = true;
                goto ending;
            }

            if (userToFeedDB.UserId == _userId)
            {
                await SendCanNotFeedOwnPetMessage();
                goto ending;
            }

            var petToFeedDB = _appServices.PetService.Get(userToFeedDB.UserId);
            if (petToFeedDB == null)
            {
                isError = true;
                goto ending;
            }

            var spentTimeByLastPetFeeding = DateTime.UtcNow - petToFeedDB.LastMPFedTime;
            if (spentTimeByLastPetFeeding < Constants.TimesToWait.FeedMPCDToWait)
            {
                string timeToWait = new DateTime((petToFeedDB.LastMPFedTime + Constants.TimesToWait.FeedMPCDToWait - DateTime.UtcNow).Ticks).ToString("HH:mm:ss");
                var persLink = Extensions.GetPersonalLink(_userId, _userName);
                var petName = HttpUtility.HtmlEncode(petToFeedDB.Name);
                petName = "<b>" + petName + "</b>";
                await _appServices.BotControlService.SendAnswerMessageGroupAsync(new AnswerMessage()
                {
                    Text = string.Format(nameof(NotEnoughTimeSpentFeedMP).UseCulture(_userCulture), persLink, petName, timeToWait),
                    replyToMsgId = _message.MessageId,
                    ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                    msgThreadId = _msgThreadId,
                },
                _chatId,
                false);
                goto ending;
            }

            _appServices.UserService.UpdateGold(_userId, userDB.Gold - Constants.Costs.FeedMP);
            _appServices.MetaUserService.UpdateLastMPFeedingTime(_userId, DateTime.UtcNow);
            _appServices.PetService.UpdateMPSatiety(petToFeedDB.UserId, petToFeedDB.MPSatiety + Constants.FoodFactors.MPFeedFactor);
            _appServices.PetService.UpdateLastMPFedTime(userToFeedDB.UserId, DateTime.UtcNow);

            await _appServices.BotControlService.SendAnswerMessageGroupAsync(new AnswerMessage()
            {
                Text = string.Format(
                    nameof(FeedMPEndSuccess).UseCulture(_userCulture),
                    HttpUtility.HtmlEncode(petToFeedDB.Name),
                    Constants.Costs.FeedMP,
                    Extensions.GetTypeEmoji(petToFeedDB.Type),
                    HttpUtility.HtmlEncode(petToFeedDB.Name)),
                replyToMsgId = _message.MessageId,
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                msgThreadId = _msgThreadId,
            },
            _chatId,
            false);

        ending:
            if (isError)
            {
                await _appServices.BotControlService.SendAnswerMessageGroupAsync(new AnswerMessage()
                {
                    Text = nameof(FeedMPNotFound).UseCulture(_userCulture),
                    replyToMsgId = _message?.MessageId,
                    msgThreadId = _msgThreadId
                },
                    _chatId,
                    false
                    );

                Log.Debug($"MP: called command FeedByMentionPetMP, but something went wrong by {_userLogInfo}");
            }
            _appServices.MetaUserService.UpdateIsFeedingMPStarted(_userId, false);
        }

        private bool IsUserAndPetRegisteredChecking(Pet petDB, Models.Mongo.User userDB)
        {
            if (userDB == null)
                return false;

            if (userDB.IsLanguageAskedOnCreate)
                return false;

            if (userDB.IsPetNameAskedOnCreate)
                return false;

            if (petDB == null)
                return false;

            if (petDB.IsGone)
                return false;

            if (petDB.HP <= 0)
                return false;

            return true;
        }
        private bool IsMessageHasMentions()
        {
            if (_message?.Entities == null)
                return false;

            return _message.Entities.Any(e => e.Type == Telegram.Bot.Types.Enums.MessageEntityType.TextMention)
                   || _message.Entities.Any(e => e.Type == Telegram.Bot.Types.Enums.MessageEntityType.Mention);
        }

        private async Task SendFeedMPMessage(Models.Mongo.User userDB)
        {
            if (!await CheckAndSendOnFailIsEnoughGoldAtFeeder(userDB))
                return;

            if (!await CheckAndSendOnFailSpentTimeByFeeder())
                return;

            var persLink = Extensions.GetPersonalLink(_userId, _userName);
            await _appServices.BotControlService.SendAnswerMessageGroupAsync(new AnswerMessage()
            {
                Text = string.Format(nameof(FeedMPStart).UseCulture(_userCulture), persLink),
                replyToMsgId = _message.MessageId,
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                msgThreadId = _msgThreadId
            },
            _chatId,
            false);
            _appServices.MetaUserService.UpdateIsFeedingMPStarted(_userId, true);
        }
        private async Task SendNotEnoughGoldForFeedMessage(Models.Mongo.User userDB)
        {
            var persLink = Extensions.GetPersonalLink(_userId, _userName);
            await _appServices.BotControlService.SendAnswerMessageGroupAsync(new AnswerMessage()
            {
                Text = string.Format(nameof(NotEnoughGoldForMPFeed).UseCulture(_userCulture), persLink, userDB.Gold, Constants.Costs.FeedMP),
                replyToMsgId = _message.MessageId,
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                msgThreadId = _msgThreadId
            },
            _chatId,
            false);
        }
        private async Task SendCanNotFeedOwnPetMessage()
        {
            await _appServices.BotControlService.SendAnswerMessageGroupAsync(new AnswerMessage()
            {
                Text = string.Format(nameof(FeedMPErrorFeedOwnPet).UseCulture(_userCulture)),
                replyToMsgId = _message.MessageId,
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                msgThreadId = _msgThreadId
            },
            _chatId,
            false);
        }

        private User GetUserFromEntity(MessageEntity entity)
        {
            if (entity == null)
                return null;

            if (entity.User == null)
                return null;

            return _appServices.UserService.Get(entity.User.Id);
        }
        private Models.Mongo.User GetUserByUsername(string username)
        {
            username = username.Replace("@", default);
            return _appServices.UserService.GetByUsername(username);
        }

        private async Task<bool> CheckAndSendOnFailSpentTimeByFeeder()
        {
            var metaUserFeeder = _appServices.MetaUserService.Get(_userId) ?? _appServices.MetaUserService.Create(new MetaUser() { UserId = _userId });
            if (metaUserFeeder == null)
            {
                return false;
            }
            var spentTimeByLastUserFeeding = DateTime.UtcNow - metaUserFeeder.LastMPFeedingTime;
            if (spentTimeByLastUserFeeding < Constants.TimesToWait.FeedMPCDToWait)
            {
                string timeToWait = new DateTime((metaUserFeeder.LastMPFeedingTime + Constants.TimesToWait.FeedMPCDToWait - DateTime.UtcNow).Ticks).ToString("HH:mm:ss");
                var persLink = Extensions.GetPersonalLink(_userId, _userName);
                await _appServices.BotControlService.SendAnswerMessageGroupAsync(new AnswerMessage()
                {
                    Text = string.Format(nameof(NotEnoughTimeSpentByLastMPFeed).UseCulture(_userCulture), persLink, timeToWait),
                    replyToMsgId = _message.MessageId,
                    ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                    msgThreadId = _msgThreadId
                },
                _chatId,
                false);
                return false;
            }

            return true;
        }
        private async Task<bool> CheckAndSendOnFailIsEnoughGoldAtFeeder(Models.Mongo.User userDB)
        {
            if (userDB.Gold < Constants.Costs.FeedMP)
            {
                await SendNotEnoughGoldForFeedMessage(userDB);
                return false;
            }

            return true;
        }
        private string GetChatRanks()
        {
            var duelResults = _appServices.ChatsMPService.Get(_chatId).DuelResults ?? new List<DuelResultModel>();
            var groupByWinner = duelResults.GroupBy(d => d.WinnerUserId);

            Dictionary<long, int> UserAndDuelWins = new Dictionary<long, int>();
            foreach (var groupResult in groupByWinner)
            {
                var userId = groupResult.Key;
                var wins = groupResult.Count();
                UserAndDuelWins.Add(userId, wins);
            }
            var sortedUserAndDuelWins = UserAndDuelWins.OrderByDescending(u => u.Value)
                                             .ToDictionary(p => p.Key, p => p.Value);

            var encodedChatName = "<i>" + HttpUtility.HtmlEncode(_chatName) + "</i>";
            StringBuilder msgToShow = new StringBuilder($"{string.Format(nameof(ShowChatRanksMPHeader).UseCulture(_userCulture), encodedChatName)}" +
                $"{Environment.NewLine}");
            int counter = 0;
            foreach (var userDuel in sortedUserAndDuelWins)
            {
                if (counter == 10)
                    break;

                var userTmpDB = _appServices.UserService.Get(userDuel.Key);
                if (userTmpDB == null)
                    continue;

                counter++;
                msgToShow.Append($"{Environment.NewLine}");
                if (counter == 1)
                {
                    msgToShow.Append($"🥇 {GetAlignedRank(userTmpDB.FirstName, userDuel.Value.ToString())}");
                    msgToShow.Append($"{Environment.NewLine}");
                    msgToShow.Append($"<code>----------</code>");
                }
                else if (counter == 2)
                    msgToShow.Append($"🥈 {GetAlignedRank(userTmpDB.FirstName, userDuel.Value.ToString())}");
                else if (counter == 3)
                    msgToShow.Append($"🥉 {GetAlignedRank(userTmpDB.FirstName, userDuel.Value.ToString())}");
                else
                    msgToShow.Append($"{counter}. {GetAlignedRank(userTmpDB.FirstName, userDuel.Value.ToString())}");
            }

            return msgToShow.ToString();
        }
        private string GetGlobalRanks()
        {
            var topPlayers = _appServices.AllUsersDataService.GetAll().OrderByDescending(aud => aud.DuelsWinCounter).ToList();

            StringBuilder msgToShow = new StringBuilder($"{nameof(ShowGlobalRanksMPHeader).UseCulture(_userCulture)}" +
                $"{Environment.NewLine}");
            int counter = 0;
            foreach (var aud in topPlayers)
            {
                if (counter == 10)
                    break;

                var userTmpDB = _appServices.UserService.Get(aud.UserId);
                if (userTmpDB == null)
                    continue;

                counter++;
                msgToShow.Append($"{Environment.NewLine}");
                if (counter == 1)
                {
                    msgToShow.Append($"🥇 {GetAlignedRank(userTmpDB.FirstName, aud.DuelsWinCounter.ToString())}");
                    msgToShow.Append($"{Environment.NewLine}");
                    msgToShow.Append($"<code>----------</code>");
                }
                else if (counter == 2)
                    msgToShow.Append($"🥈 {GetAlignedRank(userTmpDB.FirstName, aud.DuelsWinCounter.ToString())}");
                else if (counter == 3)
                    msgToShow.Append($"🥉 {GetAlignedRank(userTmpDB.FirstName, aud.DuelsWinCounter.ToString())}");
                else
                    msgToShow.Append($"{counter}. {GetAlignedRank(userTmpDB.FirstName, aud.DuelsWinCounter.ToString())}");
            }

            return msgToShow.ToString();
        }

        private string GetAlignedRank(string username, string duelResult)
        {
            const int MAX_SYMBOLS = 20;
            if (username.Length > MAX_SYMBOLS)
                username = string.Concat(username.AsSpan(0, MAX_SYMBOLS - 3), "...");

            if (username.Length < MAX_SYMBOLS)
            {
                while (username.Length < MAX_SYMBOLS)
                    username = string.Concat(username, " ");
            }

            username = "<code>" + username + "</code>";
            return $"{username} ⚔️{duelResult}";
        }
    }
}
