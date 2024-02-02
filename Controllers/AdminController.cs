using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TamagotchiBot.Services.Interfaces;
using Telegram.Bot.Types;
using TamagotchiBot.UserExtensions;
using Serilog;
using TamagotchiBot.Models.Mongo;
using static TamagotchiBot.UserExtensions.Constants;
using static TamagotchiBot.Resources.Resources;
using TamagotchiBot.Models.Answers;
using User = TamagotchiBot.Models.Mongo.User;

namespace TamagotchiBot.Controllers
{
    public class AdminController : ControllerBase
    {
        private readonly IApplicationServices _appServices;
        private readonly Message _message = null;
        private readonly CallbackQuery _callback = null;
        private readonly long _userId;
        private readonly string _userInfo;

        private AdminController(IApplicationServices services, Message message = null, CallbackQuery callback = null)
        {
            _callback = callback;
            _message = message;
            _userId = callback?.From.Id ?? message.From.Id;
            _appServices = services;
            _userInfo = Extensions.GetLogUser(_appServices.UserService.Get(_userId));
        }
        public AdminController(IApplicationServices services, CallbackQuery callback) : this(services, null, callback)
        {
        }

        public AdminController(IApplicationServices services, Message message) : this(services, message, null)
        {
        }

        /// <summary>
        /// Returns true in case confirmed AdminMessage
        /// </summary>
        /// <returns></returns>
        internal async Task<bool> ProcessMessage()
        {
            if (_message == null || _message.Text == null)
            {
                Log.Error("AdminController: Message to process is null");
                return false;
            }

            string msg = _message.Text;
            if (msg.First() == '/')
                msg = msg.Substring(1);

            try
            {
                return await ExecuteCommand(msg);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ADMIN CONTROLLER");
                return true;
            }
        }

        private async Task<bool> ExecuteCommand(string command)
        {
            if (command == Commands.CheckCommand)
            {
                await SendCheckData();
                return true;
            }
            if (command == Commands.RestartCommand)
            {
                await RestartPet();
                return true;
            }
            if (command == Commands.KillCommand)
            {
                await TestKillPet();
                return true;
            }
            if (command.Length >= 4 && command.Substring(0, 4) == Commands.GoldCommand)
            {
                await AddGold(command);
                return true;
            }

            return false;
        }

        private async Task<bool> AddGold(string command)
        {
            bool result = false;
            AnswerMessage toSend;
            User userDB = _appServices.UserService.Get(_userId);

            string amountGoldStr = command.Substring(4).Trim();
            if (!int.TryParse(amountGoldStr, out int amountGold))
            {
                toSend = new AnswerMessage()
                {
                    Text = $"ERROR: Wrong amount of gold: '{amountGoldStr}'"
                };
            }
            else if (userDB == null)
            {
                toSend = new AnswerMessage()
                {
                    Text = $"ERROR: no user in DB with id {_userId}"
                };
            }
            else
            {
                toSend = new AnswerMessage()
                {
                    Text = $"Added {amountGold} gold",
                    StickerId = StickersId.ChangelogSticker
                };

                _appServices.UserService.UpdateGold(_userId, userDB.Gold + amountGold);
                result = true;
            }

            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId);
            return result;
        }

