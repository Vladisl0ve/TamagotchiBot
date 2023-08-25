﻿using System.Globalization;
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

namespace TamagotchiBot.Controllers
{
    public class CreatorController
    {
        private IApplicationServices _appServices;
        private readonly Message _message = null;
        private readonly CallbackQuery _callback = null;
        private readonly long _userId;
        private readonly CultureInfo _userCulture;

        public CreatorController(IApplicationServices services, Message message = null, CallbackQuery callback = null)
        {
            _callback = callback;
            _message = message;
            _userId = callback?.From.Id ?? message.From.Id;

            _appServices = services;

            Culture = _userCulture = new CultureInfo(_appServices.UserService.Get(_userId)?.Culture ?? "ru");
            UpdateOrCreateAUD(message?.From ?? callback.From, message, callback);
        }
        public void CreateUser()
        {
            if (_message != null)
                CreateUserFromMessage(_message);
            else if (_callback != null)
                CreateUserFromCallback(_callback);
        }
        public void AskALanguage()
        {
            var toSend = new AnswerMessage(ChangeLanguage,
                                    StickersId.ChangeLanguageSticker,
                                    LanguagesMarkup,
                                    null);

            _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId);
            _appServices.UserService.UpdateIsLanguageAskedOnCreate(_userId, true);
        }

