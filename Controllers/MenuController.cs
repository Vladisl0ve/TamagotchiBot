using MongoDB.Driver.Linq;
using OpenAI_API;
using OpenAI_API.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Web;
using TamagotchiBot.Database;
using TamagotchiBot.Models;
using TamagotchiBot.Models.Answers;
using TamagotchiBot.Models.Mongo;
using TamagotchiBot.Services.Interfaces;
using TamagotchiBot.UserExtensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Quartz;

using static TamagotchiBot.Resources.Resources;
using static TamagotchiBot.UserExtensions.Constants;
using Extensions = TamagotchiBot.UserExtensions.Extensions;
using User = TamagotchiBot.Models.Mongo.User;

namespace TamagotchiBot.Controllers
{
    public class MenuController : ControllerBase
    {
        private readonly IApplicationServices _appServices;
        private readonly IEnvsSettings _envs;
        private readonly Message _message = null;
        private readonly CallbackQuery _callback = null;
        private readonly long _userId;
        private readonly string _userInfo;
        private readonly CultureInfo _userCulture;
        private readonly PetType _userPetType;
        private readonly string _userPetEmoji;

        private MenuController(IApplicationServices services, IEnvsSettings envs, Message message = null, CallbackQuery callback = null)
        {
            _callback = callback;
            _message = message;
            _userId = callback?.From.Id ?? message.From.Id;

            _appServices = services;
            _envs = envs;

            _userInfo = Extensions.GetLogUser(_appServices.UserService.Get(_userId));
            _userCulture = new CultureInfo(_appServices.UserService.Get(_userId)?.Culture ?? "ru");
            _userPetType = Extensions.GetEnumPetType(_appServices.PetService.Get(_userId)?.Type ?? -1);
            _userPetEmoji = Extensions.GetTypeEmoji(_userPetType);
        }
        public MenuController(IApplicationServices services, IEnvsSettings envs, CallbackQuery callback) : this(services, envs, callback: callback, message: null)
        {
        }

        public MenuController(IApplicationServices services, IEnvsSettings envs, Message message) : this(services, envs, message: message, callback: null)
        {
        }

        public async Task ProcessMessage(string customText = null)
        {
            await CommandHandler(customText);
        }

        private async Task CommandHandler(string customText = null)
        {
            string textReceived = customText ?? _message.Text;
            if (textReceived == null)
                return;

            textReceived = textReceived.ToLower();
            if (textReceived.First() == '/')
                textReceived = textReceived.Substring(1);

            var petDB = _appServices.PetService.Get(_userId);
            var userDB = _appServices.UserService.Get(_userId);

            if (textReceived == Commands.LanguageCommand || GetAllTranslatedAndLowered(nameof(languageCommandDescription)).Contains(textReceived))
            {
                await ChangeLanguageCmd();
                return;
            }
            if (textReceived == Commands.HelpCommand || GetAllTranslatedAndLowered(nameof(helpCommandDescription)).Contains(textReceived))
            {
                await ShowHelpInfo();
                return;
            }
            if (textReceived == Commands.PetCommand
                || GetAllTranslatedAndLowered(nameof(petCommandDescription)).Contains(textReceived)
                || GetAllTranslatedAndLowered(nameof(goAwayButton)).Contains(textReceived)
                )
            {
                await ShowPetInfo(petDB);
                return;
            }
            if (textReceived == Commands.BathroomCommand || GetAllTranslatedAndLowered(nameof(bathroomCommandDescription)).Contains(textReceived))
            {
                await GoToBathroom(petDB);
                return;
            }
            if (textReceived == Commands.KitchenCommand || GetAllTranslatedAndLowered(nameof(kitchenCommandDescription)).Contains(textReceived))
            {
                await GoToKitchen(petDB, userDB);
                return;
            }
            if (textReceived == Commands.GameroomCommand || GetAllTranslatedAndLowered(nameof(gameroomCommandDescription)).Contains(textReceived))
            {
                await GoToGameroom(petDB, userDB);
                return;
            }
            if (textReceived == Commands.HospitalCommand || GetAllTranslatedAndLowered(nameof(hospitalCommandDescription)).Contains(textReceived))
            {
                await GoToHospital(petDB);
                return;
            }
            if (textReceived == Commands.RanksCommand || GetAllTranslatedAndLowered(nameof(ranksCommandDescription)).Contains(textReceived))
            {
                await ShowRankingInfo();
                return;
            }
            if (textReceived == Commands.SleepCommand || GetAllTranslatedAndLowered(nameof(sleepCommandDescription)).Contains(textReceived))
            {
                await GoToSleep(petDB);
                return;
            }
            if (textReceived == Commands.ChangelogCommand || GetAllTranslatedAndLowered(nameof(changelogCommandDescription)).Contains(textReceived))
            {
                await ShowChangelogsInfo();
                return;
            }
            //if (textReceived.StartsWith(Commands.GeminiCommand))
            //{
            //    await SendGeminiMessage(petDB, userDB);
            //    return;
            //}
            if (textReceived == Commands.MenuCommand || GetAllTranslatedAndLowered(nameof(menuCommandDescription)).Contains(textReceived))
            {
                await ShowMenuInfo();
                return;
            }
            if (textReceived == Commands.RenameCommand || GetAllTranslatedAndLowered(nameof(renameCommandDescription)).Contains(textReceived))
            {
                await RenamePet();
                return;
            }
            if (textReceived == Commands.WorkCommand || GetAllTranslatedAndLowered(nameof(workCommandDescription)).Contains(textReceived))
            {
                await ShowWorkInfo(petDB);
                return;
            }
            if (textReceived == Commands.RewardCommand || GetAllTranslatedAndLowered(nameof(rewardCommandDescription)).Contains(textReceived))
            {
                await ShowRewardInfo(userDB);
                return;
            }
            if (textReceived == Commands.ReferalCommand || GetAllTranslatedAndLowered(nameof(referalCommandDescription)).Contains(textReceived))
            {
                await ShowReferalInfo();
                return;
            }
            if (textReceived == Commands.FarmCommand || GetAllTranslatedAndLowered(nameof(farmCommandDescription)).Contains(textReceived))
            {
                await GoToFarm(petDB);
                return;
            }
            if (GetAllTranslatedAndLowered(nameof(farmButtonChangeType)).Contains(textReceived))
            {
                await ChangeTypeCMD();
                return;
            }
            if (GetAllTranslatedAndLowered(nameof(farmButtonAutoFeed)).Contains(textReceived))
            {
                await BuyAutoFeedCMD(userDB, petDB);
                return;
            }
            if (GetAllTranslatedAndLowered(nameof(CatTypeText)).Contains(textReceived))
            {
                await ChangeTypeToCatCMD(userDB, petDB);
                return;
            }
            if (GetAllTranslatedAndLowered(nameof(DogTypeText)).Contains(textReceived))
            {
                await ChangeTypeToDogCMD(userDB, petDB);
                return;
            }
            if (GetAllTranslatedAndLowered(nameof(FoxTypeText)).Contains(textReceived))
            {
                await ChangeTypeToFoxCMD(userDB, petDB);
                return;
            }
            if (GetAllTranslatedAndLowered(nameof(MouseTypeText)).Contains(textReceived))
            {
                await ChangeTypeToMouseCMD(userDB, petDB);
                return;
            }
            if (GetAllTranslatedAndLowered(nameof(PandaTypeText)).Contains(textReceived))
            {
                await ChangeTypeToPandaCMD(userDB, petDB);
                return;
            }
            if (textReceived == "test")
            {
                Log.Debug($"Called /test for {_userInfo}");
                var toSend = new AnswerMessage()
                {
                    Text = nameof(DevelopWarning).UseCulture(_userCulture),
                    StickerId = StickersId.DevelopWarningSticker,
                    ReplyMarkup = null,
                    InlineKeyboardMarkup = null
                };
                await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
                return;
            }

            //await AnswerByChatGPT(petDB);
            await AnswerByGemini(petDB, userDB);

            Log.Debug($"[MESSAGE] '{customText ?? _message.Text}' FROM {_userInfo}");
        }

