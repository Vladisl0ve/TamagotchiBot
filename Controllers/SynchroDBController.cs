using TamagotchiBot.Services.Interfaces;
using Serilog;

namespace TamagotchiBot.Controllers
{
    public class SynchroDBController
    {
        private readonly IApplicationServices _appServices;

        private readonly long _userId;
        private readonly long _chatId;
        private readonly string _chatName;
        private readonly Telegram.Bot.Types.User _user;

        public SynchroDBController(IApplicationServices services, Telegram.Bot.Types.User userFrom, long chatId, string chatTitle)
        {
            _user = userFrom;
            _userId = _user.Id;
            _chatId = chatId;
            _chatName = chatTitle ?? "";

            _appServices = services;
        }

        public bool SynchronizeWithDB()
        {
            var userDB = _appServices.UserService.Get(_userId);

            if (userDB == null)
                return false;

            if (userDB.Username != _user.Username || userDB.FirstName != _user.FirstName || userDB.LastName != _user.LastName)
            {
                var userDataToUpdate = Models.Mongo.User.Clone(userDB);

                userDataToUpdate.Username = _user.Username;
                userDataToUpdate.FirstName = _user.FirstName;
                userDataToUpdate.LastName = _user.LastName;

                _appServices.UserService.Update(_userId, userDataToUpdate);
                Log.Information($"Synchronized user with ID:{_userId}");
            }
            return true;
        }

        public void SynchronizeMPWithDB()
        {
            var chatMP = _appServices.ChatsMPService.Get(_chatId);
            chatMP ??= _appServices.ChatsMPService.Create(new Models.Mongo.ChatsMP()
                {
                    ChatId = _chatId,
                    Name = _chatName
                });

            if (chatMP?.Name != _chatName)
            {
                _appServices.ChatsMPService.Update(_chatId, new Models.Mongo.ChatsMP()
                {
                    Id = chatMP.Id,
                    ChatId = _chatId,
                    Name = _chatName
                });
            }
        }
    }
}
