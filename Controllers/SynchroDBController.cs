using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TamagotchiBot.Services.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot;
using TamagotchiBot.Models.Mongo;
using Serilog;

namespace TamagotchiBot.Controllers
{
    public class SynchroDBController
    {
        private IApplicationServices _appServices;
        private readonly ITelegramBotClient bot;
        private readonly Message message = null;
        private readonly CallbackQuery callback = null;
        private readonly long _userId;
        private readonly User _user;

        public SynchroDBController(IApplicationServices services, ITelegramBotClient bot, Message message = null, CallbackQuery callback = null)
        {
            this.bot = bot;
            this.callback = callback;
            this.message = message;
            _userId = callback?.From.Id ?? message.From.Id;
            _user = callback?.From ?? message.From;

            _appServices = services;
        }

        public bool SynchronizeWithDB()
        {
            var userDB = _appServices.UserService.Get(_userId);

            if (userDB == null)
                return false;

            if (userDB.Username != _user.Username || userDB.FirstName != _user.FirstName || userDB.LastName != _user.LastName)
            {
                var userDataToUpdate = new Models.Mongo.User().Clone(userDB);

                userDataToUpdate.Username = _user.Username;
                userDataToUpdate.FirstName = _user.FirstName;
                userDataToUpdate.LastName = _user.LastName;

                _appServices.UserService.Update(_userId, userDataToUpdate);
                Log.Information($"Synchronized user with ID:{_userId}");
            }
            return true;
        }
    }
}