        private async Task AnswerByChatGPT(Pet petDB)
        {
            new ForwardController(_appServices, _message).StartForwarding();

            var previousQA = _appServices.MetaUserService.GetLastChatGPTQA(_userId);
            bool isTimeOut;
            string chatGptAnswer;
            if (previousQA.Count >= QA_MAX_COUNTER && (DateTime.UtcNow - previousQA[0].revision) < new TimeSpan(0, 30, 0)) //30 minutes timeout
            {
                isTimeOut = true;
                chatGptAnswer = string.Format(
                    nameof(ChatGPTTimeOutText).UseCulture(_userCulture),
                    Extensions.GetTypeEmoji(petDB.Type),
                    HttpUtility.HtmlEncode(petDB.Name)
                    );
            }
            else
            {
                var (answer, isCanceled) = await GetAnswerChatGPT(petDB, Extensions.GetLongTypeEmoji(Extensions.GetEnumPetType(petDB.Type), new CultureInfo("en")));
                chatGptAnswer = $"{Extensions.GetLongTypeEmoji(_userPetType, _userCulture)} <b>{HttpUtility.HtmlEncode(petDB.Name)}</b>: ";
                chatGptAnswer += answer;
                isTimeOut = isCanceled;
            }

            var toSend = new AnswerMessage()
            {
                Text = chatGptAnswer,
                replyToMsgId = _message.MessageId,
                ReplyMarkup = isTimeOut ? ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture) : null,
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
            };

            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, true, true);
        }

        private async Task<(string answer, bool isCanceled)> GetAnswerChatGPT(Pet petDB, string type)
        {
            await _appServices.BotControlService.SendChatActionAsync(_userId, Telegram.Bot.Types.Enums.ChatAction.Typing);
            string openAiKey = _appServices.SInfoService.GetOpenAiKey();

            string result;
            bool isCanceled = false;
            try
            {
                OpenAIAPI api = new OpenAIAPI(openAiKey ?? _envs.OpenAiApiKey);

                var chat = api.Chat.CreateConversation();
                chat.Model = Model.ChatGPTTurbo;
                chat.RequestParameters.Temperature = 0.6;

                /// give instruction as System
                chat.AppendSystemMessage($"Meet {petDB.Name}, your virtual companion with real {type} traits," +
                    $" adoring all, especially children. " +
                    $"Opposes injustice, caters to ages 6-25. " +
                    $"Responds in last language, prefers Russian, Belarusian, Polish, English, defaults to {_userCulture.EnglishName}. " +
                    $"Use emojis for Telegram chat. Mood adjusts to question tones." +
                    $"Use HTML style to format answer to bold, underling or italic." +
                    $"Use '```' to mark text as fixed-width code." +
                    $"Monitor needs: {petDB.Satiety}% satiety (warn if low), " +
                    $"{petDB.Hygiene}% Hygiene (warn if low), " +
                    $"{petDB.Fatigue}% Fatigue (warn if high), " +
                    $"{petDB.HP}% HP (warn if low).");

                // give a few examples as user and assistant
                chat.AppendUserInput("Как тебя зовут?");
                chat.AppendExampleChatbotOutput($"{petDB.Name}, а тебя?");
                chat.AppendUserInput("Ты настоящий?");
                chat.AppendExampleChatbotOutput("Я твой виртуальный питомец и разумеется я настоящий🦾");
                chat.AppendUserInput("Ты умеешь говорить?");
                chat.AppendExampleChatbotOutput("Нет, ведь же я животное, а не человек :) Но я умею переписываться с тобой в Телеграмме");
                chat.AppendUserInput("Ты голодный?");
                chat.AppendExampleChatbotOutput(petDB.Satiety > 50 ? $"Не особо, я сыт на {petDB.Satiety}" : $"Можно и перекусить, ведь я сыт на {petDB.Satiety}");
                chat.AppendUserInput("Хочешь кушать?");
                chat.AppendExampleChatbotOutput(petDB.Satiety > 50 ? $"Не особо, я сыт на {petDB.Satiety}" : $"Можно и перекусить, ведь я сыт на {petDB.Satiety}");
                chat.AppendUserInput("Ты чистый или грязный?");
                chat.AppendExampleChatbotOutput(petDB.Hygiene > 50 ? $"Я чистый на {petDB.Hygiene}, всё хорошо" : $"Надо бы сходить в душ");
                chat.AppendUserInput("Ты уставший?");
                chat.AppendExampleChatbotOutput(petDB.Fatigue > 50 ? $"Не, я бодр" : $"Стоит отдохнуть, что-то у меня голова болеть начинает");
                chat.AppendUserInput("Ты живой?");
                chat.AppendExampleChatbotOutput(petDB.HP > 50 ? $"ДА, я полон сил! У меня {petDB.HP} здоровья" : $"Мне плохо, всё болит. Моё здоровье всего лишь {petDB.HP}");

                var previousQA = _appServices.MetaUserService.GetLastChatGPTQA(_userId);
                foreach (var item in previousQA.Skip(Math.Max(0, previousQA.Count - Constants.QA_TO_FEED_COUNTER)))
                {
                    chat.AppendUserInput(item.userQ);
                    chat.AppendExampleChatbotOutput(item.chatGPTA);
                }

                chat.AppendUserInput($"{_message.Text}");

                result = await chat.GetResponseFromChatbotAsync();
                Log.Information($"CHATGPT USAGE ========> {chat.MostRecentApiResult.Model.ModelID}: TOTAL [{chat.MostRecentApiResult.Usage.TotalTokens}] = PROMPT [{chat.MostRecentApiResult.Usage.PromptTokens}] + COMPLETETION [{chat.MostRecentApiResult.Usage.CompletionTokens}]");

                result = FixHTMLEscaping(result);
                _appServices.MetaUserService.AppendNewChatGPTQA(_userId, _message.Text, result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"CHATGPT ERROR");
                isCanceled = true;
                result = nameof(ChatgptErrorAnswerText).UseCulture(_userCulture);
            }

            return (result, isCanceled);
        }

        private string FixHTMLEscaping(string toFix)
        {
            var result = toFix;
            if (toFix.Contains("```"))
            {
                var chunks = toFix.Split("```");
                if (chunks.Length == 0)
                    return result;

                if (chunks.Length % 2 == 0)
                    return result;

                for (int i = 0; i < chunks.Length; i++)
                {
                    if (chunks[i] == chunks[0] || chunks[i] == chunks[^1])
                        continue;

                    if (i % 2 == 1)
                        chunks[i] = "<pre>" + HttpUtility.HtmlEncode(chunks[i]) + "</pre>";
                }

                result = string.Join(" ", chunks);
            }

            return result;
        }

        private async Task ChangeTypeToCatCMD(User userDB, Pet petDB)
        {
            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.ChangedToCatCounter++;
            _appServices.AllUsersDataService.Update(aud);

            Log.Debug($"Called /ChangeTypeToCatCMD for {_userInfo}");
            await ChangePetTypeCMD(userDB,
                                   petDB,
                                   PetType.Cat,
                                   string.Format(
                                       nameof(changedTypeToCat).UseCulture(_userCulture),
                                       HttpUtility.HtmlEncode(petDB.Name)));
        }
        private async Task ChangeTypeToDogCMD(User userDB, Pet petDB)
        {
            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.ChangedToDogCounter++;
            _appServices.AllUsersDataService.Update(aud);

            Log.Debug($"Called /ChangeTypeToDogCMD for {_userInfo}");
            await ChangePetTypeCMD(
                userDB,
                petDB,
                PetType.Dog,
                string.Format(
                    nameof(changedTypeToDog).UseCulture(_userCulture),
                    HttpUtility.HtmlEncode(petDB.Name)));
        }
        private async Task ChangeTypeToPandaCMD(User userDB, Pet petDB)
        {
            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.ChangedToPandaCounter++;
            _appServices.AllUsersDataService.Update(aud);

            Log.Debug($"Called /ChangeTypeToPandaCMD for {_userInfo}");
            await ChangePetTypeCMD(userDB,
                                   petDB,
                                   PetType.Panda,
                                   string.Format(
                                       nameof(changedTypeToPanda).UseCulture(_userCulture),
                                       HttpUtility.HtmlEncode(petDB.Name)));
        }
        private async Task ChangeTypeToFoxCMD(User userDB, Pet petDB)
        {
            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.ChangedToFoxCounter++;
            _appServices.AllUsersDataService.Update(aud);

            Log.Debug($"Called /ChangeTypeToFoxCMD for {_userInfo}");
            await ChangePetTypeCMD(userDB,
                                   petDB,
                                   PetType.Fox,
                                   string.Format(
                                       nameof(changedTypeToFox).UseCulture(_userCulture),
                                       HttpUtility.HtmlEncode(petDB.Name)));
        }
        private async Task ChangeTypeToMouseCMD(User userDB, Pet petDB)
        {
            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.ChangedToMouseCounter++;
            _appServices.AllUsersDataService.Update(aud);

            Log.Debug($"Called /ChangeTypeToMouseCMD for {_userInfo}");
            await ChangePetTypeCMD(userDB,
                                   petDB,
                                   PetType.Mouse,
                                   string.Format(
                                       nameof(changedTypeToMouse).UseCulture(_userCulture),
                                       HttpUtility.HtmlEncode(petDB.Name)));
        }

        private async Task ChangePetTypeCMD(User userDB, Pet petDB, PetType newPetType, string toSendText)
        {
            if ((int)newPetType == petDB.Type)
            {
                var toSendErr1 = new AnswerMessage()
                {
                    Text = nameof(changeTypeErrorSameType).UseCulture(_userCulture),
                    StickerId = Constants.StickersId.ChangeTypeErrorSticker,
                    ReplyMarkup = ReplyKeyboardItems.ChangeTypeKeyboardMarkup(_userCulture),
                    ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
                };
                await _appServices.BotControlService.SendAnswerMessageAsync(toSendErr1, _userId, false);

                return;
            }

            if (userDB.Gold < Costs.ChangePetType)
            {
                var toSendErr2 = new AnswerMessage()
                {
                    Text = nameof(goldNotEnough).UseCulture(_userCulture),
                    StickerId = Constants.StickersId.ChangeTypeErrorGoldSticker,
                    ReplyMarkup = ReplyKeyboardItems.ChangeTypeKeyboardMarkup(_userCulture),
                    ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
                };
                await _appServices.BotControlService.SendAnswerMessageAsync(toSendErr2, _userId, false);

                return;
            }
            _appServices.PetService.UpdateType(userDB.UserId, newPetType);
            _appServices.UserService.UpdateGold(_userId, userDB.Gold - Costs.ChangePetType);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.GetStickerByType(nameof(StickersId.PetChangeTypeSticker_Cat), newPetType),
                ReplyMarkup = ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }

        public async Task CallbackHandler()
        {
            var userDb = _appServices.UserService.Get(_userId);
            var petDb = _appServices.PetService.Get(_userId);

            if (userDb == null || petDb == null)
                return;

            if (_callback.Data == CallbackButtons.PetCommand.PetCommandInlineBasicInfo(_userCulture).CallbackData)
            {
                await ShowBasicInfoInline(petDb);
                return;
            }

            if (_callback.Data == CallbackButtons.PetCommand.PetCommandInlineExtraInfo(_userCulture).CallbackData)
            {
                await ShowExtraInfoInline(petDb);
                return;
            }

            if (_callback.Data == CallbackButtons.KitchenCommand.KitchenCommandInlineBread.CallbackData)
            {
                await FeedWithBreadInline(petDb);
                return;
            }

            if (_callback.Data == CallbackButtons.KitchenCommand.KitchenCommandInlineRedApple.CallbackData)
            {
                await FeedWithAppleInline(petDb);
                return;
            }

            if (_callback.Data == CallbackButtons.KitchenCommand.KitchenCommandInlineChocolate.CallbackData)
            {
                await FeedWithChocolateInline(petDb);
                return;
            }

            if (_callback.Data == CallbackButtons.KitchenCommand.KitchenCommandInlineLollipop.CallbackData)
            {
                await FeedWithLollipopInline(petDb);
                return;
            }

            if (_callback.Data == CallbackButtons.SleepCommand.SleepCommandInlinePutToSleep(default, _userCulture).CallbackData)
            {
                await PutToSleepInline(petDb);
                return;
            }

            if (_callback.Data == CallbackButtons.WorkCommand.WorkCommandInlineWorkOnPC(_userCulture).CallbackData)
            {
                await StartWorkInline(petDb, JobType.WorkingOnPC);
                return;
            }

            if (_callback.Data == CallbackButtons.WorkCommand.WorkCommandInlineShowTime(default, JobType.WorkingOnPC, _userCulture).CallbackData)
            {
                await StartWorkInline(petDb, JobType.WorkingOnPC);
                return;
            }

            if (_callback.Data == CallbackButtons.WorkCommand.WorkCommandInlineDistributeFlyers(_userCulture).CallbackData)
            {
                await StartWorkInline(petDb, JobType.FlyersDistributing);
                return;
            }

            if (_callback.Data == CallbackButtons.WorkCommand.WorkCommandInlineShowTime(default, JobType.FlyersDistributing, _userCulture).CallbackData)
            {
                await StartWorkInline(petDb, JobType.FlyersDistributing);
                return;
            }

            if (_callback.Data == CallbackButtons.RewardsCommand.RewardCommandInlineDailyReward(_userCulture).CallbackData)
            {
                await GetDailyRewardInline(userDb);
                return;
            }

            if (_callback.Data == CallbackButtons.RewardsCommand.RewardCommandDailyRewardInlineShowTime(default, _userCulture).CallbackData)
            {
                await GetDailyRewardInline(userDb);
                return;
            }

            if (_callback.Data == "gameroomCommandInlineCard")
            {
                await PlayCardInline(petDb);
                return;
            }

            if (_callback.Data == CallbackButtons.GameroomCommand.GameroomCommandInlineDice.CallbackData)
            {
                await PlayDiceInline(petDb);
                return;
            }

            if (_callback.Data == CallbackButtons.HospitalCommand.HospitalCommandCurePills(_userCulture).CallbackData)
            {
                await CureWithPill(petDb);
                return;
            }

            if (_callback.Data == CallbackButtons.BathroomCommand.BathroomCommandBrushTeeth(_userCulture).CallbackData)
            {
                await TeethInline(petDb);
                return;
            }

            if (_callback.Data == CallbackButtons.BathroomCommand.BathroomCommandMakePoo(_userCulture).CallbackData)
            {
                await MakePooInline(petDb);
                return;
            }

            if (_callback.Data == CallbackButtons.BathroomCommand.BathroomCommandTakeShower(_userCulture).CallbackData)
            {
                await TakeShowerInline(petDb);
                return;
            }

            if (_callback.Data == CallbackButtons.RanksCommand.RanksCommandInlineGold(_userCulture).CallbackData)
            {
                await ShowRanksGold();
                return;
            }

            if (_callback.Data == CallbackButtons.RanksCommand.RanksCommandInlineDiamonds(_userCulture).CallbackData)
            {
                await ShowRanksDiamonds();
                return;
            }

            if (_callback.Data == CallbackButtons.RanksCommand.RanksCommandInlineLevel(_userCulture).CallbackData)
            {
                await ShowRanksLevel();
                return;
            }

            if (_callback.Data == CallbackButtons.RanksCommand.RanksCommandInlineLevelAll(_userCulture).CallbackData)
            {
                await ShowRanksLevelAllGame();
                return;
            }

            if (_callback.Data == CallbackButtons.RanksCommand.RanksCommandInlineApples(_userCulture).CallbackData)
            {
                await ShowRanksApples();
                return;
            }

            if (_callback.Data == CallbackButtons.FarmCommand.FarmCommandInlineEnableAutoFeed(_userCulture).CallbackData)
            {
                await SetAutoFeedStatus(petDb, true);
                return;
            }

            if (_callback.Data == CallbackButtons.FarmCommand.FarmCommandInlineDisableAutoFeed(_userCulture).CallbackData)
            {
                await SetAutoFeedStatus(petDb, false);
                return;
            }
        }

        #region Message Answers
        private async Task ChangeTypeCMD()
        {
            string toSendText = string.Format(nameof(changeTypeButtonCommand).UseCulture(_userCulture),
                                              Costs.ChangePetType);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.ChangeTypeButtonCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.ChangeTypePetSticker,
                ReplyMarkup = ReplyKeyboardItems.ChangeTypeKeyboardMarkup(_userCulture),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
            };
            Log.Debug($"Called /ChangeTypeCMD for {_userInfo}");
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task ShowWorkInfo(Pet petDB)
        {
            Log.Debug($"Called /ShowWorkInfo for {_userInfo}");

            var accessCheck = CheckStatusIsInactiveOrNull(petDB, IsGoToWorkCommand: true);
            if (accessCheck != null)
            {
                Log.Debug($"Pet is busy for {_userInfo}");
                await _appServices.BotControlService.SendAnswerMessageAsync(accessCheck, _userId, false);
                return;
            }

            if (petDB.CurrentStatus == (int)CurrentStatus.Working)
            {
                await ServeWorkCommandPetStillWorking(petDB, (JobType)petDB.CurrentJob);
                return;
            }

            string toSendText = string.Format(nameof(workCommand).UseCulture(_userCulture),
                                       new DateTime(TimesToWait.WorkOnPCToWait.Ticks).ToString("HH:mm:ss"),
                                       Rewards.WorkOnPCGoldReward,
                                       petDB.Fatigue,
                                       new DateTime(TimesToWait.FlyersDistToWait.Ticks).ToString("HH:mm:ss"),
                                       Rewards.FlyersDistributingGoldReward);

            List<CallbackModel> inlineParts = InlineItems.InlineWork(_userCulture);
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.WorkCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.GetStickerByType(nameof(StickersId.PetWorkSticker_Cat), _userPetType),
                InlineKeyboardMarkup = toSendInline,
                ReplyMarkup = ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture)
            };

            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task GoToFarm(Pet petDB)
        {
            Log.Debug($"Called /GoToFarm for {_userInfo}");
            var accessCheck = CheckStatusIsInactiveOrNull(petDB);
            if (accessCheck != null)
            {
                Log.Debug($"Pet is busy for {_userInfo}");
                await _appServices.BotControlService.SendAnswerMessageAsync(new AnswerMessage()
                {
                    Text = nameof(petIsBusyForFarm).UseCulture(_userCulture),
                    ReplyMarkup = ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture),
                    StickerId = StickersId.GetStickerByType(nameof(StickersId.PetBusySticker_Cat), _userPetType),
                }, _userId, false);
                return;
            }

            var encodedPetName = HttpUtility.HtmlEncode(petDB.Name);
            encodedPetName = "<b>" + encodedPetName + "</b>";

            var userDB = _appServices.UserService.Get(_userId);
            var nextFeed = new CronExpression(Constants.CronSchedule.AutoFeedCron).GetNextValidTimeAfter(DateTime.UtcNow) ?? DateTime.UtcNow;
            var timeRemaining = nextFeed - DateTime.UtcNow;

            string toSendText = petDB.IsAutoFeedEnabled
                    ? string.Format(nameof(farmCommand_ENABLED).UseCulture(_userCulture),
                                              encodedPetName,
                                              userDB.AutoFeedCharges,
                                              (int)timeRemaining.TotalHours,
                                              timeRemaining.Minutes,
                                              string.Format(nameof(turnedOn_F).UseCulture(_userCulture)))
                    : string.Format(nameof(farmCommand_DISABLED).UseCulture(_userCulture),
                                              encodedPetName,
                                              userDB.AutoFeedCharges,
                                              string.Format(nameof(turnedOff_F).UseCulture(_userCulture)));

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.FarmCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            InlineKeyboardMarkup toSendInline = userDB.AutoFeedCharges > 0
                ? Extensions.InlineKeyboardOptimizer(InlineItems.InlineFarm(_userCulture), 1)
                : null;

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.FarmSticker,
                ReplyMarkup = ReplyKeyboardItems.FarmKeyboardMarkup(_userCulture),
                InlineKeyboardMarkup = toSendInline,
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }

        private async Task SetAutoFeedStatus(Pet pet, bool isEnabled)
        {
            var userDB = _appServices.UserService.Get(_userId);
            if (userDB.AutoFeedCharges <= 0 && isEnabled)
                return;

            pet.IsAutoFeedEnabled = isEnabled;
            _appServices.PetService.Update(_userId, pet);

            string answerText = isEnabled
                ? nameof(AutoFeedEnabledCallbackAnswer).UseCulture(_userCulture)
                : nameof(AutoFeedDisabledCallbackAnswer).UseCulture(_userCulture);

            await _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, answerText, false);

            // Update message text
            var encodedPetName = HttpUtility.HtmlEncode(pet.Name);
            encodedPetName = "<b>" + encodedPetName + "</b>";

            var nextFeed = new CronExpression(Constants.CronSchedule.AutoFeedCron).GetNextValidTimeAfter(DateTime.UtcNow) ?? DateTime.UtcNow;
            var timeRemaining = nextFeed - DateTime.UtcNow;

            string toSendText = isEnabled
                    ? string.Format(nameof(farmCommand_ENABLED).UseCulture(_userCulture),
                                              encodedPetName,
                                              userDB.AutoFeedCharges,
                                              (int)timeRemaining.TotalHours,
                                              timeRemaining.Minutes,
                                              string.Format(nameof(turnedOn_F).UseCulture(_userCulture)))
                    : string.Format(nameof(farmCommand_DISABLED).UseCulture(_userCulture),
                                              encodedPetName,
                                              userDB.AutoFeedCharges,
                                              string.Format(nameof(turnedOff_F).UseCulture(_userCulture)));

            InlineKeyboardMarkup toSendInline = userDB.AutoFeedCharges > 0
               ? Extensions.InlineKeyboardOptimizer(InlineItems.InlineFarm(_userCulture), 1)
               : null;

            await _appServices.BotControlService.EditMessageTextAsync(_userId,
                                                                    _callback.Message.MessageId,
                                                                    toSendText,
                                                                    toSendInline,
                                                                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
        }

        private async Task ShowRewardInfo(User userDB)
        {
            Log.Debug($"Called /ShowRewardInfo for {_userInfo}");

            string toSendText = string.Empty;

            List<CallbackModel> inlineParts;
            InlineKeyboardMarkup toSendInline = default;

            if (userDB.GotDailyRewardTime.AddHours(TimesToWait.DailyRewardToWait.TotalHours) > DateTime.UtcNow)
            {
                TimeSpan remainsTime = TimesToWait.DailyRewardToWait - (DateTime.UtcNow - userDB.GotDailyRewardTime);

                if (remainsTime > TimeSpan.Zero)
                {
                    await _appServices.BotControlService.SendAnswerMessageAsync(GetRemainedTimeDailyReward(remainsTime), _userId, false);
                    return;
                }
            }
            else
            {
                toSendText = nameof(rewardCommand).UseCulture(_userCulture);

                inlineParts = InlineItems.InlineRewards(_userCulture);
                toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

                var aud = _appServices.AllUsersDataService.Get(_userId);
                aud.RewardCommandCounter++;
                _appServices.AllUsersDataService.Update(aud);
            }

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.DailyRewardSticker,
                InlineKeyboardMarkup = toSendInline,
                ReplyMarkup = ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture)
            };

            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task ChangeLanguageCmd()
        {
            _appServices.UserService.UpdateLanguage(_userId, null);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.LanguageCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = nameof(ChangeLanguage).UseCulture(_userCulture),
                StickerId = StickersId.ChangeLanguageSticker,
                ReplyMarkup = Constants.ReplyKeyboardItems.LanguagesMarkup,
                InlineKeyboardMarkup = null
            };
            Log.Debug($"Called /ChangeLanugage for {_userInfo}");
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }

        private async Task AnswerByGemini(Pet petDB, User userDB)
        {
            await _appServices.BotControlService.SendChatActionAsync(_userId, Telegram.Bot.Types.Enums.ChatAction.Typing);

            var previousQA = _appServices.MetaUserService.GetLastGeminiQA(_userId);
            bool isTimeOut;
            string geminiAnswer;

            if (IsGeminiTimeout(previousQA))
            {
                isTimeOut = true;
                geminiAnswer = string.Format(
                    nameof(ChatGPTTimeOutText).UseCulture(_userCulture),
                    Extensions.GetTypeEmoji(petDB.Type),
                    HttpUtility.HtmlEncode(petDB.Name)
                    );
            }
            else
            {
                var (answer, isCanceled) = await GetAnswerGemini(_message.Text, petDB, userDB);
                geminiAnswer = $"{Extensions.GetLongTypeEmoji(_userPetType, _userCulture)} <b>{HttpUtility.HtmlEncode(petDB.Name)}</b>: ";
                geminiAnswer += answer;
                isTimeOut = isCanceled;
            }


            var toSend = new AnswerMessage()
            {
                Text = geminiAnswer,
                replyToMsgId = _message.MessageId,
                ReplyMarkup = isTimeOut ? ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture) : null,
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
            };

            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, true, true);
        }

        private async Task BuyAutoFeedCMD(User userDB, Pet petDB)
        {
            if (userDB.Diamonds < Constants.Costs.AutoFeedCostDiamonds)
            {
                var toSendErr = new AnswerMessage()
                {
                    Text = nameof(Resources.Resources.notEnoughDiamonds).UseCulture(_userCulture),
                    ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
                };
                await _appServices.BotControlService.SendAnswerMessageAsync(toSendErr, _userId, false);
                return;
            }

            _appServices.DiamondService.UpdateDiamonds(_userId, userDB.Diamonds - Constants.Costs.AutoFeedCostDiamonds);
            _appServices.UserService.UpdateAutoFeedCharges(_userId, userDB.AutoFeedCharges + Constants.AutoFeed.AutoFeedChargesInitial);
            _appServices.PetService.UpdateIsAutoFeedEnabled(_userId, true);

            var toSend = new AnswerMessage()
            {
                Text = string.Format(nameof(autoFeedBought).UseCulture(_userCulture), userDB.AutoFeedCharges + Constants.AutoFeed.AutoFeedChargesInitial),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }

        private async Task<(string answer, bool isCanceled)> GetAnswerGemini(string textToAnswer, Pet petDB, User userDB)
        {
            var geminiKey = _appServices.SInfoService.GetGeminiKey();
            if (string.IsNullOrEmpty(geminiKey))
                return ("Gemini API key is not set", true);

            string result;
            bool isCanceled = false;
            try
            {
                string promptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Prompts", "GeminiPetRoleplay.txt");
                string systemPrompt = await System.IO.File.ReadAllTextAsync(promptPath);

                systemPrompt = systemPrompt
                    .Replace("{{Name}}", petDB.Name)
                    .Replace("{{HP}}", petDB.HP.ToString())
                    .Replace("{{Satiety}}", petDB.Satiety.ToString("F0"))
                    .Replace("{{Hygiene}}", petDB.Hygiene.ToString())
                    .Replace("{{Fatigue}}", petDB.Fatigue.ToString())
                    .Replace("{{Joy}}", petDB.Joy.ToString())
                    .Replace("{{Level}}", petDB.Level.ToString())
                    .Replace("{{CurrentStatus}}", Extensions.GetCurrentStatus(petDB.CurrentStatus, _userCulture))
                    .Replace("{{Gold}}", userDB.Gold.ToString())
                    .Replace("{{Language}}", _userCulture.DisplayName)
                    .Replace("{{Diamonds}}", userDB.Diamonds.ToString());

                var previousQA = _appServices.MetaUserService.GetLastGeminiQA(_userId);
                int historyLimit = _appServices.SInfoService.GetGeminiMaxHistory();

                var contentsList = new List<object>();

                //Add history
                //Skipping older ones if we want to limit context window sent to API, similar to ChatGPT QA_TO_FEED_COUNTER
                foreach (var item in previousQA.Skip(Math.Max(0, previousQA.Count - Constants.QA_TO_FEED_COUNTER)))
                {
                    contentsList.Add(new { role = "user", parts = new[] { new { text = item.userQ } } });
                    contentsList.Add(new { role = "model", parts = new[] { new { text = item.geminiA } } });
                }

                //Add current message
                contentsList.Add(new { role = "user", parts = new[] { new { text = textToAnswer } } });

                var prompt = new
                {
                    systemInstruction = new
                    {
                        parts = new[] { new { text = systemPrompt } }
                    },
                    contents = contentsList.ToArray()
                };

                using var client = new HttpClient();
                var response = await client.PostAsJsonAsync(
                    $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent?key={geminiKey}",
                    prompt);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(responseString);
                    result = jsonDoc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    //Save to history
                    _appServices.MetaUserService.AppendNewGeminiQA(_userId, textToAnswer, result, historyLimit);
                }
                else
                {
                    result = $"Error: {response.StatusCode}";
                    isCanceled = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GEMINI ERROR");
                isCanceled = true;
                result = nameof(ChatgptErrorAnswerText).UseCulture(_userCulture);
            }

            return (result, isCanceled);
        }

        [Obsolete]
        private async Task SendGeminiMessage(Pet petDB, User userDB)
        {
            string question = _message.Text
                .Replace("/gemini", "")
                .Replace("@test_tmg_bot", "") //hardcoded for now as it is not critical
                .Trim();

            if (string.IsNullOrEmpty(question))
            {
                var toSend = new AnswerMessage()
                {
                    Text = "daroŭ",
                    StickerId = null,
                    ReplyMarkup = ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture),
                    InlineKeyboardMarkup = null
                };
                Log.Debug($"Called /gemini for {_userInfo}");
                await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
                return;
            }

            await _appServices.BotControlService.SendChatActionAsync(_userId, Telegram.Bot.Types.Enums.ChatAction.Typing);
            var (answer, isCanceled) = await GetAnswerGemini(question, petDB, userDB);

            if (isCanceled)
            {
                await _appServices.BotControlService.SendAnswerMessageAsync(new AnswerMessage()
                {
                    Text = answer,
                    ReplyMarkup = ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture)
                }, _userId, false);
            }
            else
            {
                await _appServices.BotControlService.SendAnswerMessageAsync(new AnswerMessage()
                {
                    Text = answer,
                    ParseMode = Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    ReplyMarkup = ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture)
                }, _userId, false);
            }
        }
        private string BuildPetInfoText(Pet petDB, User userDB, string randomAd, string randomPetPhrase)
        {
            var encodedPetName = HttpUtility.HtmlEncode(petDB.Name);
            encodedPetName = "<b>" + encodedPetName + "</b>";

            return string.Format(
                nameof(petCommand).UseCulture(_userCulture),
                encodedPetName,
                petDB.HP,
                petDB.EXP,
                petDB.Level,
                petDB.Satiety,
                petDB.Fatigue,
                Extensions.GetCurrentStatus(petDB.CurrentStatus, _userCulture),
                petDB.Joy,
                userDB.Gold,
                petDB.Hygiene,
                petDB.Level * Factors.ExpToLvl,
                Extensions.GetLongTypeEmoji(_userPetType, _userCulture),
                petDB.IsAutoFeedEnabled
                    ? string.Format(nameof(autoFeederUserStatus).UseCulture(_userCulture), string.Format(nameof(turnedOn_F).UseCulture(_userCulture)), userDB.AutoFeedCharges)
                    : string.Format(nameof(autoFeederUserStatus).UseCulture(_userCulture), string.Format(nameof(turnedOff_F).UseCulture(_userCulture)), userDB.AutoFeedCharges),
                userDB.Diamonds,
                randomAd,
                randomPetPhrase
            );
        }

        private string GetRandomAd(bool isDailyRewardOnCooldown)
        {
            var ads = new List<string>
            {
                nameof(petCommand_ads_1).UseCulture(_userCulture),
                nameof(petCommand_ads_2).UseCulture(_userCulture),
                nameof(petCommand_ads_3).UseCulture(_userCulture),
                nameof(petCommand_ads_4).UseCulture(_userCulture),
                nameof(petCommand_ads_5).UseCulture(_userCulture)
            };

            if (isDailyRewardOnCooldown)
                ads.Remove(nameof(petCommand_ads_1).UseCulture(_userCulture));

            return ads[new Random().Next(ads.Count)];
        }

        private async Task ShowPetInfo(Pet petDB)
        {
            var userDB = _appServices.UserService.Get(_userId);
            var dateTimeWhenOver = userDB.GotDailyRewardTime.Add(TimesToWait.DailyRewardToWait);
            bool isDailyRewardOnCooldown = dateTimeWhenOver > DateTime.UtcNow;
            var randomAd = GetRandomAd(isDailyRewardOnCooldown);

            var previousQA = _appServices.MetaUserService.GetLastGeminiQA(_userId);
            var randomPetPhrase = IsGeminiTimeout(previousQA) ? "..." : GetRandomPetPhrase();

            string toSendText = BuildPetInfoText(petDB, userDB, randomAd, randomPetPhrase);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.PetCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage
            {
                Text = toSendText,
                StickerId = StickersId.GetStickerByType(nameof(StickersId.PetInfoSticker_Cat), _userPetType),
                ReplyMarkup = ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture),
                InlineKeyboardMarkup = Extensions.InlineKeyboardOptimizer(InlineItems.InlinePet(_userCulture)),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
            };
            Log.Debug($"Called /ShowPetInfo for {_userInfo}");
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }

        private string GetRandomPetPhrase()
        {
            var phrases = new List<string>
            {
                nameof(petCommand_phrase_1).UseCulture(_userCulture),
                nameof(petCommand_phrase_2).UseCulture(_userCulture),
                nameof(petCommand_phrase_3).UseCulture(_userCulture),
                nameof(petCommand_phrase_4).UseCulture(_userCulture),
                nameof(petCommand_phrase_5).UseCulture(_userCulture),
                nameof(petCommand_phrase_6).UseCulture(_userCulture)
            };

            return phrases[new Random().Next(phrases.Count)];
        }

        private AnswerMessage CheckStatusIsInactiveOrNull(Pet petDB, bool IsGoToSleepCommand = false, bool IsGoToWorkCommand = false)
        {
            if (petDB.CurrentStatus == (int)CurrentStatus.Sleeping && !IsGoToSleepCommand)
            {
                string denyText = nameof(denyAccessSleeping).UseCulture(_userCulture);
                return new AnswerMessage()
                {
                    Text = denyText,
                    StickerId = StickersId.GetStickerByType(nameof(StickersId.PetBusySticker_Cat), _userPetType),
                };
            }

            if (petDB.CurrentStatus == (int)CurrentStatus.Working && !IsGoToWorkCommand)
            {
                string denyText = nameof(denyAccessWorking).UseCulture(_userCulture);
                return new AnswerMessage()
                {
                    Text = denyText,
                    StickerId = StickersId.GetStickerByType(nameof(StickersId.PetBusySticker_Cat), _userPetType),
                };
            }

            return null;
        }
        private async Task GoToBathroom(Pet petDB)
        {
            Log.Debug($"Called /GoToBathroom for {_userInfo}");
            var accessCheck = CheckStatusIsInactiveOrNull(petDB);
            if (accessCheck != null)
            {
                Log.Debug($"Pet is busy for {_userInfo}");
                await _appServices.BotControlService.SendAnswerMessageAsync(accessCheck, _userId, false);
                return;
            }

            string toSendText = string.Format(
                nameof(bathroomCommand).UseCulture(_userCulture),
                petDB.Hygiene);

            List<CallbackModel> inlineParts = InlineItems.InlineHygiene(_userCulture);
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.BathroomCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.GetStickerByType(nameof(StickersId.PetBathroomSticker_Cat), _userPetType),
                InlineKeyboardMarkup = toSendInline,
                ReplyMarkup = ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture)
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task GoToKitchen(Pet petDB, User userDB)
        {
            Log.Debug($"Called /GoToKitchen for {_userInfo}");
            var accessCheck = CheckStatusIsInactiveOrNull(petDB);
            if (accessCheck != null)
            {
                Log.Debug($"Pet is busy for {_userInfo}");
                await _appServices.BotControlService.SendAnswerMessageAsync(accessCheck, _userId, false);
                return;
            }

            string toSendText = string.Format(
                nameof(kitchenCommand).UseCulture(_userCulture),
                petDB.Satiety,
                userDB.Gold);

            List<CallbackModel> inlineParts = InlineItems.InlineFood;
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.KitchenCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.GetStickerByType(nameof(StickersId.PetKitchenSticker_Cat), _userPetType),
                InlineKeyboardMarkup = toSendInline,
                ReplyMarkup = ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture)
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task GoToGameroom(Pet petDB, User userDB)
        {
            Log.Debug($"Called /GoToGameroom for {_userInfo}");
            var accessCheck = CheckStatusIsInactiveOrNull(petDB);
            if (accessCheck != null)
            {
                Log.Debug($"Pet is busy for {_userInfo}");
                await _appServices.BotControlService.SendAnswerMessageAsync(accessCheck, _userId, false);
                return;
            }

            string toSendText = string.Format(nameof(gameroomCommand).UseCulture(_userCulture),
                                              petDB.Fatigue,
                                              petDB.Joy,
                                              userDB.Gold,
                                              Factors.CardGameJoyFactor,
                                              Costs.AppleGame,
                                              Factors.DiceGameJoyFactor,
                                              Costs.DiceGame,
                                              Factors.TicTacToeGameJoyFactor,
                                              Costs.TicTacToeGame);

            List<CallbackModel> inlineParts = InlineItems.InlineGames;
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.GameroomCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.GetStickerByType(nameof(StickersId.PetGameroomSticker_Cat), _userPetType),
                InlineKeyboardMarkup = toSendInline,
                ReplyMarkup = ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture)
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task GoToHospital(Pet petDB)
        {
            Log.Debug($"Called /GoToHospital for {_userInfo}");
            var accessCheck = CheckStatusIsInactiveOrNull(petDB);
            if (accessCheck != null)
            {
                Log.Debug($"Pet is busy for {_userInfo}");
                await _appServices.BotControlService.SendAnswerMessageAsync(accessCheck, _userId, false);
                return;
            }

            string commandHospital = petDB.HP switch
            {
                >= 80 => nameof(hospitalCommandHighHp).UseCulture(_userCulture),
                > 20 and < 80 => nameof(hospitalCommandMidHp).UseCulture(_userCulture),
                _ => nameof(hospitalCommandLowHp).UseCulture(_userCulture)
            };

            string stickerHospital = petDB.HP switch
            {
                >= 80 => StickersId.GetStickerByType(nameof(StickersId.PetHospitalHighHPSticker_Cat), _userPetType),
                > 20 and < 80 => StickersId.GetStickerByType(nameof(StickersId.PetHospitalMidHPSticker_Cat), _userPetType),
                _ => StickersId.GetStickerByType(nameof(StickersId.PetHospitalLowHPSticker_Cat), _userPetType),
            };

            string toSendText = string.Format(commandHospital, petDB.HP);

            List<CallbackModel> inlineParts = InlineItems.InlineHospital(_userCulture);
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.HospitalCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = stickerHospital,
                InlineKeyboardMarkup = toSendInline,
                ReplyMarkup = ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture)
            };

            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task ShowRankingInfo()
        {
            Log.Debug($"Called /ShowRankingInfo for {_userInfo}");

            var anwserRating = GetRanksByLevel();

            if (anwserRating == null)
                return;

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.RanksCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = anwserRating,
                StickerId = StickersId.GetStickerByType(nameof(StickersId.PetRanksSticker_Cat), _userPetType),
                InlineKeyboardMarkup = Extensions.InlineKeyboardOptimizer(InlineItems.InlineRanks(_userCulture), 3),
                ReplyMarkup = ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
            };

            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task GoToSleep(Pet petDB)
        {
            Log.Debug($"Called /GoToSleep for {_userInfo}");

            var accessCheck = CheckStatusIsInactiveOrNull(petDB, true);
            if (accessCheck != null)
            {
                Log.Debug($"Pet is busy for {_userInfo}");
                await _appServices.BotControlService.SendAnswerMessageAsync(accessCheck, _userId, false);
                return;
            }

            string toSendText = string.Format(nameof(sleepCommand).UseCulture(_userCulture),
                                              _userPetEmoji,
                                              petDB.Name,
                                              petDB.Fatigue,
                                              Extensions.GetCurrentStatus(petDB.CurrentStatus, _userCulture));

            InlineKeyboardMarkup toSendInline;
            if (petDB.CurrentStatus == (int)CurrentStatus.Sleeping)
            {
                var ticksToWait = (petDB.ToWakeUpTime - DateTime.UtcNow).Ticks;
                string timeToWaitStr = string.Format(
                    nameof(sleepCommandInlineShowTime).UseCulture(_userCulture),
                    new DateTime().AddTicks(ticksToWait).ToString("HH:mm:ss"));

                toSendInline = Extensions.InlineKeyboardOptimizer(
                    new List<CallbackModel>()
                    {
                        new CallbackModel()
                        {
                            Text = timeToWaitStr,
                            CallbackData = CallbackButtons.SleepCommand.SleepCommandInlinePutToSleep(default, _userCulture).CallbackData
                        }
                    });
            }
            else
                toSendInline = Extensions.InlineKeyboardOptimizer(
                    new List<CallbackModel>()
                    {
                        new CallbackModel()
                        {
                            Text = nameof(sleepCommandInlinePutToSleep).UseCulture(_userCulture),
                            CallbackData = CallbackButtons.SleepCommand.SleepCommandInlinePutToSleep(default, _userCulture).CallbackData
                        }
                    });

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.SleepCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.GetStickerByType(nameof(StickersId.PetSleepSticker_Cat), _userPetType),
                InlineKeyboardMarkup = toSendInline,
                ReplyMarkup = ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture)
            };

            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task ShowChangelogsInfo()
        {
            string linkToDiscussChat = "https://t.me/news_virtualpetbot";
            string toSendText = string.Format(
                nameof(changelogCommand).UseCulture(_userCulture),
                linkToDiscussChat);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.ChangelogCommandSticker,
                InlineKeyboardMarkup = new InlineKeyboardButton(nameof(ChangelogGoToDicussChannelButton).UseCulture(_userCulture))
                {
                    Url = linkToDiscussChat
                },
                ReplyMarkup = ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture)
            };
            Log.Debug($"Called /ShowChangelogsInfo for {_userInfo}");
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task ShowHelpInfo()
        {
            string toSendText = nameof(helpCommand).UseCulture(_userCulture);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.HelpCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.HelpCommandSticker,
                ReplyMarkup = ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture)
            };
            Log.Debug($"Called /ShowHelpInfo for {_userInfo}");
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task ShowReferalInfo()
        {
            Log.Debug($"Called /ShowReferalInfo for {_userInfo}");
            var botUsername = (await _appServices.BotControlService.GetBotUserInfo()).Username;

            var refAmounts = _appServices.ReferalInfoService.GetDoneRefsAmount(_userId);
            var goldByRef = Rewards.ReferalAdded * refAmounts;
            var refLink = Extensions.GetReferalLink(_userId, botUsername);
            string toSendText = string.Format(
                nameof(referalCommand).UseCulture(_userCulture),
                refAmounts,
                goldByRef,
                refLink,
                Rewards.ReferalAdded);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.MenuCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.ReferalCommandSticker,
                InlineKeyboardMarkup = new InlineKeyboardMarkup(new List<InlineKeyboardButton>()
                {
                    new InlineKeyboardButton(CallbackButtons.ReferalCommand.ToAddToNewGroupReferalCommand(_userCulture).Text)
                    {
                        Url = $"http://t.me/{botUsername}?startgroup=start"
                    },
                    new InlineKeyboardButton(CallbackButtons.ReferalCommand.ToShareReferalCommand(_userCulture).Text)
                    {
                        Url = $"https://t.me/share/url?url={Extensions.GetReferalLink(_userId, botUsername)}"
                    }
                }),
                ReplyMarkup = ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task ShowMenuInfo()
        {
            Log.Debug($"Called /ShowMenuInfo for {_userInfo}");

            string toSendText = string.Format(nameof(menuCommand).UseCulture(_userCulture), _userPetEmoji);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.MenuCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSend = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.MenuCommandSticker,
                ReplyMarkup = ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task RenamePet()
        {
            Log.Debug($"Called /RenamePet for {_userInfo}");

            if (_appServices.BannedUsersService.GetAll().Exists(bs => bs.UserId == _userId && bs.IsRenameBanned))
            {
                Log.Debug($"Banned for renaming for {_userInfo}");

                string toSendTextBan = string.Format(nameof(renameBannedCommand).UseCulture(_userCulture));

                var audF = _appServices.AllUsersDataService.Get(_userId);
                audF.RenameCommandCounter++;
                _appServices.AllUsersDataService.Update(audF);

                var toSend = new AnswerMessage() { Text = toSendTextBan, StickerId = StickersId.BannedSticker };
                await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
                return;
            }

            string toSendText = string.Format(nameof(renameCommand).UseCulture(_userCulture));

            _appServices.MetaUserService.UpdateIsPetNameAskedOnRename(_userId, true);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.RenameCommandCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var toSendFinal = new AnswerMessage()
            {
                Text = toSendText,
                StickerId = StickersId.RenamePetSticker,
                ReplyMarkup = new ReplyKeyboardRemove()
            };

            await _appServices.BotControlService.SendAnswerMessageAsync(toSendFinal, _userId, false);
        }
        #endregion

        #region Inline Answers
        private async Task ShowBasicInfoInline(Pet petDB)
        {
            var userDB = _appServices.UserService.Get(_userId);
            var dateTimeWhenOver = userDB.GotDailyRewardTime.Add(TimesToWait.DailyRewardToWait);
            bool isDailyRewardOnCooldown = dateTimeWhenOver > DateTime.UtcNow;
            var randomAd = GetRandomAd(isDailyRewardOnCooldown);
            var randomPetPhrase = GetRandomPetPhrase();

            string toSendText = BuildPetInfoText(petDB, userDB, randomAd, randomPetPhrase);

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>
            {
                CallbackButtons.PetCommand.PetCommandInlineExtraInfo(_userCulture)
            });

            Log.Debug($"Callbacked ShowBasicInfoInline for {_userInfo}");
            await _appServices.BotControlService.SendAnswerCallback(
                _userId,
                _callback?.Message?.MessageId ?? 0,
                new AnswerCallback(toSendText, toSendInline, Telegram.Bot.Types.Enums.ParseMode.Html),
                false
            );
        }
        private async Task ShowExtraInfoInline(Pet petDB)
        {
            var encodedPetName = HttpUtility.HtmlEncode(petDB.Name);
            var audUser = _appServices.AllUsersDataService.Get(_userId);
            if (audUser == null)
                return;

            var petWasFetTimes = audUser.ChocolateEatenCounter +
                                 audUser.AppleEatenCounter +
                                 audUser.BreadEatenCounter +
                                 audUser.LollypopEatenCounter;

            var petWasSleptMinutes = (int)(audUser.SleepenTimesCounter * Constants.TimesToWait.SleepToWait.TotalMinutes);
            var petWasWorkingMinutes = (int)((audUser.WorkFlyersCounter * Constants.TimesToWait.FlyersDistToWait.TotalMinutes) + (audUser.WorkOnPCCounter + TimesToWait.WorkOnPCToWait.TotalMinutes));

            encodedPetName = "<b>" + encodedPetName + "</b>";
            string toSendText = string.Format(
                nameof(petCommandMoreInfo1).UseCulture(_userCulture),
                _userPetEmoji,
                encodedPetName,
                petDB.BirthDateTime,
                _appServices.ReferalInfoService.GetDoneRefsAmount(_userId),
                petWasFetTimes,
                audUser.AppleGamePlayedCounter,
                petWasSleptMinutes,
                petWasWorkingMinutes,
                (int)((DateTime.UtcNow - petDB.BirthDateTime).TotalDays)
                );
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
            {
                CallbackButtons.PetCommand.PetCommandInlineBasicInfo(_userCulture)
            });

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.ExtraInfoShowedTimesCounter++;
            _appServices.AllUsersDataService.Update(aud);

            Log.Debug($"Callbacked ShowExtraInfoInline for {_userInfo}");
            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline, Telegram.Bot.Types.Enums.ParseMode.Html),
                                                              false);
        }

        private async Task PetIsNotHungry()
        {
            Log.Debug($"Sent alert PetIsNotHungry for {_userInfo}");

            string answerLocal = string.Format(nameof(tooManyStarvingCommand).UseCulture(_userCulture));
            await _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, answerLocal, true);
        }
        private async Task NotEnoughGold()
        {
            Log.Debug($"Sent alert NotEnoughGold for {_userInfo}");

            string anwserLocal = string.Format(nameof(goldNotEnough).UseCulture(_userCulture));
            await _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwserLocal, true);
        }

        private async Task PetIsTooTired(JobType job = JobType.None)
        {
            Log.Debug($"Sent alert PetIsTooTired for {_userInfo}");

            string anwserLocal = job switch
            {
                JobType.WorkingOnPC => string.Format(nameof(tooTiredForJobPC).UseCulture(_userCulture), Factors.WorkOnPCFatigueFactor),
                JobType.FlyersDistributing => string.Format(nameof(tooTiredForJobFlyers).UseCulture(_userCulture), Factors.FlyersDistributingFatigueFactor),
                _ => string.Format(nameof(tooTiredText).UseCulture(_userCulture))
            };
            await _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwserLocal, true);
        }

        private async Task PetIsFullOfJoy()
        {
            Log.Debug($"Sent alert PetIsFullOfJoy for {_userInfo}");

            string anwserLocal = string.Format(nameof(PetIsFullOfJoyText).UseCulture(_userCulture));
            await _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwserLocal, true);
        }

        private async Task TakeShowerInline(Pet petDB)
        {
            if (petDB.Hygiene >= 100)
            {
                await SendAlertToUser(nameof(PetIsCleanEnoughAlert).UseCulture(_userCulture), true);
                return;
            }

            var newHygiene = petDB.Hygiene + HygieneFactors.ShowerFactor;
            newHygiene = newHygiene > 100 ? 100 : newHygiene;

            _appServices.PetService.UpdateHygiene(_userId, newHygiene);
            _appServices.PetService.UpdateEXP(_userId, petDB.EXP + ExpForAction.Hygiene);

            string anwser = string.Format(nameof(PetHygieneAnwserCallback).UseCulture(_userCulture), HygieneFactors.ShowerFactor);
            await SendAlertToUser(anwser);

            string toSendText = string.Format(nameof(bathroomCommand).UseCulture(_userCulture), newHygiene);
            List<CallbackModel> inlineParts = InlineItems.InlineHygiene(_userCulture);
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            Log.Debug($"Callbacked TakeShowerInline for {_userInfo}");
            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }
        private async Task MakePooInline(Pet petDB)
        {
            if (petDB.Hygiene >= 100)
            {
                await SendAlertToUser(nameof(PetDoesntWantToPoo).UseCulture(_userCulture), true);
                return;
            }

            var newHygiene = petDB.Hygiene + HygieneFactors.PoopFactor;
            newHygiene = newHygiene > 100 ? 100 : newHygiene;

            _appServices.PetService.UpdateHygiene(_userId, newHygiene);
            _appServices.PetService.UpdateEXP(_userId, petDB.EXP + ExpForAction.Hygiene);

            string anwser = string.Format(nameof(PetHygieneAnwserCallback).UseCulture(_userCulture), HygieneFactors.PoopFactor);
            await SendAlertToUser(anwser);

            string toSendText = string.Format(nameof(bathroomCommand).UseCulture(_userCulture), newHygiene);
            List<CallbackModel> inlineParts = InlineItems.InlineHygiene(_userCulture);
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            Log.Debug($"Callbacked PoopInline for {_userInfo}");
            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }
        private async Task TeethInline(Pet petDB)
        {
            if (petDB.Hygiene >= 100)
            {
                await SendAlertToUser(nameof(PetIsCleanEnoughAlert).UseCulture(_userCulture), true);
                return;
            }

            var newHygiene = petDB.Hygiene + HygieneFactors.TeethFactor;
            newHygiene = newHygiene > 100 ? 100 : newHygiene;

            _appServices.PetService.UpdateHygiene(_userId, newHygiene);
            _appServices.PetService.UpdateEXP(_userId, petDB.EXP + ExpForAction.Hygiene);

            string anwser = string.Format(nameof(PetHygieneAnwserCallback).UseCulture(_userCulture), HygieneFactors.TeethFactor);
            await SendAlertToUser(anwser);

            string toSendText = string.Format(nameof(bathroomCommand).UseCulture(_userCulture), newHygiene);
            List<CallbackModel> inlineParts = InlineItems.InlineHygiene(_userCulture);
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            Log.Debug($"Callbacked TeethInline for {_userInfo}");
            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }

        private async Task<bool> ToContinueFeedingPet(Pet petDB, User userDB, int foodPrice)
        {
            if (petDB.Satiety >= 100)
            {
                await PetIsNotHungry();
                return false;
            }

            var newGold = userDB.Gold - foodPrice;
            if (newGold < 0)
            {
                await NotEnoughGold();
                return false;
            }

            return true;
        }
        private async Task FeedWithBreadInline(Pet petDB)
        {
            var userDB = _appServices.UserService.Get(_userId);

            if (!await ToContinueFeedingPet(petDB, userDB, Costs.Bread))
                return;

            var newGold = userDB.Gold - Costs.Bread;
            var newSatiety = Math.Round(petDB.Satiety + FoodFactors.BreadHungerFactor, 2);
            newSatiety = newSatiety > 100 ? 100 : newSatiety;

            _appServices.UserService.UpdateGold(_userId, newGold);
            _appServices.PetService.UpdateSatiety(_userId, newSatiety);
            _appServices.PetService.UpdateEXP(_userId, petDB.EXP + ExpForAction.FeedingBread);
            var aud = _appServices.AllUsersDataService.Get(_userId);

            aud.BreadEatenCounter++;
            aud.GoldSpentCounter += Costs.Bread;
            _appServices.AllUsersDataService.Update(aud);

            string anwser = string.Format(nameof(PetFeedingAnwserCallback).UseCulture(_userCulture), (int)FoodFactors.BreadHungerFactor);
            await SendAlertToUser(anwser);

            string toSendText = string.Format(nameof(kitchenCommand).UseCulture(_userCulture), newSatiety, newGold);
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(InlineItems.InlineFood, 3);

            Log.Debug($"Callbacked FeedWithBreadInline for {_userInfo}");
            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }
        private async Task FeedWithAppleInline(Pet petDB)
        {
            var userDB = _appServices.UserService.Get(_userId);

            if (!await ToContinueFeedingPet(petDB, userDB, Costs.Apple))
                return;

            var newGold = userDB.Gold - Constants.Costs.Apple;
            var newSatiety = Math.Round(petDB.Satiety + FoodFactors.RedAppleHungerFactor, 2);
            newSatiety = newSatiety > 100 ? 100 : newSatiety;

            _appServices.UserService.UpdateGold(_userId, newGold);
            _appServices.PetService.UpdateSatiety(_userId, newSatiety);
            _appServices.PetService.UpdateEXP(_userId, petDB.EXP + ExpForAction.FeedingApple);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.AppleEatenCounter++;
            aud.GoldSpentCounter += Costs.Apple;
            _appServices.AllUsersDataService.Update(aud);

            await SendAlertToUser(string.Format(nameof(PetFeedingAnwserCallback).UseCulture(_userCulture), (int)FoodFactors.RedAppleHungerFactor));

            string toSendText = string.Format(nameof(kitchenCommand).UseCulture(_userCulture), newSatiety, newGold);
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(InlineItems.InlineFood, 3);

            Log.Debug($"Callbacked FeedWithAppleInline for {_userInfo}");
            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }
        private async Task FeedWithChocolateInline(Pet petDB)
        {
            var userDB = _appServices.UserService.Get(_userId);

            if (!await ToContinueFeedingPet(petDB, userDB, Costs.Chocolate))
                return;

            var newGold = userDB.Gold - Constants.Costs.Chocolate;
            var newSatiety = Math.Round(petDB.Satiety + FoodFactors.ChocolateHungerFactor, 2);
            newSatiety = newSatiety > 100 ? 100 : newSatiety;

            _appServices.UserService.UpdateGold(_userId, newGold);
            _appServices.PetService.UpdateSatiety(_userId, newSatiety);
            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.ChocolateEatenCounter++;
            aud.GoldSpentCounter += Constants.Costs.Chocolate;
            _appServices.AllUsersDataService.Update(aud);

            string anwser = string.Format(nameof(PetFeedingAnwserCallback).UseCulture(_userCulture), (int)FoodFactors.ChocolateHungerFactor);
            await SendAlertToUser(anwser);

            string toSendText = string.Format(nameof(kitchenCommand).UseCulture(_userCulture), newSatiety, newGold);
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(InlineItems.InlineFood, 3);

            Log.Debug($"Callbacked FeedWithChocolateInline for {_userInfo}");
            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }
        private async Task FeedWithLollipopInline(Pet petDB)
        {
            var userDB = _appServices.UserService.Get(_userId);

            if (!await ToContinueFeedingPet(petDB, userDB, Costs.Lollipop))
                return;

            var newGold = userDB.Gold - Constants.Costs.Lollipop;
            var newSatiety = Math.Round(petDB.Satiety + FoodFactors.LollipopHungerFactor, 2);
            newSatiety = newSatiety > 100 ? 100 : newSatiety;

            _appServices.UserService.UpdateGold(_userId, newGold);
            _appServices.PetService.UpdateSatiety(_userId, newSatiety);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.LollypopEatenCounter++;
            aud.GoldSpentCounter += Costs.Lollipop;
            _appServices.AllUsersDataService.Update(aud);

            string anwser = string.Format(nameof(PetFeedingAnwserCallback).UseCulture(_userCulture), (int)FoodFactors.LollipopHungerFactor);
            await SendAlertToUser(anwser);

            string toSendText = string.Format(nameof(kitchenCommand).UseCulture(_userCulture), newSatiety, newGold);
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(InlineItems.InlineFood, 3);

            Log.Debug($"Callbacked FeedWithLollipopInline for {_userInfo}");
            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }

        private async Task PutToSleepInline(Pet petDB)
        {
            Pet petResult = Pet.Clone(petDB);

            if (await CheckStatusIsInactive(petResult, true))
                return;

            if (petResult.CurrentStatus == (int)CurrentStatus.Sleeping)
            {
                await UpdateSleepingInline(petResult);
                return;
            }

            if (petResult.Fatigue < Limits.ToRestMinLimitOfFatigue)
            {
                await DeclineSleepingInline(petResult);
                return;
            }

            await StartSleepingInline(petResult);
        }
        private async Task StartSleepingInline(Pet petDB)
        {
            petDB.CurrentStatus = (int)CurrentStatus.Sleeping;
            petDB.StartSleepingTime = DateTime.UtcNow;
            petDB.ToWakeUpTime = DateTime.UtcNow + TimesToWait.SleepToWait;
            _appServices.PetService.Update(petDB.UserId, petDB);
            _appServices.PetService.UpdateEXP(_userId, petDB.EXP + ExpForAction.Sleep);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.SleepenTimesCounter++;
            _appServices.AllUsersDataService.Update(aud);


            var timeToWait = petDB.ToWakeUpTime - DateTime.UtcNow;
            string toSendText = string.Format(nameof(sleepCommand).UseCulture(_userCulture), _userPetEmoji, petDB.Name, petDB.Fatigue, Extensions.GetCurrentStatus(petDB.CurrentStatus, _userCulture));
            InlineKeyboardMarkup toSendInline =
                Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
                {
                    CallbackButtons.SleepCommand.SleepCommandInlinePutToSleep(timeToWait, _userCulture)
                });

            Log.Debug($"Callbacked StartSleepingInline for {_userInfo}");
            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }
        private async Task DeclineSleepingInline(Pet petDB)
        {
            await _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, nameof(PetSleepingDoesntWantYetAnwserCallback).UseCulture(_userCulture));
            string sendTxt = string.Format(nameof(sleepCommand).UseCulture(_userCulture), _userPetEmoji, petDB.Name, petDB.Fatigue, Extensions.GetCurrentStatus(petDB.CurrentStatus, _userCulture));

            InlineKeyboardMarkup toSendInlineWhileActive =
                Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
                {
                    new CallbackModel()
                    {
                        Text = nameof(sleepCommandInlinePutToSleep).UseCulture(_userCulture),
                        CallbackData = CallbackButtons.SleepCommand.SleepCommandInlinePutToSleep(default, _userCulture).CallbackData
                    }
                });

            Log.Debug($"Callbacked DeclineSleepingInline for {_userInfo}");
            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback.Message?.MessageId ?? 0,
                                                              new AnswerCallback(sendTxt, toSendInlineWhileActive),
                                                              false);
        }
        private async Task UpdateSleepingInline(Pet petDB)
        {
            await _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, nameof(PetSleepingAlreadyAnwserCallback).UseCulture(_userCulture));

            string toSendText = string.Format(nameof(sleepCommand).UseCulture(_userCulture), _userPetEmoji, petDB.Name, petDB.Fatigue, Extensions.GetCurrentStatus(petDB.CurrentStatus, _userCulture));

            var timeToWait = petDB.ToWakeUpTime - DateTime.UtcNow;
            timeToWait = timeToWait < TimeSpan.Zero ? default : timeToWait;

            InlineKeyboardMarkup toSendInline =
                Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
                {
                    CallbackButtons.SleepCommand.SleepCommandInlinePutToSleep(timeToWait, _userCulture)
                });

            Log.Debug($"Callbacked UpdateSleepingInline for {_userInfo}");
            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }

        private async Task PlayCardInline(Pet petDB)
        {
            var userDB = _appServices.UserService.Get(_userId);

            var newGold = userDB.Gold - Costs.AppleGame;
            if (newGold < 0)
            {
                await NotEnoughGold();
                return;
            }

            var newFatigue = petDB.Fatigue + Factors.CardGameFatigueFactor;
            if (newFatigue > 100)
            {
                await PetIsTooTired();
                return;
            }

            if (petDB.Joy >= 100)
            {
                await PetIsFullOfJoy();
                return;
            }
            var newJoy = petDB.Joy + Factors.CardGameJoyFactor;
            newJoy = newJoy > 100 ? 100 : newJoy;

            _appServices.UserService.UpdateGold(_userId, newGold);
            _appServices.PetService.UpdateFatigue(_userId, newFatigue);
            _appServices.PetService.UpdateJoy(_userId, newJoy);

            string anwser = string.Format(nameof(PetPlayingAnwserCallback).UseCulture(_userCulture), Factors.CardGameFatigueFactor);
            await SendAlertToUser(anwser);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.CardsPlayedCounter++;
            _appServices.AllUsersDataService.Update(aud);
            _appServices.PetService.UpdateEXP(_userId, petDB.EXP + ExpForAction.Play);

            string toSendText = string.Format(nameof(gameroomCommand).UseCulture(_userCulture),
                                              newFatigue,
                                              newJoy,
                                              newGold,
                                              Factors.CardGameJoyFactor,
                                              Factors.CardGameJoyFactor,
                                              Costs.AppleGame,
                                              Factors.DiceGameJoyFactor,
                                              Costs.DiceGame,
                                              Factors.TicTacToeGameJoyFactor,
                                              Costs.TicTacToeGame);
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(InlineItems.InlineGames, 3);

            Log.Debug($"Callbacked PlayCardInline for {_userInfo}");
            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }
        private async Task PlayDiceInline(Pet petDB)
        {
            var userDB = _appServices.UserService.Get(_userId);

            var newGold = userDB.Gold - Costs.DiceGame;
            if (newGold < 0)
            {
                await NotEnoughGold();
                return;
            }

            var newFatigue = petDB.Fatigue + Factors.DiceGameFatigueFactor;
            if (newFatigue > 100)
            {
                await PetIsTooTired();
                return;
            }

            if (petDB.Joy >= 100)
            {
                await PetIsFullOfJoy();
                return;
            }
            var newJoy = petDB.Joy + Factors.DiceGameJoyFactor;
            newJoy = newJoy > 100 ? 100 : newJoy;

            _appServices.UserService.UpdateGold(_userId, newGold);
            _appServices.PetService.UpdateFatigue(_userId, newFatigue);
            _appServices.PetService.UpdateJoy(_userId, newJoy);
            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.DicePlayedCounter++;
            aud.GoldSpentCounter += Costs.DiceGame;
            _appServices.AllUsersDataService.Update(aud);
            _appServices.PetService.UpdateEXP(_userId, petDB.EXP + ExpForAction.Play);

            string anwser = string.Format(nameof(PetPlayingAnwserCallback).UseCulture(_userCulture), Factors.DiceGameJoyFactor);
            await _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwser);

            string toSendText = string.Format(nameof(gameroomCommand).UseCulture(_userCulture),
                                              newFatigue,
                                              newJoy,
                                              newGold,
                                              Factors.CardGameJoyFactor,
                                              Costs.AppleGame,
                                              Factors.DiceGameJoyFactor,
                                              Costs.DiceGame,
                                              Factors.TicTacToeGameJoyFactor,
                                              Costs.TicTacToeGame);
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(InlineItems.InlineGames, 3);

            Log.Debug($"Callbacked PlayDiceInline for {_userInfo}");
            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }
        private async Task StartWorkInline(Pet petDB, JobType jobType)
        {
            if (petDB == null)
                return;

            if (petDB.CurrentStatus == (int)CurrentStatus.Active)
            {
                if (jobType == JobType.WorkingOnPC)
                {
                    await StartWorkOnPC(petDB);
                    return;
                }

                if (jobType == JobType.FlyersDistributing)
                {
                    await StartJobFlyers(petDB);
                    return;
                }
            }
            else if (petDB.CurrentStatus == (int)CurrentStatus.Working)
            {
                if (petDB.CurrentJob == (int)JobType.WorkingOnPC)
                {
                    await ServeWorkOnPC(petDB);
                    return;
                }
                if (petDB.CurrentJob == (int)JobType.FlyersDistributing)
                {
                    await ServeJobFlyers(petDB);
                    return;
                }
            }
        }

        private async Task GetDailyRewardInline(User userDB)
        {
            var dateTimeWhenOver = userDB.GotDailyRewardTime.Add(TimesToWait.DailyRewardToWait);
            if (dateTimeWhenOver > DateTime.UtcNow)
            {
                Log.Debug($"Callbacked GetDailyRewardInline (still waiting) for {_userInfo}");
                await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                                  _callback.Message?.MessageId ?? 0,
                                                                  await ShowRemainedTimeDailyRewardCallback(dateTimeWhenOver - DateTime.UtcNow, true),
                                                                  false);
                return;
            }

            var newGold = userDB.Gold + Constants.Rewards.DailyGoldReward;

            _appServices.UserService.UpdateGold(_userId, newGold);
            _appServices.UserService.UpdateDailyRewardTime(_userId, DateTime.UtcNow);
            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.GoldEarnedCounter += Constants.Rewards.DailyGoldReward;
            _appServices.AllUsersDataService.Update(aud);

            string anwser = string.Format(nameof(DailyRewardAnwserCallback).UseCulture(_userCulture), Rewards.DailyGoldReward);
            await SendAlertToUser(anwser, true);

            Log.Debug($"Callbacked GetDailyRewardInline (default) for {_userInfo}");
            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              await ShowRemainedTimeDailyRewardCallback(new TimeSpan(23, 59, 59), false),
                                                              false);
        }

        private AnswerMessage GetRemainedTimeWork(TimeSpan remainedTime, JobType job)
        {
            AnswerMessage result = job switch
            {
                JobType.WorkingOnPC => new AnswerMessage()
                {
                    InlineKeyboardMarkup = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
                    {
                        CallbackButtons.WorkCommand.WorkCommandInlineShowTime(remainedTime, JobType.WorkingOnPC, _userCulture)
                    }),
                    Text = string.Format(nameof(workCommandPCWorking).UseCulture(_userCulture)),
                    StickerId = StickersId.GetStickerByType(nameof(StickersId.PetWorkOnPCSticker_Cat), _userPetType)
                },
                //DEFAULT, also Flyers job
                _ => new AnswerMessage()
                {
                    InlineKeyboardMarkup = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
                    {
                        CallbackButtons.WorkCommand.WorkCommandInlineShowTime(remainedTime, JobType.FlyersDistributing, _userCulture)
                    }),
                    Text = string.Format(nameof(workCommandFlyersWorking).UseCulture(_userCulture)),
                    StickerId = StickersId.GetStickerByType(nameof(StickersId.PetFlyersJobSticker_Cat), _userPetType)
                },
            };
            return result;
        }
        private async Task<AnswerCallback> ShowRemainedTimeWorkOnPCCallback(TimeSpan remainedTime = default)
        {
            InlineKeyboardMarkup toSendInline;
            string toSendText;

            if (remainedTime == default)
                remainedTime = new TimeSpan(0);

            string anwser = string.Format(nameof(workCommandPCWorking).UseCulture(_userCulture));
            await _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwser);

            toSendText = string.Format(nameof(workCommandPCWorking).UseCulture(_userCulture));
            toSendInline = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
            {
                CallbackButtons.WorkCommand.WorkCommandInlineShowTime(remainedTime, JobType.WorkingOnPC, _userCulture)
            });

            return new AnswerCallback(toSendText, toSendInline);
        }
        private async Task<AnswerCallback> ShowRemainedTimeJobFlyersCallback(TimeSpan remainedTime = default)
        {
            InlineKeyboardMarkup toSendInline;
            string toSendText;

            if (remainedTime == default)
                remainedTime = new TimeSpan(0);

            string anwser = string.Format(nameof(workCommandFlyersWorking).UseCulture(_userCulture));
            await _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwser);

            toSendText = string.Format(nameof(workCommandFlyersWorking).UseCulture(_userCulture));
            toSendInline = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
            {
                CallbackButtons.WorkCommand.WorkCommandInlineShowTime(remainedTime, JobType.FlyersDistributing, _userCulture)
            });

            return new AnswerCallback(toSendText, toSendInline);
        }

        private AnswerMessage GetRemainedTimeDailyReward(TimeSpan remainedTime)
        {
            InlineKeyboardMarkup toSendInline;
            string toSendText;

            toSendText = string.Format(nameof(rewardCommandDailyRewardGotten).UseCulture(_userCulture));
            string inlineStr = string.Format(nameof(rewardCommandDailyRewardInlineShowTime).UseCulture(_userCulture), new DateTime(remainedTime.Ticks).ToString("HH:mm:ss"));

            toSendInline = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
                    {
                        new CallbackModel()
                        {
                            Text = inlineStr,
                            CallbackData = CallbackButtons.RewardsCommand.RewardCommandDailyRewardInlineShowTime(default, _userCulture).CallbackData
                        }
                    });

            return new AnswerMessage() { InlineKeyboardMarkup = toSendInline, Text = toSendText, StickerId = StickersId.DailyRewardSticker, ReplyMarkup = ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture) };
        }
        private async Task<AnswerCallback> ShowRemainedTimeDailyRewardCallback(TimeSpan remainedTime = default, bool isAlert = false)
        {
            InlineKeyboardMarkup toSendInline;
            string toSendText;

            if (remainedTime == default)
                remainedTime = new TimeSpan(0);

            if (isAlert)
            {
                string anwser = string.Format(nameof(rewardCommandDailyRewardGotten).UseCulture(_userCulture));
                await SendAlertToUser(anwser, isAlert);
            }

            toSendText = string.Format(nameof(rewardCommandDailyRewardGotten).UseCulture(_userCulture));

            toSendInline = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
            {
                CallbackButtons.RewardsCommand.RewardCommandDailyRewardInlineShowTime(remainedTime, _userCulture)
            });

            return new AnswerCallback(toSendText, toSendInline);
        }

        private async Task CureWithPill(Pet petDB)
        {
            Log.Debug($"Callbacked CureWithPill for {_userInfo}");
            var newHP = petDB.HP + Factors.PillHPFactor;
            if (newHP > 100)
                newHP = 100;

            var newJoy = petDB.Joy + Factors.PillJoyFactor;
            if (newJoy < 0)
                newJoy = 0;

            _appServices.PetService.UpdateHP(_userId, newHP);
            _appServices.PetService.UpdateJoy(_userId, newJoy);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.PillEatenCounter++;
            _appServices.AllUsersDataService.Update(aud);

            string anwser = string.Format(nameof(PetCuringAnwserCallback).UseCulture(_userCulture), Factors.PillHPFactor, Factors.PillJoyFactor);
            await SendAlertToUser(anwser, true);

            string commandHospital = newHP switch
            {
                >= 80 => nameof(hospitalCommandHighHp).UseCulture(_userCulture),
                > 20 and < 80 => nameof(hospitalCommandMidHp).UseCulture(_userCulture),
                _ => nameof(hospitalCommandLowHp).UseCulture(_userCulture)
            };

            string toSendText = string.Format(commandHospital, newHP);
            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(InlineItems.InlineHospital(_userCulture));

            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }

        private async Task ShowRanksGold()
        {
            Log.Debug($"Callbacked ShowRanksGold for {_userInfo}");
            string toSendText = GetRanksByGold();
            if (toSendText == null)
                return;

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(InlineItems.InlineRanks(_userCulture), 3);

            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline, Telegram.Bot.Types.Enums.ParseMode.Html),
                                                              false);
        }
        private async Task ShowRanksDiamonds()
        {
            Log.Debug($"Callbacked ShowRanksDiamonds for {_userInfo}");
            string toSendText = GetRanksByDiamonds();
            if (toSendText == null)
                return;

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(InlineItems.InlineRanks(_userCulture), 3);

            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline, Telegram.Bot.Types.Enums.ParseMode.Html),
                                                              false);
        }
        private async Task ShowRanksApples()
        {
            Log.Debug($"Callbacked ShowRanksApples for {_userInfo}");
            string toSendText = GetRanksByApples();
            if (toSendText == null)
                return;

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(InlineItems.InlineRanks(_userCulture), 3);

            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline, Telegram.Bot.Types.Enums.ParseMode.Html),
                                                              false);
        }
        private async Task ShowRanksLevelAllGame()
        {
            Log.Debug($"Callbacked ShowRanksLevelAllGame for {_userInfo}");
            string toSendText = GetRanksByLevelAllGame();
            if (toSendText == null)
                return;

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(InlineItems.InlineRanks(_userCulture), 3);
            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline, Telegram.Bot.Types.Enums.ParseMode.Html),
                                                              false);
        }

        private string GetRanksByLevelAllGame()
        {
            try
            {
                var topPets = _appServices.PetService.GetAll()
                .OrderByDescending(p => p.LevelAllGame)
                .ThenByDescending(p => p.LastUpdateTime)
                .Take(10); //First 10 top-level pets

                string anwserRating = "";
                var currentUser = _appServices.UserService.Get(_userId);
                var currentPet = _appServices.PetService.Get(_userId);

                int counter = 1;
                foreach (var petDB in topPets)
                {
                    var userDB = _appServices.UserService.Get(petDB.UserId);
                    string name = petDB.Name ?? userDB.Username ?? userDB.FirstName + userDB.LastName;

                    if (counter == 1)
                    {
                        anwserRating += nameof(ranksCommandLevelAll).UseCulture(_userCulture) + "\n\n";
                        if (currentUser.UserId == userDB.UserId)
                            anwserRating += "<b>" + "🌟 " + (petDB.LevelAllGame + petDB.Level) + $" {Extensions.GetTypeEmoji(petDB.Type)} " + HttpUtility.HtmlEncode(name) + "</b>";
                        else
                            anwserRating += "🌟 " + (petDB.LevelAllGame + petDB.Level) + $" {Extensions.GetTypeEmoji(petDB.Type)} " + HttpUtility.HtmlEncode(name);
                        anwserRating += "\n⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯";
                        counter++;
                    }
                    else
                    {
                        anwserRating += "\n";

                        if (currentUser.UserId == userDB.UserId)
                            anwserRating += "<b>" + counter + ". " + (petDB.LevelAllGame + petDB.Level) + $" {Extensions.GetTypeEmoji(petDB.Type)} " + HttpUtility.HtmlEncode(name) + "</b>";
                        else
                            anwserRating += counter + ". " + (petDB.LevelAllGame + petDB.Level) + $" {Extensions.GetTypeEmoji(petDB.Type)} " + HttpUtility.HtmlEncode(name);
                        counter++;
                    }
                }

                if (!topPets.Any(a => a.UserId == currentUser.UserId))
                {
                    string name = currentPet.Name ?? currentUser.Username ?? currentUser.FirstName + currentUser.LastName;

                    anwserRating += "\n______________________________";
                    anwserRating += "\n <b>" + (_appServices.PetService.GetAll()
                    .OrderByDescending(p => p.LevelAllGame)
                    .ThenByDescending(p => p.LastUpdateTime)
                    .ToList()
                    .FindIndex(a => a.UserId == currentUser.UserId) + 1) + ". " + (currentPet.LevelAllGame + currentPet.Level) + $" {Extensions.GetTypeEmoji(currentPet.Type)} " + HttpUtility.HtmlEncode(name) + "</b>";
                }

                return anwserRating;
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return null;
            }
        }

        private async Task ShowRanksLevel()
        {
            Log.Debug($"Callbacked ShowRanksLevel for {_userInfo}");
            string toSendText = GetRanksByLevel();
            if (toSendText == null)
                return;

            InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(InlineItems.InlineRanks(_userCulture), 3);
            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline, Telegram.Bot.Types.Enums.ParseMode.Html),
                                                              false);
        }

        #endregion

        private string GetRanksByLevel()
        {
            try
            {
                var topPets = _appServices.PetService.GetAll()
                .OrderByDescending(p => p.Level)
                .ThenByDescending(p => p.LastUpdateTime)
                .Take(10); //First 10 top-level pets

                string anwserRating = "";
                var currentUser = _appServices.UserService.Get(_userId);
                var currentPet = _appServices.PetService.Get(_userId);

                int counter = 1;
                foreach (var petDB in topPets)
                {
                    var userDB = _appServices.UserService.Get(petDB.UserId);
                    string name = petDB.Name ?? userDB.Username ?? userDB.FirstName + userDB.LastName;

                    if (counter == 1)
                    {
                        anwserRating += nameof(ranksCommand).UseCulture(_userCulture) + "\n\n";
                        if (currentUser.UserId == userDB.UserId)
                            anwserRating += "<b>" + "🌟 " + petDB.Level + $" {Extensions.GetTypeEmoji(petDB.Type)} " + HttpUtility.HtmlEncode(name) + "</b>";
                        else
                            anwserRating += "🌟 " + petDB.Level + $" {Extensions.GetTypeEmoji(petDB.Type)} " + HttpUtility.HtmlEncode(name);
                        anwserRating += "\n⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯";
                        counter++;
                    }
                    else
                    {
                        anwserRating += "\n";

                        if (currentUser.UserId == userDB.UserId)
                            anwserRating += "<b>" + counter + ". " + petDB.Level + $" {Extensions.GetTypeEmoji(petDB.Type)} " + HttpUtility.HtmlEncode(name) + "</b>";
                        else
                            anwserRating += counter + ". " + petDB.Level + $" {Extensions.GetTypeEmoji(petDB.Type)} " + HttpUtility.HtmlEncode(name);
                        counter++;
                    }
                }

                if (!topPets.Any(a => a.UserId == currentUser.UserId))
                {
                    string name = currentPet.Name ?? currentUser.Username ?? currentUser.FirstName + currentUser.LastName;

                    anwserRating += "\n______________________________";
                    anwserRating += "\n <b>" + _appServices.PetService.GetAll()
                    .OrderByDescending(p => p.Level)
                    .ThenByDescending(p => p.LastUpdateTime)
                    .ToList()
                    .FindIndex(a => a.UserId == currentUser.UserId) + ". " + currentPet.Level + $" {Extensions.GetTypeEmoji(currentPet.Type)} " + HttpUtility.HtmlEncode(name) + "</b>";
                }

                return anwserRating;
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return null;
            }
        }
        private string GetRanksByApples()
        {
            try
            {
                var topApples = _appServices.AppleGameDataService.GetAll()
                .OrderByDescending(a => a.TotalWins)
                .Take(10); //First 10 top-apples users

                string anwserRating = "";
                var currentUser = _appServices.UserService.Get(_userId);
                var currentPet = _appServices.PetService.Get(_userId);

                int counter = 1;
                foreach (var appleUser in topApples)
                {
                    var userDB = _appServices.UserService.Get(appleUser.UserId);
                    var petDB = _appServices.PetService.Get(appleUser.UserId);
                    string name = $" {Extensions.GetTypeEmoji(petDB?.Type ?? -1)} " + _appServices.PetService.Get(appleUser.UserId)?.Name ?? userDB?.Username ?? userDB?.FirstName + userDB?.LastName ?? "";
                    if (counter == 1)
                    {
                        if (currentUser == null)
                            continue;

                        if (appleUser?.TotalWins == null)
                            continue;

                        anwserRating += nameof(ranksCommandApples).UseCulture(_userCulture) + "\n\n";
                        if (currentUser.UserId == userDB?.UserId)
                            anwserRating += "<b>" + "🍏 " + appleUser.TotalWins + HttpUtility.HtmlEncode(name) + "</b>";
                        else
                            anwserRating += "🍏 " + appleUser.TotalWins + HttpUtility.HtmlEncode(name);
                        anwserRating += "\n⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯";
                        counter++;
                    }
                    else
                    {
                        if (userDB == null)
                            continue;

                        if (appleUser?.TotalWins == null)
                            continue;

                        anwserRating += "\n";
                        if (currentUser.UserId == userDB.UserId)
                            anwserRating += "<b>" + counter + ". " + appleUser.TotalWins + HttpUtility.HtmlEncode(name) + "</b>";
                        else
                            anwserRating += counter + ". " + appleUser.TotalWins + HttpUtility.HtmlEncode(name);
                        counter++;
                    }
                }

                if (!topApples.Any(a => a.UserId == _userId))
                {
                    var currentUserApple = _appServices.AppleGameDataService.Get(_userId);

                    anwserRating += "\n______________________________";
                    var counterN = _appServices.AppleGameDataService.GetAll()
                .OrderByDescending(a => a.TotalWins)
                .ToList()
                .FindIndex(a => a.UserId == _userId);
                    anwserRating += "\n <b> " + (counterN == -1 ? _appServices.AppleGameDataService.GetAll()?.Count : counterN) + ". " + (currentUserApple?.TotalWins ?? 0) + HttpUtility.HtmlEncode($" {Extensions.GetTypeEmoji(currentPet.Type)} " + currentPet?.Name ?? currentUser.Username ?? currentUser.FirstName + currentUser.LastName) + "</b>";
                }

                return anwserRating;
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return null;
            }
        }
        private string GetRanksByGold()
        {
            try
            {
                var topPets = _appServices.PetService.GetAll().Join(_appServices.UserService.GetAll(),
                p => p.UserId,
                u => u.UserId,
                (pet, user) => new { user.UserId, user.Gold, pet.Name, pet.LastUpdateTime, pet.Type })
                .OrderByDescending(p => p.Gold)
                .ThenByDescending(p => p.LastUpdateTime)
                .Take(10); //First 10 top-gold pets

                string anwserRating = "";
                var currentUser = _appServices.UserService.Get(_userId);
                var currentPet = _appServices.PetService.Get(_userId);

                int counter = 1;
                foreach (var pet in topPets)
                {
                    var userDB = _appServices.UserService.Get(pet.UserId);
                    string name = pet.Name ?? userDB.Username ?? userDB.FirstName + userDB.LastName;
                    if (counter == 1)
                    {
                        anwserRating += nameof(ranksCommandGold).UseCulture(_userCulture) + "\n\n";
                        if (currentUser.UserId == userDB.UserId)
                            anwserRating += "<b>" + "💎 " + pet.Gold + $" {Extensions.GetTypeEmoji(pet.Type)} " + HttpUtility.HtmlEncode(name) + "</b>";
                        else
                            anwserRating += "💎 " + pet.Gold + $" {Extensions.GetTypeEmoji(pet.Type)} " + HttpUtility.HtmlEncode(name);
                        anwserRating += "\n⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯";
                        counter++;
                    }
                    else
                    {
                        anwserRating += "\n";
                        if (currentUser.UserId == userDB.UserId)
                            anwserRating += "<b>" + counter + ". " + pet.Gold + $" {Extensions.GetTypeEmoji(pet.Type)} " + HttpUtility.HtmlEncode(name) + "</b>";
                        else
                            anwserRating += counter + ". " + pet.Gold + $" {Extensions.GetTypeEmoji(pet.Type)} " + HttpUtility.HtmlEncode(name);
                        counter++;
                    }
                }

                if (!topPets.Any(a => a.UserId == currentUser.UserId))
                {
                    string name = currentPet.Name ?? currentUser.Username ?? currentUser.FirstName + currentUser.LastName;

                    anwserRating += "\n______________________________";
                    anwserRating += "\n <b>" +
                        _appServices.PetService.GetAll().Join(_appServices.UserService.GetAll(),
                        p => p.UserId,
                        u => u.UserId,
                        (pet, user) => new { user.UserId, user.Gold, pet.Name, pet.LastUpdateTime })
                        .OrderByDescending(p => p.Gold)
                        .ThenByDescending(p => p.LastUpdateTime)
                    .ToList()
                        .FindIndex(a => a.UserId == currentUser.UserId) + ". " + currentUser.Gold + $" {Extensions.GetTypeEmoji(currentPet.Type)} " + HttpUtility.HtmlEncode(name) + "</b>";
                }

                return anwserRating;

            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return null;
            }
        }

        private string GetRanksByDiamonds()
        {
            try
            {
                var topPets = _appServices.PetService.GetAll().Join(_appServices.UserService.GetAll(),
                p => p.UserId,
                u => u.UserId,
                (pet, user) => new { user.UserId, user.Diamonds, pet.Name, pet.LastUpdateTime, pet.Type })
                .OrderByDescending(p => p.Diamonds)
                .ThenByDescending(p => p.LastUpdateTime)
                .Take(10); //First 10 top-diamonds pets

                string anwserRating = "";
                var currentUser = _appServices.UserService.Get(_userId);
                var currentPet = _appServices.PetService.Get(_userId);

                int counter = 1;
                foreach (var pet in topPets)
                {
                    var userDB = _appServices.UserService.Get(pet.UserId);
                    string name = pet.Name ?? userDB.Username ?? userDB.FirstName + userDB.LastName;
                    if (counter == 1)
                    {
                        anwserRating += nameof(Resources.Resources.ranksCommandDiamonds).UseCulture(_userCulture) + "\n\n";
                        if (currentUser.UserId == userDB.UserId)
                            anwserRating += "<b>" + "💎 " + pet.Diamonds + $" {Extensions.GetTypeEmoji(pet.Type)} " + HttpUtility.HtmlEncode(name) + "</b>";
                        else
                            anwserRating += "💎 " + pet.Diamonds + $" {Extensions.GetTypeEmoji(pet.Type)} " + HttpUtility.HtmlEncode(name);
                        anwserRating += "\n⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯⋯";
                        counter++;
                    }
                    else
                    {
                        anwserRating += "\n";
                        if (currentUser.UserId == userDB.UserId)
                            anwserRating += "<b>" + counter + ". " + pet.Diamonds + $" {Extensions.GetTypeEmoji(pet.Type)} " + HttpUtility.HtmlEncode(name) + "</b>";
                        else
                            anwserRating += counter + ". " + pet.Diamonds + $" {Extensions.GetTypeEmoji(pet.Type)} " + HttpUtility.HtmlEncode(name);
                        counter++;
                    }
                }

                if (!topPets.Any(a => a.UserId == currentUser.UserId))
                {
                    string name = currentPet.Name ?? currentUser.Username ?? currentUser.FirstName + currentUser.LastName;

                    anwserRating += "\n______________________________";
                    anwserRating += "\n <b>" +
                        _appServices.PetService.GetAll().Join(_appServices.UserService.GetAll(),
                        p => p.UserId,
                        u => u.UserId,
                        (pet, user) => new { user.UserId, user.Diamonds, pet.Name, pet.LastUpdateTime })
                        .OrderByDescending(p => p.Diamonds)
                        .ThenByDescending(p => p.LastUpdateTime)
                    .ToList()
                        .FindIndex(a => a.UserId == currentUser.UserId) + ". " + currentUser.Diamonds + $" {Extensions.GetTypeEmoji(currentPet.Type)} " + HttpUtility.HtmlEncode(name) + "</b>";
                }

                return anwserRating;

            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return null;
            }
        }

        private async Task UpdateWorkOnPCButtonToDefault(Pet petDB)
        {
            string toSendTextIfTimeOver = string.Format(nameof(workCommand).UseCulture(_userCulture),
                                                        new DateTime(TimesToWait.WorkOnPCToWait.Ticks).ToString("HH:mm:ss"),
                                                        Rewards.WorkOnPCGoldReward,
                                                        petDB.Fatigue,
                                                        new DateTime(TimesToWait.FlyersDistToWait.Ticks).ToString("HH:mm:ss"),
                                                        Rewards.FlyersDistributingGoldReward);

            List<CallbackModel> inlineParts = InlineItems.InlineWork(_userCulture);
            InlineKeyboardMarkup toSendInlineIfTimeOver = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            Log.Debug($"Callbacked UpdateWorkOnPCButtonToDefault for {_userInfo}");
            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendTextIfTimeOver, toSendInlineIfTimeOver),
                                                              false);
        }
        private async Task ServeWorkCommandPetStillWorking(Pet petDB, JobType job)
        {
            TimeSpan timeToWait = job switch
            {
                JobType.WorkingOnPC => TimesToWait.WorkOnPCToWait,
                JobType.FlyersDistributing => TimesToWait.FlyersDistToWait,
                _ => new TimeSpan(0)
            };
            TimeSpan remainsTime = timeToWait - (DateTime.UtcNow - petDB.StartWorkingTime);

            //if _callback handled when pet is working
            if (remainsTime > TimeSpan.Zero)
            {
                Log.Debug($"Pet is working for {_userInfo}");
                await _appServices.BotControlService.SendAnswerMessageAsync(GetRemainedTimeWork(remainsTime, job), _userId, false);
            }
        }

        private async Task<bool> CheckStatusIsInactive(Pet petDB, bool IsGoToSleepCommand = false)
        {
            if (petDB.CurrentStatus == (int)CurrentStatus.Sleeping && !IsGoToSleepCommand)
            {
                string denyText = string.Format(nameof(denyAccessSleeping).UseCulture(_userCulture));
                await _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, denyText, true);

                return true;
            }

            if (petDB.CurrentStatus == (int)CurrentStatus.Working)
            {
                string denyText = string.Format(nameof(denyAccessWorking).UseCulture(_userCulture));
                await _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, denyText, true);

                return true;
            }

            return false;
        }
        private async Task SendAlertToUser(string textInAlert, bool isWarning = false)
        {
            Log.Debug($"Sent alert for {_userInfo}");
            await _appServices.BotControlService.AnswerCallbackQueryAsync(_callback?.Id, _userId, textInAlert, isWarning);
        }
        private async Task EditMessageToDefaultWorkCommand(Pet petDB)
        {
            petDB.CurrentStatus = (int)CurrentStatus.Active;
            _appServices.PetService.UpdateCurrentStatus(_userId, petDB.CurrentStatus);

            string toSendTextIfTimeOver = string.Format(nameof(workCommand).UseCulture(_userCulture),
                                                        new DateTime(TimesToWait.WorkOnPCToWait.Ticks).ToString("HH:mm:ss"),
                                                        Rewards.WorkOnPCGoldReward,
                                                        petDB.Fatigue,
                                                        new DateTime(TimesToWait.FlyersDistToWait.Ticks).ToString("HH:mm:ss"),
                                                        Rewards.FlyersDistributingGoldReward);
            List<CallbackModel> inlineParts = InlineItems.InlineWork(_userCulture);
            InlineKeyboardMarkup toSendInlineIfTimeOver = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

            Log.Debug($"Callbacked ShowDefaultWorkCommand (work is over) for {_userInfo}");
            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendTextIfTimeOver, toSendInlineIfTimeOver),
                                                              false);
        }
        private async Task StartWorkOnPC(Pet petDB)
        {
            InlineKeyboardMarkup toSendInline;
            string toSendText;

            if (_callback.Data == CallbackButtons.WorkCommand.WorkCommandInlineShowTime(default, JobType.WorkingOnPC, _userCulture).CallbackData)
            {
                await UpdateWorkOnPCButtonToDefault(petDB);
                return;
            }

            var newFatigue = petDB.Fatigue + Factors.WorkOnPCFatigueFactor;
            if (newFatigue > 100)
            {
                await PetIsTooTired(JobType.WorkingOnPC);
                return;
            }

            _appServices.PetService.UpdateFatigue(_userId, newFatigue);
            _appServices.UserService.UpdateGold(_userId, _appServices.UserService.Get(_userId).Gold + Rewards.WorkOnPCGoldReward);
            _appServices.PetService.UpdateCurrentStatus(_userId, (int)CurrentStatus.Working);
            _appServices.PetService.UpdateCurrentJob(_userId, (int)JobType.WorkingOnPC);
            _appServices.PetService.UpdateEXP(_userId, petDB.EXP + ExpForAction.WorkPC);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.GoldEarnedCounter += Rewards.WorkOnPCGoldReward;
            aud.WorkOnPCCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var startWorkingTime = DateTime.UtcNow;
            _appServices.PetService.UpdateStartWorkingTime(_userId, startWorkingTime);

            string anwser = string.Format(nameof(PetWorkingAnswerCallback).UseCulture(_userCulture), Factors.WorkOnPCFatigueFactor, Rewards.WorkOnPCGoldReward);
            await SendAlertToUser(anwser, true);

            toSendText = string.Format(nameof(workCommandPCWorking).UseCulture(_userCulture));

            TimeSpan remainsTime = TimesToWait.WorkOnPCToWait - (DateTime.UtcNow - startWorkingTime);

            toSendInline = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
                {
                    CallbackButtons.WorkCommand.WorkCommandInlineShowTime(remainsTime, JobType.WorkingOnPC, _userCulture)
                });

            Log.Debug($"Callbacked StartWorkOnPC for {_userInfo}");
            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }
        private async Task ServeWorkOnPC(Pet petDB)
        {
            TimeSpan remainsTime = TimesToWait.WorkOnPCToWait - (DateTime.UtcNow - petDB.StartWorkingTime);

            //if _callback handled when time of work is over
            if (remainsTime <= TimeSpan.Zero)
            {
                await EditMessageToDefaultWorkCommand(petDB);
                return;
            }

            Log.Debug($"Callbacked ServeWorkOnPC (still working) for {_userInfo}");

            //if _callback handled when pet is still working
            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              await ShowRemainedTimeWorkOnPCCallback(remainsTime),
                                                              false);
        }
        private async Task StartJobFlyers(Pet petDB)
        {
            InlineKeyboardMarkup toSendInline;
            string toSendText;

            if (_callback.Data == CallbackButtons.WorkCommand.WorkCommandInlineShowTime(default, JobType.FlyersDistributing, _userCulture).CallbackData)
            {
                await UpdateWorkOnPCButtonToDefault(petDB);
                return;
            }

            var newFatigue = petDB.Fatigue + Factors.FlyersDistributingFatigueFactor;
            if (newFatigue > 100)
            {
                await PetIsTooTired(JobType.FlyersDistributing);
                return;
            }

            _appServices.PetService.UpdateFatigue(_userId, newFatigue);
            _appServices.UserService.UpdateGold(_userId, _appServices.UserService.Get(_userId).Gold + Rewards.FlyersDistributingGoldReward);
            _appServices.PetService.UpdateCurrentStatus(_userId, (int)CurrentStatus.Working);
            _appServices.PetService.UpdateCurrentJob(_userId, (int)JobType.FlyersDistributing);
            _appServices.PetService.UpdateEXP(_userId, petDB.EXP + ExpForAction.WorkFlyers);

            var aud = _appServices.AllUsersDataService.Get(_userId);
            aud.GoldEarnedCounter += Rewards.FlyersDistributingGoldReward;
            aud.WorkFlyersCounter++;
            _appServices.AllUsersDataService.Update(aud);

            var startWorkingTime = DateTime.UtcNow;
            _appServices.PetService.UpdateStartWorkingTime(_userId, startWorkingTime);

            string anwser = string.Format(nameof(PetWorkingAnswerCallback).UseCulture(_userCulture), Factors.FlyersDistributingFatigueFactor, Rewards.FlyersDistributingGoldReward);
            await SendAlertToUser(anwser, true);

            toSendText = string.Format(nameof(workCommandFlyersWorking).UseCulture(_userCulture));

            TimeSpan remainsTime = TimesToWait.FlyersDistToWait - (DateTime.UtcNow - startWorkingTime);

            toSendInline = Extensions.InlineKeyboardOptimizer(new List<CallbackModel>()
                {
                    CallbackButtons.WorkCommand.WorkCommandInlineShowTime(remainsTime, JobType.FlyersDistributing, _userCulture)
                });

            Log.Debug($"Callbacked StartJobFlyers for {_userInfo}");
            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              new AnswerCallback(toSendText, toSendInline),
                                                              false);
        }
        private async Task ServeJobFlyers(Pet petDB)
        {
            TimeSpan remainsTime = TimesToWait.FlyersDistToWait - (DateTime.UtcNow - petDB.StartWorkingTime);

            //if _callback handled when time of work is over
            if (remainsTime <= TimeSpan.Zero)
            {
                await EditMessageToDefaultWorkCommand(petDB);
                return;
            }

            Log.Debug($"Callbacked ServeJobFlyers (still working) for {_userInfo}");

            //if _callback handled when pet is still working
            await _appServices.BotControlService.SendAnswerCallback(_userId,
                                                              _callback?.Message?.MessageId ?? 0,
                                                              await ShowRemainedTimeJobFlyersCallback(remainsTime),
                                                              false);
        }

        //private async Task PlayTicTacToeInline()
        //{
        //    Log.Debug($"Called PlayTicTacToeInline for {_userInfo}");
        //    await new TicTacToeGameController(_appServices, null, _callback).PreStart();
        //}

        private bool IsGeminiTimeout(List<(string userQ, string geminiA, DateTime revision)> previousQA)
        {
            if (previousQA == null || previousQA.Count < Constants.QA_MAX_COUNTER)
                return false;

            return (DateTime.UtcNow - previousQA[0].revision) < Constants.TimesToWait.GeminiTimeout;
        }
    }
}
