using System.Globalization;
using TamagotchiBot.Services.Interfaces;
using Telegram.Bot.Types;
using Serilog;
using System;
using TamagotchiBot.Models.Mongo;
using static TamagotchiBot.Resources.Resources;
using static TamagotchiBot.UserExtensions.Constants;
using TamagotchiBot.Models.Answers;
using Extensions = TamagotchiBot.UserExtensions.Extensions;
using TamagotchiBot.UserExtensions;
using System.Linq;
using Telegram.Bot.Types.ReplyMarkups;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Web;

namespace TamagotchiBot.Controllers
{
    public class CreatorController : ControllerBase
    {
        private readonly IApplicationServices _appServices;
        private readonly Message _message = null;
        private readonly CallbackQuery _callback = null;
        private readonly long _userId;
        private CultureInfo _userCulture;

        private string _userInfo;

        public CreatorController(IApplicationServices services, Message message = null, CallbackQuery callback = null)
        {
            _callback = callback;
            _message = message;
            _userId = callback?.From.Id ?? message.From.Id;
            _appServices = services;

            _userInfo = Extensions.GetLogUser(_appServices.UserService.Get(_userId));

            _userCulture = new CultureInfo(_appServices.UserService.Get(_userId)?.Culture ?? "ru");
            UpdateOrCreateAUD(message?.From ?? callback.From, message, callback);
        }
        public void CreateUser()
        {
            if (_message != null)
                CreateUserFromMessage(_message);
        }
        public async Task AskALanguage()
        {
            var toSend = new AnswerMessage(nameof(ChangeLanguage).UseCulture(_userCulture),
                                    StickersId.ChangeLanguageSticker,
                                    Constants.ReplyKeyboardItems.LanguagesMarkup,
                                    null);
            Log.Debug($"Asked for language after register {_userInfo}");

            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
            _appServices.UserService.UpdateIsLanguageAskedOnCreate(_userId, true);
        }

        internal async Task<bool> CreatePet(Models.Mongo.User userDB)
        {
            var msgText = _message?.Text;
            if (string.IsNullOrEmpty(msgText))
                return false;

            _appServices.PetService.Create(new Pet()
            {
                UserId = _userId,
                Name = msgText,
                Level = 1,
                BirthDateTime = DateTime.UtcNow,
                LastUpdateTime = DateTime.UtcNow,
                EducationLevel = 1,
                EducationStage = 0,
                NextRandomEventNotificationTime = DateTime.UtcNow.AddMinutes(25),
                EXP = 0,
                HP = 100,
                Joy = 70,
                Fatigue = 0,
                Satiety = 80,
                Type = (int)PetType.Cat
            });
            Log.Information($"Pet of UserID: {_userId} has been added to Db");

            _appServices.UserService.UpdateIsPetNameAskedOnCreate(_userId, false);
            var resultRef = _appServices.ReferalInfoService.UpdateTaskDone(_userId, true);

            if (userDB.ReferaledBy != 0 && resultRef)
            {
                var producerCulture = _appServices.UserService.Get(userDB.ReferaledBy)?.Culture ?? "ru";
                await _appServices.BotControlService.SendAnswerMessageAsync(
                    new AnswerMessage()
                    {
                        Text = string.Format(
                            nameof(ReferalAddedMessageText).UseCulture(producerCulture),
                            Rewards.ReferalAdded,
                            userDB.Username is null ? $"{userDB.FirstName} {userDB.LastName}" : $"@{userDB.Username}"),
                        ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
                    }, userDB.ReferaledBy);
            }

            var toSend = new AnswerMessage()
            {
                Text = string.Format(
                    nameof(ConfirmedName).UseCulture(_userCulture),
                    HttpUtility.HtmlEncode(msgText)),
                StickerId = StickersId.GetStickerByType(nameof(StickersId.PetConfirmedNameSticker_Cat), PetType.Cat),
                ReplyMarkup = ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
            };

            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);

