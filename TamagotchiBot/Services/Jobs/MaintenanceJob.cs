using Quartz;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;
using TamagotchiBot.Services.Interfaces;

namespace TamagotchiBot.Services.Jobs
{
    public class MaintenanceJob : IJob
    {
        private readonly IApplicationServices _appServices;

        public MaintenanceJob(IApplicationServices appServices)
        {
            _appServices = appServices;
        }

        public Task Execute(IJobExecutionContext context)
        {
            if (!_appServices.SInfoService.GetDoMaintainWorks())
                return Task.CompletedTask;

            Log.Information($"MAINTAIN JOB STARTED");
            _appServices.SInfoService.DisableMaintainWorks();

            int usersDeletedPartly = 0;
            int usersDeletedFull = 0;

            var allUsersData = _appServices.AllUsersDataService.GetAll().Select(a => a.UserId);
            foreach (var userId in allUsersData)
            {
                var petDB = _appServices.PetService.Get(userId);
                var userDB = _appServices.UserService.Get(userId);

                if (petDB == null && userDB == null)
                {
                    _appServices.ChatService.Remove(userId);
                    _appServices.MetaUserService.Remove(userId);
                    _appServices.AppleGameDataService.Delete(userId);
                    _appServices.TicTacToeGameDataService.Delete(userId);
                    _appServices.HangmanGameDataService.Delete(userId);

                    Log.Information($"DELETED (partly) id: {userId}");
                    usersDeletedPartly++;

                    continue;
                }

                if (petDB == null || petDB.Name == null || userDB == null)
                {
                    _appServices.ChatService.Remove(userId);
                    _appServices.PetService.Remove(userId);
                    _appServices.UserService.Remove(userId);
                    _appServices.MetaUserService.Remove(userId);
                    _appServices.AppleGameDataService.Delete(userId);
                    _appServices.TicTacToeGameDataService.Delete(userId);
                    _appServices.HangmanGameDataService.Delete(userId);

                    Log.Information($"DELETED id: {userId}");
                    usersDeletedFull++;
                }
            }

            Log.Warning($"DELETED USERS ON MAINTAIN: partly {usersDeletedPartly}; full {usersDeletedFull}");
            Log.Information($"MAINTAINS ARE OVER");
            return Task.CompletedTask;
        }
    }
}
