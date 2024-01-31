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
using User = TamagotchiBot.Models.Mongo.User;
using System.Text;

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

        private readonly string _chatName;
        private readonly string _userName;
        private readonly string _userLogInfo;

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
            _userLogInfo = Extensions.GetLogUser(_appServices.UserService.Get(_userId) ?? new Models.Mongo.User() { UserId = _userId });

            Culture = _userCulture = new CultureInfo(_appServices.UserService.Get(_userId)?.Culture ?? "ru");
        }
        public void CommandHandler(string botUsername, string customText = null)
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
                    SendInviteForUnregistered();
                    return;
                }

                ShowPetMP(petDB, userDB);
                return;
            }

            if (textReceived == Constants.CommandsMP.StartDuelCommand)
            {
                if (!IsUserAndPetRegisteredChecking(petDB, userDB))
                {
                    SendInviteForUnregistered();
                    return;
                }

                StartDuel(petDB, userDB);
                return;
            }

            if (textReceived == Constants.CommandsMP.ShowChatRanksMPCommand)
            {
                if (!IsUserAndPetRegisteredChecking(petDB, userDB))
                {
                    SendInviteForUnregistered();
                    return;
                }

                ShowRanksMP(petDB, userDB);
                return;
            }

            if (textReceived == Constants.CommandsMP.FeedMPCommand)
            {
                if (!IsUserAndPetRegisteredChecking(petDB, userDB))
                {
                    SendInviteForUnregistered();
                    return;
                }

                SendFeedMPMessage(userDB);
                return;
            }

            if (IsMessageHasMentions())
            {
                FeedByMentionPetMP(petDB, userDB);
            }
        }
        public void CallbackHandler()
        {
            var userDB = _appServices.UserService.Get(_userId);
            var petDB = _appServices.PetService.Get(_userId);

            if (userDB == null || petDB == null || petDB.IsGone || petDB.HP <= 0)
                return;

            if (_callback.Data.Contains(new DuelMuliplayerCommand().StartDuelMultiplayerButton.CallbackData))
            {
                AcceptDuel();
            }

            if (_callback.Data.Contains(new RanksMultiplayerCommand().ShowChatRanksMP.CallbackData))
            {
                ShowRanks();
            }

            if (_callback.Data.Contains(new RanksMultiplayerCommand().ShowGlobalRanksMP.CallbackData))
            {
                ShowRanks(true);
            }

            async void AcceptDuel()
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
                    Culture = new CultureInfo(userDB?.Culture ?? "ru");
                    string notEnoughHPTextCallback = string.Format(NotEnoughHPForDuelCallback, Constants.Costs.DuelHP, petDB?.HP ?? 0);
                    _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, notEnoughHPTextCallback, true);
                    Log.Debug($"MP: called callback AcceptDuel, but not enough HP by {_userLogInfo}");
                    return;
                }

                if (_userId == duelCreatorId)
                {
                    Culture = new CultureInfo(userDB?.Culture ?? "ru");
                    string cannotYourselfText = string.Format(DuelMPErrorYourself);
                    _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, cannotYourselfText, true);
                    Log.Debug($"MP: called callback AcceptDuel, but can not accept own duel by {_userLogInfo}");
                    return;
                }

                if (userDB?.Gold < Constants.Costs.DuelGold)
                {
                    Culture = new CultureInfo(userDB?.Culture ?? "ru");
                    string notEnoughGoldTextCallback = string.Format(NotEnoughGoldForDuelCallback, userDB?.Gold ?? 0, Constants.Costs.DuelGold);
                    _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, notEnoughGoldTextCallback, true);
                    Log.Debug($"MP: called callback AcceptDuel, but not enough gold by {_userLogInfo}");
                    return;
                }

                var metaUserDB = _appServices.MetaUserService.Get(_userId);
                var timeToWaitNextDuel = metaUserDB?.NextPossibleDuelTime - DateTime.UtcNow;
                if (timeToWaitNextDuel != null && timeToWaitNextDuel > TimeSpan.Zero)
                {
                    var timeToWaitString = new DateTime(timeToWaitNextDuel.GetValueOrDefault().Ticks, DateTimeKind.Utc).ToString("HH:mm:ss");
                    Culture = new CultureInfo(userDB?.Culture ?? "ru");
                    string waitForDuelText = string.Format(DuelMPCooldownCallback, timeToWaitString);
                    _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, waitForDuelText, true);
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

                var userDBCreator = _appServices.UserService.Get(duelCreatorId);
                Culture = new CultureInfo(userDBCreator?.Culture ?? "ru");
                AnswerMessage answerMessage = new AnswerMessage()
                {
                    StickerId = Constants.StickersId.MPDuelStarted,
                    ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                    Text = string.Format(DuelMPFighting1, personalLinkCreatorDuel, personalLinkDuelAccepted, petDefenderName, petAttackerName),
                    msgThreadId = _msgThreadId
                };

                Log.Debug($"MP: duel accepted by {_userLogInfo}");
                Culture = new CultureInfo(userDBCreator?.Culture ?? "ru");
                var fightingMsg = await _appServices.BotControlService.SendAnswerMessageGroupAsync(answerMessage, _chatId, false);
                if (fightingMsg == null)
                {
                    Log.Fatal("ERROR ON FIGHTING_MP #1");
                    return;
                }

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
                    _appServices.MetaUserService.UpdateNextPossibleDuelTime(_userId, DateTime.UtcNow + new Constants.TimesToWait().DuelCDToWait);
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
                    _appServices.MetaUserService.UpdateNextPossibleDuelTime(userDBCreator.UserId, DateTime.UtcNow + new Constants.TimesToWait().DuelCDToWait);
                }

                var fightEnd = string.Format(DuelMPFightingEnd, personalLinkCreatorDuel, personalLinkDuelAccepted, ownerWinnerName, fightingEndExplanation, Constants.Rewards.WonDuel);
                await _appServices.BotControlService.EditMessageTextAsync(_chatId, fightingMsg.MessageId, fightEnd, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
            }
            async void ShowRanks(bool isGlobal = false)
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
                            InlineKeyboardButton.WithCallbackData(new RanksMultiplayerCommand().ShowChatRanksMP.Text,  new RanksMultiplayerCommand().ShowChatRanksMP.CallbackData + $"_{_userId}"),
                            InlineKeyboardButton.WithCallbackData(new RanksMultiplayerCommand().ShowGlobalRanksMP.Text,  new RanksMultiplayerCommand().ShowGlobalRanksMP.CallbackData + $"_{_userId}"),
                        },
                        new List<InlineKeyboardButton>()
                        {
                            InlineKeyboardButton.WithUrl(
                                new InviteMuliplayerCommand().InviteReferalMultiplayerButton(userDB.FirstName).Text,
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
                Text = toSendText,
                msgThreadId = _msgThreadId
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
                Text = toSendText,
                msgThreadId = _msgThreadId
            };

            await _appServices.BotControlService.SendAnswerMessageGroupAsync(answerMessage, _chatId, false);
            Log.Debug($"MP: called SendWelcomeMessageOnStart, invited by ID: {_userId}");
        }

        private async void ShowRanksMP(Pet petDB, User userDB)
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
                            InlineKeyboardButton.WithCallbackData(new RanksMultiplayerCommand().ShowChatRanksMP.Text,  new RanksMultiplayerCommand().ShowChatRanksMP.CallbackData + $"_{_userId}"),
                            InlineKeyboardButton.WithCallbackData(new RanksMultiplayerCommand().ShowGlobalRanksMP.Text,  new RanksMultiplayerCommand().ShowGlobalRanksMP.CallbackData + $"_{_userId}"),
                        },
                        new List<InlineKeyboardButton>()
                        {
                            InlineKeyboardButton.WithUrl(
                                new InviteMuliplayerCommand().InviteReferalMultiplayerButton(userDB.FirstName).Text,
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
        private async void StartDuel(Pet petDB, User userDB)
        {
            var personalLink = Extensions.GetPersonalLink(_userId, _userName);
            if (userDB?.Gold < Constants.Costs.DuelGold)
            {
                Culture = new CultureInfo(userDB.Culture ?? "ru");
                AnswerMessage notEnoughGoldMsg = new AnswerMessage()
                {
                    ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                    Text = string.Format(NotEnoughGoldForDuel, personalLink, userDB.Gold, Constants.Costs.DuelGold),
                    msgThreadId = _msgThreadId
                };
                await _appServices.BotControlService.SendAnswerMessageGroupAsync(notEnoughGoldMsg, _chatId, false);
                Log.Debug($"MP: called command StartDuel, but not enough gold by {_userLogInfo}");
                return;
            }

            if (petDB?.HP < Constants.Costs.DuelHP)
            {
                Culture = new CultureInfo(userDB?.Culture ?? "ru");
                AnswerMessage notEnoughHPMsg = new AnswerMessage()
                {
                    ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                    Text = string.Format(NotEnoughHPForDuel, personalLink, Constants.Costs.DuelHP, petDB.HP),
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
                Culture = new CultureInfo(userDB?.Culture ?? "ru");
                AnswerMessage waitForDuelText = new AnswerMessage()
                {
                    ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                    Text = string.Format(DuelMPCooldown, waitTimeString, personalLink),
                    msgThreadId = _msgThreadId
                };
                await _appServices.BotControlService.SendAnswerMessageGroupAsync(waitForDuelText, _chatId, false);
                Log.Debug($"MP: called command StartDuel, but have to wait {waitTimeString} by {_userLogInfo}");
                return;
            }

            Culture = new CultureInfo(userDB?.Culture ?? "ru");
            var customCallback = new DuelMuliplayerCommand().StartDuelMultiplayerButton;
            customCallback.CallbackData += $"_{userDB?.UserId}_{_message.MessageId}";
            AnswerMessage answerMessage = new AnswerMessage()
            {
                InlineKeyboardMarkup = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
                    {
                        customCallback
                    }),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                Text = string.Format(DuelMPStartCommand, personalLink, petDB?.Name),
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
            _appServices.MetaUserService.UpdateMsgDuelId(_userId, sentMsg?.MessageId ?? -1);
            _appServices.MetaUserService.UpdateChatDuelId(_userId, _chatId);
            _appServices.MetaUserService.UpdateMsgCreatorDuelId(_userId, _message?.MessageId ?? -1);
            _appServices.MetaUserService.UpdateDuelStartTime(_userId, DateTime.UtcNow);
            Log.Debug($"MP: started duel by {_userLogInfo}");
        }
        private async void ShowPetMP(Pet petDB, User userDB)
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
                Text = toSendText,
                msgThreadId = _msgThreadId
            };

            await _appServices.BotControlService.SendAnswerMessageGroupAsync(answerMessage, _chatId, false);
            Log.Debug($"MP: called showPet by {_userLogInfo}");
        }
        public async void FeedByMentionPetMP(Pet petDB, User userDB)
        {
            bool isError = false;
            var metaUserDB = _appServices.MetaUserService.Get(_userId);

            if (!metaUserDB?.IsFeedingMPStarted ?? true)
            {
                Log.Debug($"MP: called command FeedByMentionPetMP, but feeding not started yet by {_userLogInfo}");
                return;
            }

            if (!CheckAndSendOnFailIsEnoughGoldAtFeeder(userDB))
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
                SendCanNotFeedOwnPetMessage();
                goto ending;
            }

            var petToFeedDB = _appServices.PetService.Get(userToFeedDB.UserId);
            if (petToFeedDB == null)
            {
                isError = true;
                goto ending;
            }

            var spentTimeByLastPetFeeding = DateTime.UtcNow - petToFeedDB.LastMPFedTime;
            if (spentTimeByLastPetFeeding < new Constants.TimesToWait().FeedMPCDToWait)
            {
                string timeToWait = new DateTime((petToFeedDB.LastMPFedTime + new Constants.TimesToWait().FeedMPCDToWait - DateTime.UtcNow).Ticks).ToString("HH:mm:ss");
                var persLink = Extensions.GetPersonalLink(_userId, _userName);
                var petName = HttpUtility.HtmlEncode(petToFeedDB.Name);
                petName = "<b>" + petName + "</b>";
                await _appServices.BotControlService.SendAnswerMessageGroupAsync(new AnswerMessage()
                {
                    Text = string.Format(NotEnoughTimeSpentFeedMP, persLink, petName, timeToWait),
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
            _appServices.PetService.UpdateMPSatiety(petToFeedDB.UserId, petToFeedDB.MPSatiety + Constants.FoodFactors.MPFeed);
            _appServices.PetService.UpdateLastMPFedTime(userToFeedDB.UserId, DateTime.UtcNow);

            await _appServices.BotControlService.SendAnswerMessageGroupAsync(new AnswerMessage()
            {
                Text = string.Format(FeedMPEndSuccess, HttpUtility.HtmlEncode(petToFeedDB.Name), Constants.Costs.FeedMP),
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
                    Text = FeedMPNotFound,
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

        private async void SendFeedMPMessage(Models.Mongo.User userDB)
        {
            if (!CheckAndSendOnFailIsEnoughGoldAtFeeder(userDB))
                return;

            if (!await CheckAndSendOnFailSpentTimeByFeeder())
                return;

            var persLink = Extensions.GetPersonalLink(_userId, _userName);
            await _appServices.BotControlService.SendAnswerMessageGroupAsync(new AnswerMessage()
            {
                Text = string.Format(FeedMPStart, persLink),
                replyToMsgId = _message.MessageId,
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                msgThreadId = _msgThreadId
            },
            _chatId,
            false);
            _appServices.MetaUserService.UpdateIsFeedingMPStarted(_userId, true);
        }
        private async void SendNotEnoughGoldForFeedMessage(Models.Mongo.User userDB)
        {
            var persLink = Extensions.GetPersonalLink(_userId, _userName);
            await _appServices.BotControlService.SendAnswerMessageGroupAsync(new AnswerMessage()
            {
                Text = string.Format(NotEnoughGoldForMPFeed, persLink, userDB.Gold, Constants.Costs.FeedMP),
                replyToMsgId = _message.MessageId,
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                msgThreadId = _msgThreadId
            },
            _chatId,
            false);
        }
        private async void SendCanNotFeedOwnPetMessage()
        {
            await _appServices.BotControlService.SendAnswerMessageGroupAsync(new AnswerMessage()
            {
                Text = string.Format(FeedMPErrorFeedOwnPet),
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
            if (spentTimeByLastUserFeeding < new Constants.TimesToWait().FeedMPCDToWait)
            {
                string timeToWait = new DateTime((metaUserFeeder.LastMPFeedingTime + new Constants.TimesToWait().FeedMPCDToWait - DateTime.UtcNow).Ticks).ToString("HH:mm:ss");
                var persLink = Extensions.GetPersonalLink(_userId, _userName);
                await _appServices.BotControlService.SendAnswerMessageGroupAsync(new AnswerMessage()
                {
                    Text = string.Format(NotEnoughTimeSpentByLastMPFeed, persLink, timeToWait),
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
        private bool CheckAndSendOnFailIsEnoughGoldAtFeeder(Models.Mongo.User userDB)
        {
            if (userDB.Gold < Constants.Costs.FeedMP)
            {
                SendNotEnoughGoldForFeedMessage(userDB);
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
            StringBuilder msgToShow = new StringBuilder($"{string.Format(ShowChatRanksMPHeader, encodedChatName)}" +
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

            StringBuilder msgToShow = new StringBuilder($"{ShowGlobalRanksMPHeader}" +
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