            await Task.Delay(2500);
            await new MenuController(_appServices, null, _message).ProcessMessage("/pet");
            return true;
        }
        internal async Task<bool> AskToConfirmNewName()
        {
            var msgText = _message?.Text;

            if (string.IsNullOrEmpty(msgText))
            {
                var toSendRenameInfoAgain = new AnswerMessage()
                {
                    Text = nameof(renameCommand).UseCulture(_userCulture),
                    StickerId = StickersId.RenamePetSticker,
                    ReplyMarkup = new ReplyKeyboardRemove()
                };

                Log.Debug($"Resent rename info {_userInfo}");

                await _appServices.BotControlService.SendAnswerMessageAsync(toSendRenameInfoAgain, _userId, false);
                return false;
            }

            if (!string.IsNullOrEmpty(_message.Text)
                && _message.Text.FirstOrDefault() == '/')
            {
                _appServices.MetaUserService.UpdateIsPetNameAskedOnRename(_userId, false);
                _appServices.MetaUserService.UpdateIsAskedToConfirmRenaming(_userId, false);

                await new MenuController(_appServices, null, _message).ProcessMessage("/pet");
                return true;
            }

            if (!await IsNicknameAcceptable())
                return false;

            _appServices.MetaUserService.UpdateIsAskedToConfirmRenaming(_userId, true);
            _appServices.MetaUserService.UpdateTmpPetName(_userId, msgText);

            var petDB = _appServices.PetService.Get(_userId);
            var toSend = new AnswerMessage()
            {
                Text = string.Format(
                    nameof(AskToConfirmRenamingPet).UseCulture(_userCulture),
                    petDB.Name,
                    msgText,
                    Constants.Costs.RenamePet),
                StickerId = StickersId.GetStickerByType(nameof(StickersId.PetAskForConfirmNameSticker_Cat), Extensions.GetEnumPetType(petDB.Type)),
                ReplyMarkup = new ReplyKeyboardMarkup(new List<KeyboardButton>()
                {
                    new KeyboardButton(nameof(YesTextEmoji).UseCulture(_userCulture)),
                    new KeyboardButton(nameof(NoTextEmoji).UseCulture(_userCulture))
                })
                {
                    OneTimeKeyboard = true
                }
            };

            Log.Debug($"Asked to confirm new name for pet {_userInfo}");
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);

