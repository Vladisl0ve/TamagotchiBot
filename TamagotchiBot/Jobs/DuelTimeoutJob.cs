using Quartz;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using TamagotchiBot.Models.Mongo;
using TamagotchiBot.Services.Interfaces;
using TamagotchiBot.UserExtensions;

namespace TamagotchiBot.Jobs
{
    public class DuelTimeoutJob : IJob
    {
        private readonly IApplicationServices _appServices;

        public DuelTimeoutJob(IApplicationServices appServices)
        {
            _appServices = appServices;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var activeDuelMetaUsers = GetAllActiveDuels();
            Log.Information($"Active Duels MP timer started - {activeDuelMetaUsers.Count} users");

            int counterDuelsEnded = 0;
            TimeSpan duelLifeTime;

            duelLifeTime = Constants.TimesToWait.DuelCDToWait; //5 min life

            foreach (var metaUser in activeDuelMetaUsers)
            {
                if (metaUser.DuelStartTime + duelLifeTime < DateTime.UtcNow)
                {
                    var petDB = _appServices.PetService.Get(metaUser.UserId);
                    var userDB = _appServices.UserService.Get(metaUser.UserId);

                    if (petDB == null || userDB == null)
                    {
                        _appServices.MetaUserService.Remove(metaUser.UserId);
                        Log.Information($"Deleted metauser id: {metaUser.UserId}");
                        continue;
                    }

                    var userLink = Extensions.GetPersonalLink(metaUser.UserId, userDB.FirstName ?? "0_o");
                    var petNameEncoded = HttpUtility.HtmlEncode(petDB.Name ?? "^_^");

                    string textToSend = string.Format(nameof(Resources.Resources.DuelMPTimeout).UseCulture(userDB.Culture), userLink, petNameEncoded, Constants.Costs.DuelGold);
                    await _appServices.BotControlService.EditMessageTextAsync(metaUser.ChatDuelId, metaUser.MsgDuelId, textToSend, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
                    await _appServices.BotControlService.DeleteMessageAsync(metaUser.ChatDuelId, metaUser.MsgCreatorDuelId, false);
                    _appServices.UserService.UpdateGold(metaUser.UserId, userDB.Gold + Constants.Costs.DuelGold);
                    _appServices.MetaUserService.UpdateChatDuelId(metaUser.UserId, -1);
                    _appServices.MetaUserService.UpdateMsgDuelId(metaUser.UserId, -1);
                    _appServices.MetaUserService.UpdateMsgCreatorDuelId(metaUser.UserId, -1);
                    counterDuelsEnded++;
                }
            }

            Log.Information($"Active Duels MP timer ended - {counterDuelsEnded} duels closed");
        }

        private List<MetaUser> GetAllActiveDuels() => _appServices.MetaUserService.GetAll().Where(mu => mu.MsgDuelId > 0).ToList();
    }
}
