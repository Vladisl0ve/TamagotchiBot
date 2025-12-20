using Quartz;
using Serilog;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TamagotchiBot.Models.Answers;
using TamagotchiBot.Services.Interfaces;
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

        private Dictionary<int, (long id, int rewardGold, int rewardDiamonds)> RewardUsersByPetExpRanking()
        {
            var topPets = _appServices.PetService.GetAll()
                .OrderByDescending(p => p.Level)
                .ThenByDescending(p => p.LastUpdateTime)
                .Take(10)
                .ToList(); //First 10 top-level pets

            Dictionary<int, (long id, int rewardGold, int rewardDiamonds)> rewardedUserIds = [];

            int rank = 1;
            foreach (var pet in topPets)
            {
                var user = _appServices.UserService.Get(pet.UserId);
                if (user != null)
                {
                    int rewardGold = rank switch
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
                    int rewardDiamonds = rank switch
                    {
                        1 => Constants.DiamondsForTopExpRanking.Top1,
                        2 => Constants.DiamondsForTopExpRanking.Top2,
                        3 => Constants.DiamondsForTopExpRanking.Top3,
                        4 => Constants.DiamondsForTopExpRanking.Top4_10,
                        5 => Constants.DiamondsForTopExpRanking.Top4_10,
                        6 => Constants.DiamondsForTopExpRanking.Top4_10,
                        7 => Constants.DiamondsForTopExpRanking.Top4_10,
                        8 => Constants.DiamondsForTopExpRanking.Top4_10,
                        9 => Constants.DiamondsForTopExpRanking.Top4_10,
                        10 => Constants.DiamondsForTopExpRanking.Top4_10,
                        _ => throw new System.NotImplementedException("No reward for 11th pet")
                    };

                    _appServices.UserService.AddGold(user.UserId, rewardGold);
                    _appServices.UserService.UpdateDiamonds(user.UserId, user.Diamonds + rewardDiamonds);
                    rewardedUserIds.Add(rank, (user.UserId, rewardGold, rewardDiamonds));
                }
                rank++;
            }

            return rewardedUserIds;
        }
        
        private async Task SendRewardMessages(Dictionary<int, (long id, int rewardGold, int rewardDiamonds)> rewardedUserIds)
        {
            foreach (var rankUserIdKVPair in rewardedUserIds)
            {
                var user = _appServices.UserService.Get(rankUserIdKVPair.Value.id);
                if (user != null)
                {
                    var userCulture = new CultureInfo(user.Culture ?? "ru");

                    string texttoSend = string.Format(nameof(Resources.Resources.MonthlyGoldReward).UseCulture(userCulture), rankUserIdKVPair.Key, rankUserIdKVPair.Value.rewardGold);
                    if (rankUserIdKVPair.Value.rewardDiamonds > 0)
                    {
                        texttoSend += string.Format(nameof(Resources.Resources.MonthlyRewardAdditionalDiamonds).UseCulture(userCulture), rankUserIdKVPair.Value.rewardDiamonds);
                    }

                    AnswerMessage answerMessage = new AnswerMessage()
                    {
                        Text = texttoSend,
                        ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                        StickerId = Constants.StickersId.MonthlyRewardSticker,
                    };
                    await _appServices.BotControlService.SendAnswerMessageAsync(answerMessage, rankUserIdKVPair.Value.id);
                }
            }
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var rewardedUserIds = RewardUsersByPetExpRanking();
            await SendRewardMessages(rewardedUserIds);
            _appServices.PetService.ResetAllExpAndLevel();
            Log.Debug("ResetAllExpJob executed: All pets' experience has been reset.");
        }
    }
}