        internal bool CreatePet()
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
                NextRandomEventNotificationTime = DateTime.UtcNow.AddMinutes(25),
                EXP = 0,
                Gold = -1,
                HP = 100,
                Joy = 70,
                Fatigue = 0,
                Satiety = 80,
                Type = null
            });
            Log.Information($"Pet of UserID: {_userId} has been added to Db");

            _appServices.UserService.UpdateIsPetNameAskedOnCreate(_userId, false);

            var toSend = new AnswerMessage()
            {
                Text = ConfirmedName,
                StickerId = StickersId.PetConfirmedName_Cat,
                ReplyMarkup = null,
                InlineKeyboardMarkup = null
            };

            _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId);
            return true;
        }
        internal bool ApplyNewName()
        {
            var msgText = _message?.Text;
            if (string.IsNullOrEmpty(msgText))
                return false;

            if (string.IsNullOrEmpty(newLanguage))
            {
                var toSendLanguagesAgain = new AnswerMessage()
                {
                    Text = ChangeLanguage,
                    StickerId = StickersId.ChangeLanguageSticker,
                    ReplyMarkup = LanguagesMarkup,
                    InlineKeyboardMarkup = null
                };

                _appServices.BotControlService.SendAnswerMessageAsync(toSendLanguagesAgain, _userId);
                return false;
            }

            _appServices.UserService.UpdateLanguage(_userId, newLanguage);
            Culture = new CultureInfo(newLanguage);

            if (!isLanguageChanged)
            {
                _appServices.UserService.UpdateIsLanguageAskedOnCreate(_userId, false);
                return true;
            }

            string stickerToSend = newLanguage.Language() switch
            {
                Language.Polish => StickersId.PolishLanguageSetSticker,
                Language.English => StickersId.EnglishLanguageSetSticker,
                Language.Belarusian => StickersId.BelarussianLanguageSetSticker,
                Language.Russian => StickersId.RussianLanguageSetSticker,
                _ => null,
            };
            var toSend = new AnswerMessage()
            {
                Text = ConfirmedLanguage,
                StickerId = stickerToSend,
                ReplyMarkup = new ReplyKeyboardRemove()
            };

            _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId);

            return true;
        }
        internal bool ApplyNewLanguage(bool isLanguageChanged = false)
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
                    Text = ChangeLanguage,
                    StickerId = StickersId.ChangeLanguageSticker,
                    ReplyMarkup = LanguagesMarkup,
                    InlineKeyboardMarkup = null
                };

                _appServices.BotControlService.SendAnswerMessageAsync(toSendLanguagesAgain, _userId);
                return false;
            }

            _appServices.UserService.UpdateLanguage(_userId, newLanguage);
            Culture = new CultureInfo(newLanguage);

            if (!isLanguageChanged)
            {
                _appServices.UserService.UpdateIsLanguageAskedOnCreate(_userId, false);
                return true;
            }

            string stickerToSend = newLanguage.Language() switch
            {
                Language.Polish => StickersId.PolishLanguageSetSticker,
                Language.English => StickersId.EnglishLanguageSetSticker,
                Language.Belarusian => StickersId.BelarussianLanguageSetSticker,
                Language.Russian => StickersId.RussianLanguageSetSticker,
                _ => null,
            };
            var toSend = new AnswerMessage()
            {
                Text = ConfirmedLanguage,
                StickerId = stickerToSend,
                ReplyMarkup = new ReplyKeyboardRemove()
            };

            _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId);

            return true;
        }
        internal void SendWelcomeText()
        {
            var toSend =  new AnswerMessage()
            {
                Text = Welcome,
                StickerId = StickersId.WelcomeSticker,
                ReplyMarkup = new ReplyKeyboardRemove()
            };
            _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId);
        }
        internal void AskForAPetName()
        {
            var toSend =  new AnswerMessage()
            {
                Text = ChooseName,
                StickerId = StickersId.PetChooseName_Cat
            };
            _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId);

            _appServices.UserService.UpdateIsPetNameAskedOnCreate(_userId, true);
        }

        internal void UpdateIndicators()
        {
            Telegram.Bot.Types.User userFromMsg = _message?.From ?? _callback?.From;
            var petDB = _appServices.PetService.Get(_userId);
            var petResult = new Pet().Clone(petDB);

            if (userFromMsg == null || petDB == null)
                return;

            int minuteCounter = (int)(DateTime.UtcNow - petDB.LastUpdateTime).TotalMinutes;

            //EXP
            petResult.EXP = UpdateIndicatorEXP(minuteCounter, petDB);

            //Satiety & HP
            var satietyHp = UpdateIndicatorSatietyAndHP(minuteCounter, petDB);
            petResult.Satiety = satietyHp.Item1;
            petResult.HP = satietyHp.Item2;

            //Joy
            petResult.Joy = UpdateIndicatorJoy(minuteCounter, petDB);

            //Fatigue
            petResult.Fatigue = UpdateIndicatorFatigue(minuteCounter, petDB);

            //Hygiene
            petResult.Hygiene = UpdateIndicatorHygiene(minuteCounter, petDB);

            //Sleeping
            if (petDB.CurrentStatus == (int)CurrentStatus.Sleeping)
            {
                var sleepResult =  UpdateIndicatorSleeping(petDB);
                var currentStatus = sleepResult.Item1;
                var currentFatigue = sleepResult.Item2;

                petResult.CurrentStatus = (int)currentStatus;
                petResult.Fatigue = currentStatus != CurrentStatus.Active ? petResult.Fatigue : currentFatigue;
            }

            //Work
            if (petDB.CurrentStatus == (int)CurrentStatus.WorkingOnPC)
                petResult.CurrentStatus = (int)UpdateIndicatorWork(petDB);

            petResult.LastUpdateTime = DateTime.UtcNow;
            _appServices.PetService.Update(userFromMsg.Id, petResult);
        }
        internal bool CheckIsPetZeroHP() => _appServices.PetService.Get(_userId)?.HP <= 0;
        internal bool CheckIsPetGone() => _appServices.PetService.Get(_userId)?.IsGone ?? false;
        internal async void AfterDeath()
        {
            var petDB = _appServices.PetService.Get(_userId);
            var userDB = _appServices.UserService.Get(_userId);
            if (petDB == null)
                return;

            var petResult = new Pet().Clone(petDB);
            petResult.IsGone = true;

            Culture = new CultureInfo(userDB.Culture);
            string textToSend = string.Format(FarewellText, petResult.Name, userDB.Username);

            var toSend = new AnswerMessage()
            {
                Text = textToSend,
                StickerId = StickersId.PetGone_Cat,
            };

            _appServices.PetService.Update(_userId, petResult);
            _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId);
            _appServices.BotControlService.SendChatActionAsync(_userId, Telegram.Bot.Types.Enums.ChatAction.Typing);

            await Task.Delay(2500);
            AskForResurrect();
        }
        internal async void ToResurrectAnswer()
        {
            bool? answerFromUser = _message.Text == ResurrectYesText ? true : _message.Text == ResurrectNoText ? false : null;
            if (answerFromUser == null)
            {
                AskForResurrect();
                return;
            }

            if (answerFromUser == true)
            {
                if (_appServices.UserService.Get(_userId).Gold >= 1000)
                {
                    ResurrectPet();
                    return;
                }
                else //not enough gold to buy back
                {
                    Culture = _userCulture;
                    var toSend = new AnswerMessage()
                    {
                        Text = NotEnoughGoldToResurrect
                    };

                    _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId);
                    await Task.Delay(1000);
                    answerFromUser = false;
                }
            }

            if (answerFromUser == false)
            {
                Culture = _userCulture;
                var toSend = new AnswerMessage()
                {
                    Text = EpilogueText,
                    StickerId = StickersId.PetEpilogue_Cat
                };
                _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId);
                await Task.Delay(2500);
                KillThePet();
                return;
            }
        }

        private void CreateUserFromMessage(Message msg)
        {
            if (msg == null)
                return;

            var ads = Extensions.GetAdsProducerFromStart(msg.Text);
            if (ads != null)
                _appServices.AdsProducersService.AddOrInsert(ads);

            _appServices.UserService.Create(msg.From);


            Log.Information($"User {msg.From.Username ?? msg.From.Id.ToString()} has been added to Db");

            Culture = new CultureInfo(msg.From.LanguageCode ?? "ru");
        }
        private void CreateUserFromCallback(CallbackQuery callback)
        {
            var msg = callback.Message;

            if (msg == null)
                return;

            var ads = Extensions.GetAdsProducerFromStart(msg.Text);
            if (ads != null)
                _appServices.AdsProducersService.AddOrInsert(ads);

            _appServices.UserService.Create(callback.From);

            Log.Information($"User {msg.From.Username ?? msg.From.Id.ToString()} has been added to Db");

            Culture = new CultureInfo(msg.From.LanguageCode ?? "ru");
        }
        private void ResurrectPet()
        {
            var petDB = _appServices.PetService.Get(_userId);
            var userDB = _appServices.UserService.Get(_userId);
            petDB.IsGone = false;
            petDB.HP = 100;
            petDB.Satiety = 100;
            petDB.Fatigue = 50;
            userDB.Gold -= Costs.ResurrectPet;
            _appServices.PetService.Update(_userId, petDB);
            _appServices.UserService.Update(_userId, userDB);

            Culture = _userCulture;
            var toSend = new AnswerMessage()
            {
                Text = PetCameBackText,
                StickerId = StickersId.ResurrectedPetSticker
            };

            _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId);
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
                    Culture = Culture.ToString(),
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
        private void KillThePet()
        {
            _appServices.PetService.Remove(_userId);
            _appServices.AppleGameDataService.Delete(_userId);
            AskALanguage();
        }
        private void AskForResurrect()
        {
            Culture = _userCulture;
            var toResurrect = new AnswerMessage()
            {
                Text = ToResurrectQuestion,
                ReplyMarkup = new ReplyKeyboardMarkup(new List<KeyboardButton>()
                {
                    new KeyboardButton(ResurrectYesText),
                    new KeyboardButton(ResurrectNoText)
                })
                {
                    OneTimeKeyboard = true
                }
            };
            _appServices.BotControlService.SendAnswerMessageAsync(toResurrect, _userId);
        }

        #region Indicator Updaters

        private int UpdateIndicatorEXP(int minuteCounter, Pet pet)
        {
            var petResult = new Pet().Clone(pet);

            int toAddExp = minuteCounter * Factors.ExpFactor;
            petResult.EXP += toAddExp;

            if (petResult.EXP > 100)
            {
                petResult.Level += petResult.EXP / Factors.ExpToLvl;
                petResult.EXP -= (petResult.EXP / Factors.ExpToLvl) * Factors.ExpToLvl;
            }

            return petResult.EXP;
        }
        private (double, int) UpdateIndicatorSatietyAndHP(int minuteCounter, Pet pet)
        {
            var petResult = new Pet().Clone(pet);

            double decreaseSatiety = Math.Round(minuteCounter * Factors.StarvingFactor, 2);

            petResult.Satiety -= decreaseSatiety;
            petResult.Satiety = Math.Round(petResult.Satiety, 2);

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
            var petResult = new Pet().Clone(petDB);

            double toDecreaseHygiene = Math.Round(minuteCounter * Factors.HygieneFactor);

            petResult.Hygiene -= (int)toDecreaseHygiene;

            if (petResult.Hygiene < 0)
                petResult.Hygiene = 0;

            return petResult.Hygiene;
        }
        private int UpdateIndicatorJoy(int minuteCounter, Pet pet)
        {
            var petResult = new Pet().Clone(pet);

            double toDecreaseJoy = Math.Round(minuteCounter * Factors.JoyFactor);

            petResult.Joy -= (int)toDecreaseJoy;

            if (petResult.Joy < 0)
                petResult.Joy = 0;

            return petResult.Joy;
        }
        private int UpdateIndicatorFatigue(int minuteCounter, Pet pet)
        {
            var petResult = new Pet().Clone(pet);

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
            var petResult = new Pet().Clone(petDB);

            var remainsToSleepTime = petResult.ToWakeUpTime - DateTime.UtcNow;
            if (remainsToSleepTime <= TimeSpan.Zero)
            {
                petResult.CurrentStatus = (int)CurrentStatus.Active;
                petResult.Fatigue = 0;

                return ((CurrentStatus)petResult.CurrentStatus, petResult.Fatigue);
            }

            return ((CurrentStatus)petResult.CurrentStatus, petResult.Fatigue);
        }
        private CurrentStatus UpdateIndicatorWork(Pet petDB)
        {
            TimeSpan remainsTime = new TimesToWait().WorkOnPCToWait - (DateTime.UtcNow - petDB.StartWorkingTime);

            //if _callback handled when time of work is over
            if (remainsTime <= TimeSpan.Zero)
                return CurrentStatus.Active;

            return CurrentStatus.WorkingOnPC;
        }

        #endregion

    }
}
