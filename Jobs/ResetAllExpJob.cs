using Quartz;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TamagotchiBot.Models.Mongo;
using TamagotchiBot.Services.Interfaces;
using TamagotchiBot.Services.Mongo;
using TamagotchiBot.UserExtensions;

namespace TamagotchiBot.Jobs
{
    public class ResetAllExpJob : IJob
    {
        private readonly IApplicationServices _appServices;
        public ResetAllExpJob(IApplicationServices applicationServices)
        {
            _appServices = applicationServices;
        }

        private List<long> RewardUsersByPetExpRanking()
        {
            var topPets = _appServices.PetService.GetAll()
                .OrderByDescending(p => p.Level)
                .ThenByDescending(p => p.LastUpdateTime)
                .Take(10)
                .ToList(); //First 10 top-level pets

            List<long> rewardedUserIds = new List<long>();

            int rank = 1;
            foreach (var pet in topPets)
            {
                var user = _appServices.UserService.Get(pet.UserId);
                if (user != null)
                {
                    int reward = rank switch
                    {
                        1 => Constants.GoldForTopExpRanking.Top1,
                        2 => Constants.GoldForTopExpRanking.Top2,
                        3 => Constants.GoldForTopExpRanking.Top3,
                        4 => Constants.GoldForTopExpRanking.Top4_10,
                        5 => Constants.GoldForTopExpRanking.Top4_10,
                        6 => Constants.GoldForTopExpRanking.Top4_10,
                        7 => Constants.GoldForTopExpRanking.Top4_10,
                        8 => Constants.GoldForTopExpRanking.Top4_10,
                        9 => Constants.GoldForTopExpRanking.Top4_10,
                        10 => Constants.GoldForTopExpRanking.Top4_10,
                        _ => throw new System.NotImplementedException("No reward for 11th pet")
                    };

                    _appServices.UserService.AddGold(user.UserId, reward);
                    rewardedUserIds.Add(user.UserId);
                }
                rank++;
            }

            return rewardedUserIds;
        }
        public Task Execute(IJobExecutionContext context)
        {
            var rewardedUserIds = RewardUsersByPetExpRanking();
            _appServices.PetService.ResetAllExpAndLevel();
            Log.Debug("ResetAllExpJob executed: All pets' experience has been reset.");
            return Task.CompletedTask;
        }
    }
}
