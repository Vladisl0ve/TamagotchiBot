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

namespace TamagotchiBot.Services.Jobs
{
    public class ChangelogJob : IJob
    {
        private readonly IApplicationServices _appServices;

        public ChangelogJob(IApplicationServices appServices)
        {
            _appServices = appServices;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            if (!_appServices.SInfoService.GetDoSendChangelogs())
                return;

            var usersToNotify = GetAllActiveUsersIds();
            Log.Information($"ChangelogJob - {usersToNotify.Count} users");
            _appServices.SInfoService.DisableChangelogsSending();

            Log.Warning($"BD CLEANING ON CHANGELOGS STARTED");

            int usersSuccess = 0;
            int usersDeleted = 0;
            int usersForbidden = 0;
            foreach (var userDB in usersToNotify)
            {
                var petDB = _appServices.PetService.Get(userDB.UserId);

                if (petDB == null)
                {
                    _appServices.ChatService.Remove(userDB.UserId);
                    _appServices.PetService.Remove(userDB.UserId);
                    _appServices.UserService.Remove(userDB.UserId);
                    _appServices.MetaUserService.Remove(userDB.UserId);
                    _appServices.AppleGameDataService.Delete(userDB.UserId);
                    _appServices.TicTacToeGameDataService.Delete(userDB.UserId);
                    _appServices.HangmanGameDataService.Delete(userDB.UserId);


                    Log.Information($"DELETED {Extensions.GetLogUser(userDB)}");
                    usersDeleted++;
                    continue;
                }

                try
                {
                    var toSend = new AnswerMessage()
                    {
                        Text = nameof(Resources.Resources.changelog_onPatch_18).UseCulture(userDB?.Culture),
                        StickerId = Constants.StickersId.ChangelogSticker,
                        ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
                    };

                    await _appServices.BotControlService.SendAnswerMessageAsync(toSend, userDB.UserId, false);

                    await Task.Delay(100);

                    usersSuccess++;
                    Log.Information($"Sent changelog to {Extensions.GetLogUser(userDB)}");
                }
                catch (ApiRequestException ex)
                {
                    if (ex.ErrorCode == 403) //Forbidden by user
                    {
                        Log.Warning($"{ex?.Message} {Extensions.GetLogUser(userDB)}");

                        _appServices.ChatService.Remove(userDB.UserId);
                        _appServices.PetService.Remove(userDB.UserId);
                        _appServices.UserService.Remove(userDB.UserId);
                        _appServices.MetaUserService.Remove(userDB.UserId);
                        _appServices.AppleGameDataService.Delete(userDB.UserId);
                        _appServices.TicTacToeGameDataService.Delete(userDB.UserId);
                        _appServices.HangmanGameDataService.Delete(userDB.UserId);

                        usersForbidden++;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message);
                }
            }

            Log.Warning($"DELETED USERS:   {usersDeleted}");
            Log.Warning($"SUCESS SENT:     {usersSuccess}");
            Log.Warning($"FORBIDDEN USERS: {usersForbidden}");
            Log.Warning($"BD CLEANING IS OVER...");
            Log.Information($"Changelogs have been sent - {usersSuccess} success, {usersToNotify.Count - usersSuccess} failed");
        }

        private List<Models.Mongo.User> GetAllActiveUsersIds() => _appServices.UserService.GetAll().ToList();
    }
}
