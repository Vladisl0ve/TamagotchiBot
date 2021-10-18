using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TamagotchiBot.Services;
using Telegram.Bot.Types;

namespace TamagotchiBot.Controllers
{
    public class UserController
    {
        private readonly UserService _userService;
        private readonly Message message;
        public UserController(UserService userService, Message message)
        {
            _userService = userService;
            this.message = message;
        }

        public Models.User Update()
        {
            var user = _userService.Get(message.From.Id);
            if (user == null)
                return _userService.Create(new Models.User()
                {
                    UserId = message.From.Id,
                    Username = message.From.Username,
                    FirstName = message.From.FirstName,
                    LastName = message.From.LastName,
                    Culture = message.From.LanguageCode
                });
            else if (user.Username != message.From.Username || user.LastName != message.From.LastName || user.FirstName != message.From.FirstName || user.Culture != message.From.LanguageCode)
                return _userService.Update(user.UserId, new Models.User()
                {
                    Id = user.Id,
                    UserId = message.From.Id,
                    Username = message.From.Username,
                    FirstName = message.From.FirstName,
                    LastName = message.From.LastName,
                    Culture = message.From.LanguageCode
                });
            else
                return user;

        }
    }
}