            return true;
        }
        internal async Task<bool> ApplyNewLanguage(bool isLanguageChanged = false)
        {
            var msgText = _message?.Text;
            if (string.IsNullOrEmpty(msgText))
                return false;

            var flagFromMsg = msgText.Split(' ').LastOrDefault();

            var newLanguage = flagFromMsg?.GetCulture();
            if (string.IsNullOrEmpty(newLanguage))
            {
                var toSendLanguagesAgain = new AnswerMessage()
                {
                    Text = nameof(ChangeLanguage).UseCulture(_userCulture),
                    StickerId = StickersId.ChangeLanguageSticker,
                    ReplyMarkup = Constants.ReplyKeyboardItems.LanguagesMarkup,
                    InlineKeyboardMarkup = null
                };

                Log.Debug($"Asked for language again {_userInfo}");

                await _appServices.BotControlService.SendAnswerMessageAsync(toSendLanguagesAgain, _userId, false);
                return false;
            }

            _appServices.UserService.UpdateLanguage(_userId, newLanguage);
            _userCulture = new CultureInfo(newLanguage);

            if (!isLanguageChanged)
            {
                _appServices.UserService.UpdateIsLanguageAskedOnCreate(_userId, false);
                return true;
            }

            string stickerToSend = newLanguage.Language() switch
            {
                Languages.Polish => StickersId.PolishLanguageSetSticker,
                Languages.English => StickersId.EnglishLanguageSetSticker,
                Languages.Belarusian => StickersId.BelarussianLanguageSetSticker,
                Languages.Russian => StickersId.RussianLanguageSetSticker,
                Languages.Ukrainian => StickersId.UkrainianLanguageSetSticker,
                _ => null,
            };
            var toSend = new AnswerMessage()
            {
                Text = nameof(ConfirmedLanguage).UseCulture(_userCulture),
                StickerId = stickerToSend,
                ReplyMarkup = ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture)
            };

            Log.Debug($"Confirmed language for {_userInfo}");
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);

            return true;
        }
        internal async Task SendWelcomeText()
        {
            var toSend = new AnswerMessage()
            {
                Text = nameof(Welcome).UseCulture(_userCulture),
                StickerId = StickersId.WelcomeSticker,
                ReplyMarkup = new ReplyKeyboardRemove()
            };

            Log.Debug($"Sent welcome text for {_userInfo}");
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        internal async Task AskForAPetName()
        {
            var petDB = _appServices.PetService.Get(_userId);
            var toSend = new AnswerMessage()
            {
                Text = nameof(ChooseName).UseCulture(_userCulture),
                StickerId = StickersId.GetStickerByType(nameof(StickersId.PetChooseNameSticker_Cat), Extensions.GetEnumPetType(petDB?.Type ?? new Random().Next(5))),
            };

            Log.Debug($"Asked for a pet name for {_userInfo}");
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);

            _appServices.UserService.UpdateIsPetNameAskedOnCreate(_userId, true);
        }

        internal async Task UpdateIndicators()
        {
            Telegram.Bot.Types.User userFromMsg = _message?.From ?? _callback?.From;
            var petDB = _appServices.PetService.Get(_userId);

            if (userFromMsg == null || petDB == null || petDB.IsGone || petDB.HP <= 0)
                return;

            var petResult = Pet.Clone(petDB);

            int minuteCounter = (int)(DateTime.UtcNow - petDB.LastUpdateTime).TotalMinutes;

            //EXP & Level
            var expLevel = UpdateIndicatorEXP(minuteCounter, petDB);
            petResult.EXP = expLevel.Item1;
            petResult.Level = expLevel.Item2;

            //Satiety & HP
            var satietyHp = UpdateIndicatorSatietyAndHP(minuteCounter, petDB);
            petResult.Satiety = satietyHp.Item1;
            petResult.HP = satietyHp.Item2;
            petResult.MPSatiety = 0;

            //Joy
            petResult.Joy = UpdateIndicatorJoy(minuteCounter, petDB);

            //Fatigue
            petResult.Fatigue = UpdateIndicatorFatigue(minuteCounter, petDB);

            //Hygiene
            petResult.Hygiene = UpdateIndicatorHygiene(minuteCounter, petDB);

            //Sleeping
            if (petDB.CurrentStatus == (int)CurrentStatus.Sleeping)
            {
                var sleepResult = UpdateIndicatorSleeping(petDB);
                var currentStatus = sleepResult.Item1;
                var currentFatigue = sleepResult.Item2;

                petResult.CurrentStatus = (int)currentStatus;
                petResult.Fatigue = currentStatus != CurrentStatus.Active ? petResult.Fatigue : currentFatigue;
            }

            //Work
            if (petDB.CurrentStatus == (int)CurrentStatus.Working)
            {
                var (currentStatus, currentJob) = UpdateIndicatorWork(petDB);
                petResult.CurrentStatus = currentStatus;
                petResult.CurrentJob = currentJob;
            }

            //Education
            if (petDB.CurrentStatus == (int)CurrentStatus.Studying)
            {
                var aud = _appServices.AllUsersDataService.Get(_userId);
                await UpdateIndicatorStudying(petResult, aud);
                _appServices.AllUsersDataService.Update(aud);
            }

            petResult.LastUpdateTime = DateTime.UtcNow;
            _appServices.PetService.Update(userFromMsg.Id, petResult);
        }

        private async Task UpdateIndicatorStudying(Pet petResult, AllUsersData aud)
        {
            var currentLevel = petResult.EducationLevel.GetActualEducationLevel();
            bool isLevelCompleted = false;

            var timeLeft = petResult.StartStudyingTime + Extensions.GetEducationTime(currentLevel) - DateTime.UtcNow;

            if (timeLeft > TimeSpan.Zero)
                return;

            petResult.CurrentStatus = (int)CurrentStatus.Active;
            petResult.EducationStage++;

            // Leveling logic
            var stagesNeeded = currentLevel.GetStagesNeeded();

            if (petResult.EducationStage >= stagesNeeded)
            {
                petResult.EducationStage = 0;
                petResult.EducationLevel = (int)(currentLevel + 1);

                isLevelCompleted = true;
            }

            int expReward = currentLevel.GetEducationExpReward();
            petResult.EXP += expReward;
            aud.EducationStagesPassedCounter++;

            string text = string.Format(nameof(Resources.Resources.educationCommand_NotifyFinished).UseCulture(_userCulture), 1, expReward);

            if (isLevelCompleted)
            {
                text += string.Format(nameof(Resources.Resources.educationCommand_newLevelCompleted).UseCulture(_userCulture), petResult.EducationLevel.GetActualEducationLevelTranslatedString(_userCulture));
            }

            await _appServices.BotControlService.SendAnswerMessageAsync(new AnswerMessage()
            {
                Text = text,
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
                //StickerId = StickersId.GetStickerByType(nameof(StickersId.PetInfoSticker_Cat), petResult.Type),
                //ReplyMarkup = ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture)
            }, _userId, false);
        }

        internal bool CheckIsPetZeroHP() => _appServices.PetService.Get(_userId)?.HP <= 0;
        internal bool CheckIsPetGone() => _appServices.PetService.Get(_userId)?.IsGone ?? false;
        internal async Task AfterDeath()
        {
            var petDB = _appServices.PetService.Get(_userId);
            var userDB = _appServices.UserService.Get(_userId);
            if (petDB == null)
                return;

            var petResult = Pet.Clone(petDB);
            petResult.IsGone = true;

            string textToSend = string.Format(
                nameof(FarewellText).UseCulture(_userCulture),
                petResult.Name,
                userDB.Username,
                userDB.Gold,
                userDB.Diamonds);

            var toSend = new AnswerMessage()
            {
                Text = textToSend,
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                StickerId = StickersId.GetStickerByType(nameof(StickersId.PetGoneSticker_Cat), petResult.Type),
            };

            Log.Debug($"Sent farewell for {_userInfo}");

            _appServices.PetService.Update(_userId, petResult);
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
            await _appServices.BotControlService.SendChatActionAsync(_userId, Telegram.Bot.Types.Enums.ChatAction.Typing);

            await Task.Delay(2500);
            await AskForResurrect();
        }
        internal async Task ToRenamingAnswer()
        {
            bool? answerFromUser = null;
            string msgText = _message.Text;
            if (msgText == null)
                return;
            msgText = msgText.ToLower();

            if (GetAllTranslatedAndLowered(nameof(YesTextEmoji)).Contains(msgText))
                answerFromUser = true;
            else if (GetAllTranslatedAndLowered(nameof(NoTextEmoji)).Contains(msgText))
                answerFromUser = false;

            if (answerFromUser == null)
            {
                await AskToConfirmNewName();
                return;
            }

            if (answerFromUser == true)
            {
                if (_appServices.UserService.Get(_userId).Gold >= Costs.RenamePet)
                {
                    _appServices.MetaUserService.UpdateIsPetNameAskedOnRename(_userId, false);
                    _appServices.MetaUserService.UpdateIsAskedToConfirmRenaming(_userId, false);
                    await RenamePet();
                    return;
                }
                else //not enough gold to rename
                {
                    var toSend = new AnswerMessage()
                    {
                        Text = nameof(NotEnoughGold).UseCulture(_userCulture),
                        ReplyMarkup = new ReplyKeyboardRemove()
                    };

                    Log.Debug($"Not enough gold for rename for {_userInfo}");
                    await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
                    await Task.Delay(1000);
                    answerFromUser = false;
                }
            }

            if (answerFromUser == false)
            {
                _appServices.MetaUserService.UpdateIsPetNameAskedOnRename(_userId, false);
                _appServices.MetaUserService.UpdateIsAskedToConfirmRenaming(_userId, false);
                await new MenuController(_appServices, null, _message).ProcessMessage("/pet");
            }
        }
        internal async Task ToResurrectAnswer()
        {
            bool? answerFromUser = null;
            string msgText = _message.Text;
            if (msgText == null)
                return;
            msgText = msgText.ToLower();

            if (GetAllTranslatedAndLowered(nameof(ResurrectYesText)).Contains(msgText))
                answerFromUser = true;
            else if (GetAllTranslatedAndLowered(nameof(ResurrectNoText)).Contains(msgText))
                answerFromUser = false;

            if (answerFromUser == null)
            {
                await AskForResurrect();
                return;
            }

            if (answerFromUser == true)
            {
                if (_appServices.UserService.Get(_userId).Gold >= Costs.ResurrectPet)
                {
                    await ResurrectPet();
                    return;
                }
                else //not enough gold to buy back
                {
                    var toSend = new AnswerMessage()
                    {
                        Text = nameof(NotEnoughGold).UseCulture(_userCulture),
                        ReplyMarkup = new ReplyKeyboardRemove()
                    };
                    Log.Debug($"Sent NotEnoughGoldToResurrect for {_userInfo}");
                    await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
                    await Task.Delay(1000);
                    answerFromUser = false;
                }
            }

            if (answerFromUser == false)
            {
                var toSend = new AnswerMessage()
                {
                    Text = nameof(EpilogueText).UseCulture(_userCulture),
                    StickerId = StickersId.GetStickerByType(nameof(StickersId.PetEpilogueSticker_Cat), _appServices.PetService.Get(_userId)?.Type)
                };

                Log.Debug($"Sent epilogue for {_userInfo}");
                await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
                await Task.Delay(2500);
                await KillThePet();
            }
        }
        internal async Task<bool> IsNicknameAcceptable()
        {
            var badWordsDB = _appServices.SInfoService.GetBadWords().ConvertAll(w => w.ToUpper());
            if (string.IsNullOrEmpty(_message.Text)
                || _message.Text.Count() > 25
                || _message.Text.FirstOrDefault() == '/'
                || badWordsDB.Contains(_message.Text.ToUpper()))
            {
                Log.Debug($"Bad word detected: {_message.Text}");
                var emoji = Extensions.GetTypeEmoji(_appServices.PetService.Get(_userId)?.Type ?? -1);
                var toSend = new AnswerMessage()
                {
                    Text = string.Format(
                        nameof(BadWordDetected).UseCulture(_userCulture),
                        emoji),
                    StickerId = StickersId.PetDoesntLikeNameSticker
                };
                await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
                return false;
            }
            return true;
        }

        private void CreateUserFromMessage(Message msg)
        {
            var userTMP = _appServices.UserService.Create(msg.From);
            _userInfo = Extensions.GetLogUser(userTMP);
            Log.Information($"{_userInfo} has been added to Db");

            AdsAndRefChecks(msg);
            _userCulture = new CultureInfo(msg.From.LanguageCode ?? "ru");

            void AdsAndRefChecks(Message msg)
            {
                var ads = Extensions.GetAdsProducerFromStart(msg.Text);
                if (ads != null)
                    _appServices.AdsProducersService.AddOrInsert(ads);
                else
                {
                    var refProducer = Extensions.GetReferalProducerFromStart(msg.Text);
                    if (refProducer != -1)
                    {
                        var (_, isSuccessRefAdded) = _appServices.ReferalInfoService.AddNewReferal(refProducer, _userId);

                        if (isSuccessRefAdded)
                            Log.Information($"Added new referal for {refProducer} with ID:{_userId}");
                    }
                }
            }
        }
        private async Task RenamePet()
        {
            var metaUser = _appServices.MetaUserService.Get(_userId);
            var userDB = _appServices.UserService.Get(_userId);
            var petDB = _appServices.PetService.Get(_userId);
            userDB.Gold -= Costs.RenamePet;

            _appServices.PetService.UpdateName(_userId, metaUser.TmpPetName);
            _appServices.UserService.Update(_userId, userDB);

            var toSend = new AnswerMessage()
            {
                Text = string.Format(
                    nameof(ConfirmedName).UseCulture(_userCulture),
                    metaUser.TmpPetName),
                StickerId = StickersId.GetStickerByType(nameof(StickersId.PetConfirmedNameSticker_Cat), Extensions.GetEnumPetType(petDB?.Type)),
                ReplyMarkup = ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
            };

            Log.Debug($"Confirmed name by {_userInfo}");
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private async Task ResurrectPet()
        {
            var petDB = _appServices.PetService.Get(_userId);
            var userDB = _appServices.UserService.Get(_userId);
            petDB.IsGone = false;
            petDB.HP = 100;
            petDB.Satiety = 100;
            petDB.Fatigue = 50;
            userDB.Gold -= Costs.ResurrectPet;
            petDB.LastUpdateTime = DateTime.UtcNow;
            _appServices.PetService.Update(_userId, petDB);
            _appServices.UserService.Update(_userId, userDB);

            var toSend = new AnswerMessage()
            {
                Text = string.Format(
                    nameof(PetCameBackText).UseCulture(_userCulture),
                    Extensions.GetTypeEmoji(petDB.Type)),
                StickerId = StickersId.GetStickerByType(nameof(StickersId.PetResurrectedSticker_Cat), petDB.Type),
                ReplyMarkup = ReplyKeyboardItems.MenuKeyboardMarkup(_userCulture)
            };

            Log.Debug($"Pet came back after resurrect {_userInfo}");
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
        }
        private void UpdateOrCreateAUD(Telegram.Bot.Types.User user, Message message = null, CallbackQuery callback = null)
        {
            if (user == null)
            {
                Log.Warning("User is null, can not add to AUD");
                return;
            }

            var aud = _appServices.AllUsersDataService.Get(user.Id);
            if (aud == null) //if new
            {
                _appServices.AllUsersDataService.Create(new AllUsersData()
                {
                    UserId = user.Id,
                    ChatId = user.Id,
                    Created = DateTime.UtcNow,
                    Culture = _userCulture?.ToString() ?? "ru",
                    Username = user.Username,
                    Updated = DateTime.UtcNow,
                    LastMessage = message?.Text ?? string.Empty,
                    MessageCounter = message == null ? 0 : 1,
                    CallbacksCounter = callback == null ? 0 : 1,

                });
                return;
            }

            aud.Updated = DateTime.UtcNow;
            aud.Username = user.Username;
            aud.Culture = _appServices.UserService.Get(user.Id)?.Culture ?? "ru";
            aud.LastMessage = message?.Text ?? string.Empty;
            aud.MessageCounter = message == null ? aud.MessageCounter : aud.MessageCounter + 1;
            aud.CallbacksCounter = callback == null ? aud.CallbacksCounter : aud.CallbacksCounter + 1;
            _appServices.AllUsersDataService.Update(aud);
        }
        private async Task KillThePet()
        {
            _appServices.PetService.Remove(_userId);
            _appServices.AppleGameDataService.Delete(_userId);
            await _appServices.UserService.UpdateAppleGameStatus(_userId, false);
            await AskALanguage();
        }
        private async Task AskForResurrect()
        {
            var toResurrect = new AnswerMessage()
            {
                Text = nameof(ToResurrectQuestion).UseCulture(_userCulture),
                ReplyMarkup = new ReplyKeyboardMarkup(new List<KeyboardButton>()
                {
                    new KeyboardButton(nameof(ResurrectYesText).UseCulture(_userCulture)),
                    new KeyboardButton(nameof(ResurrectNoText).UseCulture(_userCulture))
                })
                {
                    OneTimeKeyboard = true
                }
            };

            Log.Debug($"Asked to resurrect for {_userInfo}");
            await _appServices.BotControlService.SendAnswerMessageAsync(toResurrect, _userId, false);
        }

        #region Indicator Updaters

        private (int, int) UpdateIndicatorEXP(int minuteCounter, Pet pet)
        {
            var petResult = Pet.Clone(pet);

            decimal toAddExp = minuteCounter * Factors.ExpFactor;
            while (toAddExp > 0)
            {
                if (toAddExp < Factors.ExpToLvl * petResult.Level)
                {
                    petResult.EXP += (int)toAddExp;
                    break;
                }
                else
                {
                    toAddExp -= Factors.ExpToLvl * petResult.Level;
                    petResult.Level++;
                }
            }

            if (petResult.EXP > Factors.ExpToLvl * petResult.Level)
            {
                petResult.Level += petResult.EXP / (Factors.ExpToLvl * petResult.Level);
                petResult.EXP %= Factors.ExpToLvl;
            }

            return (petResult.EXP, petResult.Level);
        }
        private (double, int) UpdateIndicatorSatietyAndHP(int minuteCounter, Pet pet)
        {
            var petResult = Pet.Clone(pet);

            double decreaseSatiety = Math.Round(minuteCounter * Factors.StarvingFactor, 2);

            petResult.Satiety -= decreaseSatiety;
            petResult.Satiety = Math.Round(petResult.Satiety, 2);
            petResult.Satiety += petResult.MPSatiety;

            if (petResult.Satiety > 100)
                petResult.Satiety = 100;

            if (petResult.Satiety < 0)
            {
                petResult.Satiety = 0;
                int tmpSat = (int)decreaseSatiety > 100 ? ((int)decreaseSatiety) - 100 : ((int)decreaseSatiety);
                petResult.HP -= tmpSat;

                if (petResult.HP < 0)
                    petResult.HP = 0;
            }

            return (petResult.Satiety, petResult.HP);
        }
        private int UpdateIndicatorHygiene(int minuteCounter, Pet petDB)
        {
            var petResult = Pet.Clone(petDB);

            double toDecreaseHygiene = Math.Round(minuteCounter * Factors.HygieneFactor);

            petResult.Hygiene -= (int)toDecreaseHygiene;

            if (petResult.Hygiene < 0)
                petResult.Hygiene = 0;

            return petResult.Hygiene;
        }
        private int UpdateIndicatorJoy(int minuteCounter, Pet pet)
        {
            var petResult = Pet.Clone(pet);

            double toDecreaseJoy = Math.Round(minuteCounter * Factors.JoyFactor);

            petResult.Joy -= (int)toDecreaseJoy;

            if (petResult.Joy < 0)
                petResult.Joy = 0;

            return petResult.Joy;
        }
        private int UpdateIndicatorFatigue(int minuteCounter, Pet pet)
        {
            var petResult = Pet.Clone(pet);

            if (petResult.CurrentStatus == (int)CurrentStatus.Active)
            {
                double toAddFatigue = Math.Round(minuteCounter * Factors.FatigueFactor);
                petResult.Fatigue += (int)toAddFatigue;
            }

            if (petResult.Fatigue > 100)
                petResult.Fatigue = 100;

            return petResult.Fatigue;
        }
        private (CurrentStatus, int) UpdateIndicatorSleeping(Pet petDB)
        {
            var petResult = Pet.Clone(petDB);

            var remainsToSleepTime = petResult.ToWakeUpTime - DateTime.UtcNow;
            if (remainsToSleepTime <= TimeSpan.Zero)
            {
                petResult.CurrentStatus = (int)CurrentStatus.Active;
                petResult.Fatigue = 0;

                return ((CurrentStatus)petResult.CurrentStatus, petResult.Fatigue);
            }

            return ((CurrentStatus)petResult.CurrentStatus, petResult.Fatigue);
        }
        private (int currentStatus, int currentJob) UpdateIndicatorWork(Pet petDB)
        {
            TimeSpan workTime = ((JobType)petDB.CurrentJob).GetTimeToWait();
            TimeSpan remainsTime = workTime - (DateTime.UtcNow - petDB.StartWorkingTime);

            //if _callback handled when time of work is over
            if (remainsTime <= TimeSpan.Zero)
                return ((int)CurrentStatus.Active, (int)JobType.None);

            return ((int)CurrentStatus.Working, petDB.CurrentJob);
        }

        #endregion

    }
}
