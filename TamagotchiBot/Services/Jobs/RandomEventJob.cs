using Quartz;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TamagotchiBot.Services.Interfaces;
using TamagotchiBot.UserExtensions;

namespace TamagotchiBot.Services.Jobs
{
    public class RandomEventJob : IJob
    {
        private readonly IApplicationServices _appServices;
        private readonly RandomEventService _randomEventService;

        public RandomEventJob(IApplicationServices appServices, RandomEventService randomEventService)
        {
            _appServices = appServices;
            _randomEventService = randomEventService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
#if !DEBUG_NOTIFY
            if (DateTime.UtcNow.Hour < 4 || DateTime.UtcNow.Hour > 20)
            {
                Log.Information($"RandomEventNotification - Sleep time for [20:00 - 04:00] UTC");
                return;
            }
#endif
            var usersToNotify = UpdateAllRandomEventUsersIds();
            var counter = 0;
            Log.Information($"RandomEventNotification - {usersToNotify.Count} users");
            foreach (var userId in usersToNotify)
            {
                var user = _appServices.UserService.Get(userId);

                if (user == null)
                    continue;

                try
                {
                    await _randomEventService.DoRandomEvent(user);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error on RandomEvent for userId: {userId}");
                }


                counter++;
                if (counter % 100 == 0)
                {
                    Log.Information("Rand: Delay 3s...");
                    await Task.Delay(3000);
                }
                Log.Information($"Sent RandomEventNotification to {Extensions.GetLogUser(user)}");
            }
            if (usersToNotify.Count > 1)
                Log.Information($"RandomEventNotification timer completed - {usersToNotify.Count} users");
        }

        private List<long> UpdateAllRandomEventUsersIds()
        {
            List<long> usersToNotify = new();
            var petsDB = _appServices.PetService.GetAll().Where(p => p.Name != null && !p.IsGone);

            foreach (var pet in petsDB)
            {
                var user = _appServices.UserService.Get(pet.UserId);
                if (user == null)
                {
                    Log.Fatal($"USER NOT FOUND ON RANDOM EVENT {pet.UserId}");
                    continue;
                }

                if (pet.NextRandomEventNotificationTime < DateTime.UtcNow)
                {
                    int minutesToAdd = new Random().Next(-15, 30);

#if DEBUG_NOTIFY
                    _appServices.PetService.UpdateNextRandomEventNotificationTime(user.UserId, DateTime.UtcNow.AddSeconds(1));
#else
                    _appServices.PetService.UpdateNextRandomEventNotificationTime(user.UserId, DateTime.UtcNow.AddHours(2).AddMinutes(minutesToAdd));
#endif
                    usersToNotify.Add(user.UserId);
                }
            }
            return usersToNotify;
        }
    }
}