        private async Task<bool> SendCheckData()
        {
            int playedUsersToday = _appServices.AllUsersDataService.GetAll().Count(p => p.Updated.Date == DateTime.UtcNow.Date);

            var dailyInfoDB = _appServices.DailyInfoService.GetToday() ?? _appServices.DailyInfoService.CreateDefault();
            long messagesSent = _appServices.AllUsersDataService.GetAll().Select(u => u.MessageCounter).Sum();
            long messagesSentToday = messagesSent - (_appServices.DailyInfoService.GetAll().Where(u => u.DateInfo.Date <= DateTime.UtcNow.AddDays(-1).Date).OrderByDescending(i => i.DateInfo).FirstOrDefault()?.MessagesSent ?? 0);
            long callbacksSent = _appServices.AllUsersDataService.GetAll().Select(u => u.CallbacksCounter).Sum();
            long callbacksSentToday = callbacksSent - (_appServices.DailyInfoService.GetAll().Where(u => u.DateInfo.Date <= DateTime.UtcNow.AddDays(-1).Date).OrderByDescending(i => i.DateInfo).FirstOrDefault()?.CallbacksSent ?? 0);
            long registeredPets = _appServices.PetService.Count();
            long registeredUsers = _appServices.UserService.Count();
            var allAUDUsers = _appServices.AllUsersDataService.CountAllAUD();
            var allRefUsers = _appServices.ReferalInfoService.CountAllRefUsers();

            string deltaUsers = "-", deltaPets = "-", deltaAUD = "-", deltaRef = "-";
            var prevDay = _appServices.DailyInfoService.GetPreviousDay();
            if (prevDay != default)
            {
                deltaPets = (registeredPets - prevDay.PetCounter).ToString();
                deltaUsers = (registeredUsers - prevDay.UserCounter).ToString();
                deltaAUD = (allAUDUsers - prevDay.AUDCounter).ToString();
                deltaRef = (allRefUsers - prevDay.ReferalsCounter).ToString();
            }

            dailyInfoDB.UsersPlayed = playedUsersToday;
            dailyInfoDB.MessagesSent = messagesSent;
            dailyInfoDB.CallbacksSent = callbacksSent;
            dailyInfoDB.DateInfo = DateTime.UtcNow;
            dailyInfoDB.TodayCallbacks = (int)callbacksSentToday;
            dailyInfoDB.TodayMessages = (int)messagesSentToday;
            dailyInfoDB.UserCounter = registeredUsers;
            dailyInfoDB.PetCounter = registeredPets;
            dailyInfoDB.AUDCounter = allAUDUsers;
            dailyInfoDB.ReferalsCounter = allRefUsers;

            _appServices.DailyInfoService.UpdateOrCreate(dailyInfoDB);
            string text = $"{dailyInfoDB.DateInfo:G}" + Environment.NewLine
                        + $"Played   users  : {playedUsersToday}" + Environment.NewLine
                        + $"Messages today: {messagesSentToday}" + Environment.NewLine
                        + $"Callbacks today: {callbacksSentToday}" + Environment.NewLine
                        + $"------------------------" + Environment.NewLine
                        + $"ΔUsers: {deltaUsers}" + Environment.NewLine
                        + $"ΔPets: {deltaPets}" + Environment.NewLine
                        + Environment.NewLine
                        + $"ΔAUD: {deltaAUD}" + Environment.NewLine
                        + $"ΔReferals: {deltaRef}" + Environment.NewLine
                        + $"------------------------" + Environment.NewLine
                        + $"Messages sent: {messagesSent}" + Environment.NewLine
                        + $"Callbacks sent  : {callbacksSent}";

            var toSend = new AnswerMessage()
            {
                Text = text
            };

            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId);
            return true;
        }

        private async Task<bool> RestartPet()
        {
            bool result = false;
            AnswerMessage toSend;
            Pet petDB = _appServices.PetService.Get(_userId);
            if (petDB == null)
            {
                toSend = new AnswerMessage()
                {
                    Text = "Netu pitomca",
                    StickerId = StickersId.MaintanceProblems
                };
            }
            else
            {
                string toSendText = string.Format(restartCommand, petDB.Name);

                _appServices.PetService.Remove(_userId);
                _appServices.UserService.Remove(_userId);
                _appServices.MetaUserService.Remove(_userId);
                _appServices.ChatService.Remove(_userId);

                toSend = new AnswerMessage()
                {
                    Text = toSendText,
                    StickerId = StickersId.DroppedPetSticker
                };
                result = true;
            }

            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId);
            return result;
        }

        private async Task<bool> TestKillPet()
        {
            bool result = false;
            AnswerMessage toSend;

            Pet petDB = _appServices.PetService.Get(_userId);
            if (petDB == null)
            {
                toSend = new AnswerMessage()
                {
                    Text = "Netu pitomca",
                    StickerId = StickersId.MaintanceProblems
                };
            }
            else
            {
                string toSendText = string.Format("HP is zero (0) for {0}", petDB.Name);
                _appServices.PetService.UpdateHP(_userId, 0);
                toSend = new AnswerMessage()
                {
                    Text = toSendText,
                    StickerId = StickersId.HelpCommandSticker
                };
                result = true;
            }
            await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId);
            return result;
        }
    }
}
