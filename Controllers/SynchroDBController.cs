using TamagotchiBot.Services.Interfaces;
using Telegram.Bot.Types;
using Serilog;

namespace TamagotchiBot.Controllers
{
    public class SynchroDBController
    {
        private IApplicationServices _appServices;
        private readonly Message _message = null;
        private readonly CallbackQuery _callback = null;
        private readonly long _userId;
        private readonly Telegram.Bot.Types.User _user;

        public SynchroDBController(IApplicationServices services, Message message = null, CallbackQuery callback = null)
        {
            _callback = callback;
            _message = message;
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
