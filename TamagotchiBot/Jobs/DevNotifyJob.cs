using Quartz;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TamagotchiBot.Database;
using TamagotchiBot.Services.Helpers;
using TamagotchiBot.Services.Interfaces;
using Telegram.Bot.Exceptions;

namespace TamagotchiBot.Jobs
{
    public class DevNotifyJob : IJob
    {
        private readonly IApplicationServices _appServices;
        private readonly IEnvsSettings _envs;

        public DevNotifyJob(IApplicationServices appServices, IEnvsSettings envs)
        {
            _appServices = appServices;
            _envs = envs;
        }

        public async Task Execute(IJobExecutionContext context)
        {
#if !RELEASE
            return;
#endif
            DateTime nextDevNotifyDB = _appServices.SInfoService.GetNextDevNotify();
            if (nextDevNotifyDB < DateTime.UtcNow)
            {
                await SendDevNotify();

                var nextDevNotify = DateTime.UtcNow + _envs.DevNotifyEvery;
                _appServices.SInfoService.UpdateNextDevNotify(nextDevNotify);
            }
        }

        private async Task SendDevNotify()
        {
            if (_envs?.ChatsToDevNotify == null)
            {
                Log.Warning("No chats do DEV notify");
                return;
            }

            var chatsToNotify = new List<string>(_envs.ChatsToDevNotify);

            foreach (var chatId in chatsToNotify)
            {
                if (!long.TryParse(chatId, out long parsedChatId))
                    continue;

                try
                {
                    await _appServices.BotControlService.SendTextMessageAsync(parsedChatId, $"Tamagotchi is alive! {DateTime.UtcNow:g}UTC", toLog: false);

                    var dailyInfoToday = _appServices.DailyInfoService.GetToday();

                    if (dailyInfoToday == null ||
                        (dailyInfoToday != null && (DateTime.UtcNow - dailyInfoToday.DateInfo) > _envs.DevExtraNotifyEvery))
                    {
                        Log.Information("Sent extra dev notify");
                        await _appServices.BotControlService.SendTextMessageAsync(parsedChatId, ToSendExtraDevNotify(), toLog: false);
                    }
                }
                catch (ApiRequestException ex)
                {
                    if (ex.ErrorCode == 403) //Forbidden by user
                    {
                        Log.Warning($"{ex.Message}, id: {chatId}");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                }
            }

        }
        private string ToSendExtraDevNotify()
        {
            return DevNotifyHelper.UpdateAndGetDevNotifyReport(_appServices);
        }
    }
}
