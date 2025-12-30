using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TamagotchiBot.Models.Answers;
using TamagotchiBot.Models.Mongo;
using TamagotchiBot.Services.Interfaces;
using TamagotchiBot.UserExtensions;
using static TamagotchiBot.UserExtensions.Constants;

namespace TamagotchiBot.Services
{
    public class RandomEventService
    {
        private readonly IApplicationServices _appServices;

        public RandomEventService(IApplicationServices appServices)
        {
            _appServices = appServices;
        }

        public async Task DoRandomEvent(Models.Mongo.User user)
        {
            var random = new Random().Next(20);
            switch (random)
            {
                case 0:
                    await RandomEventRaindow(user);
                    break;
                case 1:
                    await RandomEventStomachache(user);
                    break;
                case 2:
                    await RandomEventStepOnFoot(user);
                    break;
                case 3:
                    await RandomEventFriendMet(user);
                    break;
                case 4:
                    await RandomEventHotdog(user);
                    break;
                case 5:
                    await RandomEventNiceFlower(user);
                    break;
                case 6:
                    await RandomEventWatermelon(user);
                    break;
                case 7:
                    await RandomEventPlayComputerGames(user);
                    break;
                case 8:
                    await RandomEventFoundCoin(user);
                    break;
                case 9:
                    await RandomEventLostCoin(user);
                    break;
                case 10:
                    await RandomEventHiccups(user);
                    break;
                case 11:
                    await RandomEventFoundToy(user);
                    break;
                case 12:
                    await RandomEventMosquitoBite(user);
                    break;
                case 13:
                    await RandomEventRainyDay(user);
                    break;
                case 14:
                    await RandomEventNiceDream(user);
                    break;
                case 15:
                    await RandomEventBadDream(user);
                    break;
                case 16:
                    await RandomEventFoundTastySnack(user);
                    break;
                case 17:
                    await RandomEventWarmSun(user);
                    break;
                default:
                    await RandomEventNotify(user);
                    break;
            }
        }

