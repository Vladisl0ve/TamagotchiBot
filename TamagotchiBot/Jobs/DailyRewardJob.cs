using Quartz;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TamagotchiBot.Models.Answers;
using TamagotchiBot.Services.Interfaces;
using TamagotchiBot.UserExtensions;
using Telegram.Bot.Exceptions;
using static TamagotchiBot.UserExtensions.Constants;

namespace TamagotchiBot.Jobs
{
    public class DailyRewardJob : IJob
    {
        private readonly IApplicationServices _appServices;

        public DailyRewardJob(IApplicationServices appServices)
        {
            _appServices = appServices;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var usersToNotify = UpdateAllDailyRewardUsersIds();
            Log.Information($"DailyRewardJob - {usersToNotify.Count} users");
            int counter = 0;
            foreach (var userId in usersToNotify)
            {
                var user = _appServices.UserService.Get(userId);
                var petType = Extensions.GetEnumPetType(_appServices.PetService.Get(userId)?.Type);
                try
                {
                    var toSend = new AnswerMessage()
                    {
                        Text = nameof(Resources.Resources.rewardNotification).UseCulture(user?.Culture),
                        StickerId = GetRandomDailyRewardSticker(petType),
                    };

                    await _appServices.BotControlService.SendAnswerMessageAsync(toSend, userId, false);

                    Log.Information($"Sent DailyRewardJob to {Extensions.GetLogUser(user)}");

                    counter++;
                    if (counter % 100 == 0)
                    {
                        Log.Information("Daily: Delay 3s...");
                        await Task.Delay(3000);
                    }
                }
                catch (ApiRequestException ex)
                {
                    if (ex.ErrorCode == 403) //Forbidden by user
                    {
                        Log.Warning($"{ex.Message} {Extensions.GetLogUser(user)}");

                        //remove all data about user
                        _appServices.ChatService.Remove(user.UserId);
                        _appServices.PetService.Remove(user.UserId);
                        _appServices.UserService.Remove(user.UserId);
                        _appServices.MetaUserService.Remove(user.UserId);
                        _appServices.AppleGameDataService.Delete(user.UserId);
                        _appServices.TicTacToeGameDataService.Delete(user.UserId);
                        _appServices.HangmanGameDataService.Delete(user.UserId);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                }
            }
        }

        private List<long> UpdateAllDailyRewardUsersIds()
        {
            List<long> usersToNotify = new();
            var petsDB = _appServices.PetService.GetAll().Where(p => p.Name != null);
            foreach (var pet in petsDB)
            {
                var user = _appServices.UserService.Get(pet.UserId);

                try
                {
#if DEBUG_NOTIFY
                    if (user.NextDailyRewardNotificationTime < DateTime.UtcNow && user.GotDailyRewardTime.AddSeconds(1) < DateTime.UtcNow)
                    {
                        _appServices.UserService.UpdateNextDailyRewardNotificationTime(user.UserId, DateTime.UtcNow.AddSeconds(1));
                        usersToNotify.Add(user.UserId);
                    }
#else
                    if (user.NextDailyRewardNotificationTime < DateTime.UtcNow && user.GotDailyRewardTime.AddDays(1) < DateTime.UtcNow)
                    {
                        _appServices.UserService.UpdateNextDailyRewardNotificationTime(user.UserId, DateTime.UtcNow.AddDays(1));
                        usersToNotify.Add(user.UserId);
                    }
#endif
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error on UpdateAllDailyRewardUsersIds for userId: {pet.UserId}");
                }
            }

            return usersToNotify;
        }

        private string GetRandomDailyRewardSticker(TamagotchiBot.UserExtensions.Constants.PetType petType = TamagotchiBot.UserExtensions.Constants.PetType.UNKNOWN)
        {
            var random = new Random().Next(0, 6);

            var result = random switch
            {
                1 => Constants.StickersId.DailyRewardNotificationSticker_1,
                2 => Constants.StickersId.DailyRewardNotificationSticker_2,
                3 => Constants.StickersId.DailyRewardNotificationSticker_3,
                4 => Constants.StickersId.DailyRewardNotificationSticker_4,
                5 => Constants.StickersId.DailyRewardNotificationSticker_5,
                _ => Constants.StickersId.DailyRewardNotificationSticker_3,
            };

            if (new Random().Next(0, 2) == 0 && petType != TamagotchiBot.UserExtensions.Constants.PetType.UNKNOWN)
                result = StickersId.GetStickerByType(nameof(StickersId.PetDailyRewardSticker_Cat), petType);

            return result;
        }
    }
}