        #region RandomEvents
        private async Task RandomEventStomachache(Models.Mongo.User user)
        {
            var petDB = _appServices.PetService.Get(user.UserId);
            var newSatiety = petDB.Satiety - 15;
            var newHP = petDB.HP - 5;

            _appServices.PetService.UpdateSatiety(user.UserId, newSatiety);
            _appServices.PetService.UpdateHP(user.UserId, newHP);

            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            var toSend = new AnswerMessage()
            {
                StickerId = Constants.StickersId.RandomEventStomachache,
                Text = string.Format(nameof(Resources.Resources.RandomEventStomachache).UseCulture(user.Culture), Extensions.GetTypeEmoji(petDB.Type)),
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        private async Task RandomEventRaindow(Models.Mongo.User user)
        {
            var petDB = _appServices.PetService.Get(user.UserId);
            if (petDB == null) return;
            int newJoy = petDB.Joy + 10;
            _appServices.PetService.UpdateJoy(user.UserId, newJoy);

            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            var toSend = new AnswerMessage()
            {
                StickerId = Constants.StickersId.RandomEventRainbow,
                Text = nameof(Resources.Resources.RandomEventRainbow).UseCulture(user.Culture)
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        private async Task RandomEventFriendMet(Models.Mongo.User user)
        {
            var petDB = _appServices.PetService.Get(user.UserId);
            var userDB = _appServices.UserService.Get(user.UserId);
            int newGold = userDB.Gold + 15;
            int newJoy = petDB.Joy + 40;

            _appServices.UserService.UpdateGold(user.UserId, newGold);
            _appServices.PetService.UpdateJoy(user.UserId, newJoy);

            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            var toSend = new AnswerMessage()
            {
                StickerId = Constants.StickersId.RandomEventFriendMet,
                Text = nameof(Resources.Resources.RandomEventFriendMet).UseCulture(user.Culture)
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        private async Task RandomEventHotdog(Models.Mongo.User user)
        {
            var userDB = _appServices.UserService.Get(user.UserId);
            var petDB = _appServices.PetService.Get(user.UserId);
            var newSatiety = petDB.Satiety + 40;
            int newGold = userDB.Gold + 20;

            _appServices.PetService.UpdateSatiety(user.UserId, newSatiety);
            _appServices.UserService.UpdateGold(user.UserId, newGold);

            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            var toSend = new AnswerMessage()
            {
                StickerId = Constants.StickersId.RandomEventHotdog,
                Text = nameof(Resources.Resources.RandomEventHotdog).UseCulture(user.Culture)
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        private async Task RandomEventNotify(Models.Mongo.User user)
        {
            int rand = new Random().Next(3);
            var userPetType = Extensions.GetEnumPetType(_appServices.PetService.Get(user.UserId)?.Type);
            var emoji = Extensions.GetTypeEmoji(userPetType);
            var notifyText = new List<string>()
                {
                    string.Format(nameof(Resources.Resources.ReminderNotifyText1).UseCulture(user.Culture), emoji),
                    string.Format(nameof(Resources.Resources.ReminderNotifyText2).UseCulture(user.Culture), emoji),
                    string.Format(nameof(Resources.Resources.ReminderNotifyText3).UseCulture(user.Culture), emoji)
                };

            string toSendText = notifyText.ElementAtOrDefault(rand) ?? string.Format(nameof(Resources.Resources.ReminderNotifyText1).UseCulture(user.Culture), emoji);

            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            var toSend = new AnswerMessage()
            {
                StickerId = StickersId.GetStickerByType(nameof(StickersId.PetBoredSticker_Cat), userPetType),
                Text = toSendText
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        private async Task RandomEventStepOnFoot(Models.Mongo.User user)
        {
            var petDB = _appServices.PetService.Get(user.UserId);
            var newSatiety = petDB.Satiety - 10;
            int newHP = petDB.HP - 1;

            _appServices.PetService.UpdateSatiety(user.UserId, newSatiety, true);
            _appServices.PetService.UpdateHP(user.UserId, newHP);

            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            var toSend = new AnswerMessage()
            {
                StickerId = StickersId.GetStickerByType(nameof(StickersId.RandomEventStepOnFootSticker_Cat), petDB.Type),
                Text = nameof(Resources.Resources.RandomEventStepOnFoot).UseCulture(user.Culture)
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }
        private async Task RandomEventNiceFlower(Models.Mongo.User user)
        {
            var petDB = _appServices.PetService.Get(user.UserId);
            var newJoy = petDB.Joy + 10;

            _appServices.PetService.UpdateJoy(user.UserId, newJoy, true);

            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            var toSend = new AnswerMessage()
            {
                StickerId = StickersId.RandomEventNiceFlower,
                Text = string.Format(
                    nameof(Resources.Resources.RandomEventNiceFlower).UseCulture(user.Culture),
                    Extensions.GetTypeEmoji(petDB.Type)
                    )
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }
        private async Task RandomEventWatermelon(Models.Mongo.User user)
        {
            var petDB = _appServices.PetService.Get(user.UserId);
            var newSatiety = petDB.Satiety + 15;

            _appServices.PetService.UpdateSatiety(user.UserId, newSatiety, true);

            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            var toSend = new AnswerMessage()
            {
                StickerId = Constants.StickersId.RandomEventWatermelon,
                Text = string.Format(nameof(Resources.Resources.RandomEventWatermelon).UseCulture(user.Culture), Extensions.GetTypeEmoji(petDB.Type))
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }
        private async Task RandomEventPlayComputerGames(Models.Mongo.User user)
        {
            var petDB = _appServices.PetService.Get(user.UserId);
            var newJoy = petDB.Joy + 30;

            _appServices.PetService.UpdateJoy(user.UserId, newJoy, true);

            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            var toSend = new AnswerMessage()
            {
                StickerId = StickersId.GetStickerByType(nameof(StickersId.RandomEventPlayComputerSticker_Cat), petDB.Type),
                Text = nameof(Resources.Resources.RandomEventPlayComputerGames).UseCulture(user.Culture)
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        private async Task RandomEventFoundCoin(Models.Mongo.User user)
        {
            var userDB = _appServices.UserService.Get(user.UserId);
            var petDB = _appServices.PetService.Get(user.UserId);
            int newGold = userDB.Gold + 20;

            _appServices.UserService.UpdateGold(user.UserId, newGold);
            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            var toSend = new AnswerMessage()
            {
                StickerId = StickersId.GetStickerByType(nameof(StickersId.PetDailyRewardSticker_Cat), petDB.Type),
                Text = string.Format(nameof(Resources.Resources.RandomEventFoundCoin).UseCulture(user.Culture), Extensions.GetTypeEmoji(petDB.Type))
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        private async Task RandomEventLostCoin(Models.Mongo.User user)
        {
            var userDB = _appServices.UserService.Get(user.UserId);
            var petDB = _appServices.PetService.Get(user.UserId);
            int newGold = userDB.Gold - 10;
            if (newGold < 0) newGold = 0;

            _appServices.UserService.UpdateGold(user.UserId, newGold);
            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            var toSend = new AnswerMessage()
            {
                StickerId = StickersId.GetStickerByType(nameof(StickersId.PetGoneSticker_Cat), petDB.Type), // Sad sticker
                Text = string.Format(nameof(Resources.Resources.RandomEventLostCoin).UseCulture(user.Culture), Extensions.GetTypeEmoji(petDB.Type))
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        private async Task RandomEventHiccups(Models.Mongo.User user)
        {
            var petDB = _appServices.PetService.Get(user.UserId);
            var newSatiety = petDB.Satiety - 5;

            _appServices.PetService.UpdateSatiety(user.UserId, newSatiety, true);
            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            var toSend = new AnswerMessage()
            {
                StickerId = StickersId.GetStickerByType(nameof(StickersId.PetHospitalLowHPSticker_Cat), petDB.Type),
                Text = string.Format(nameof(Resources.Resources.RandomEventHiccups).UseCulture(user.Culture), Extensions.GetTypeEmoji(petDB.Type))
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        private async Task RandomEventFoundToy(Models.Mongo.User user)
        {
            var petDB = _appServices.PetService.Get(user.UserId);
            var newJoy = petDB.Joy + 10;

            _appServices.PetService.UpdateJoy(user.UserId, newJoy, true);
            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            var toSend = new AnswerMessage()
            {
                StickerId = StickersId.GetStickerByType(nameof(StickersId.PetGameroomSticker_Cat), petDB.Type),
                Text = string.Format(nameof(Resources.Resources.RandomEventFoundToy).UseCulture(user.Culture), Extensions.GetTypeEmoji(petDB.Type))
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        private async Task RandomEventMosquitoBite(Models.Mongo.User user)
        {
            var petDB = _appServices.PetService.Get(user.UserId);
            var newHP = petDB.HP - 5;

            _appServices.PetService.UpdateHP(user.UserId, newHP);
            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            var toSend = new AnswerMessage()
            {
                StickerId = StickersId.GetStickerByType(nameof(StickersId.PetHospitalLowHPSticker_Cat), petDB.Type),
                Text = string.Format(nameof(Resources.Resources.RandomEventMosquitoBite).UseCulture(user.Culture), Extensions.GetTypeEmoji(petDB.Type))
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        private async Task RandomEventRainyDay(Models.Mongo.User user)
        {
            var petDB = _appServices.PetService.Get(user.UserId);
            var newJoy = petDB.Joy - 10;

            _appServices.PetService.UpdateJoy(user.UserId, newJoy, true);
            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            var toSend = new AnswerMessage()
            {
                StickerId = StickersId.GetStickerByType(nameof(StickersId.PetBoredSticker_Cat), petDB.Type),
                Text = string.Format(nameof(Resources.Resources.RandomEventRainyDay).UseCulture(user.Culture), Extensions.GetTypeEmoji(petDB.Type))
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        private async Task RandomEventNiceDream(Models.Mongo.User user)
        {
            var petDB = _appServices.PetService.Get(user.UserId);
            var newJoy = petDB.Joy + 10;

            _appServices.PetService.UpdateJoy(user.UserId, newJoy, true);
            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            var toSend = new AnswerMessage()
            {
                StickerId = StickersId.GetStickerByType(nameof(StickersId.PetSleepSticker_Cat), petDB.Type),
                Text = string.Format(nameof(Resources.Resources.RandomEventNiceDream).UseCulture(user.Culture), Extensions.GetTypeEmoji(petDB.Type))
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        private async Task RandomEventBadDream(Models.Mongo.User user)
        {
            var petDB = _appServices.PetService.Get(user.UserId);
            var newJoy = petDB.Joy - 10;

            _appServices.PetService.UpdateJoy(user.UserId, newJoy, true);
            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            var toSend = new AnswerMessage()
            {
                StickerId = StickersId.GetStickerByType(nameof(StickersId.PetTooTiredSticker_Cat), petDB.Type),
                Text = string.Format(nameof(Resources.Resources.RandomEventBadDream).UseCulture(user.Culture), Extensions.GetTypeEmoji(petDB.Type))
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        private async Task RandomEventFoundTastySnack(Models.Mongo.User user)
        {
            var petDB = _appServices.PetService.Get(user.UserId);
            var newSatiety = petDB.Satiety + 15;

            _appServices.PetService.UpdateSatiety(user.UserId, newSatiety, true);
            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            var toSend = new AnswerMessage()
            {
                StickerId = StickersId.GetStickerByType(nameof(StickersId.PetKitchenSticker_Cat), petDB.Type),
                Text = string.Format(nameof(Resources.Resources.RandomEventFoundTastySnack).UseCulture(user.Culture), Extensions.GetTypeEmoji(petDB.Type))
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        private async Task RandomEventWarmSun(Models.Mongo.User user)
        {
            var petDB = _appServices.PetService.Get(user.UserId);
            var newJoy = petDB.Joy + 10;

            _appServices.PetService.UpdateJoy(user.UserId, newJoy, true);
            _appServices.PetService.UpdateGotRandomEventTime(user.UserId, DateTime.UtcNow);

            var toSend = new AnswerMessage()
            {
                StickerId = StickersId.GetStickerByType(nameof(StickersId.PetInfoSticker_Cat), petDB.Type),
                Text = string.Format(nameof(Resources.Resources.RandomEventWarmSun).UseCulture(user.Culture), Extensions.GetTypeEmoji(petDB.Type))
            };
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, user.UserId, false);
        }

        #endregion
    }
}
