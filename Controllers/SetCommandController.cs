using System.Globalization;
using TamagotchiBot.Services.Interfaces;
using Telegram.Bot.Types;
using Extensions = TamagotchiBot.UserExtensions.Extensions;

namespace TamagotchiBot.Controllers
{
    public class SetCommandController
    {

        private IApplicationServices _appServices;
        private readonly Message _message = null;
        private readonly CallbackQuery _callback = null;
        private readonly long _userId;

        public SetCommandController(IApplicationServices services, Message message = null, CallbackQuery callback = null)
        {
            _callback = callback;
            _message = message;
            _userId = callback?.From.Id ?? message.From.Id;

            _appServices = services;

            Resources.Resources.Culture = new CultureInfo(_appServices.UserService.Get(_userId)?.Culture ?? "ru");
        }
        public void UpdateCommands()
        {
            var userDB = _appServices.UserService.Get(_userId);
            var petDB = _appServices.PetService.Get(_userId);


            Resources.Resources.Culture = new CultureInfo(userDB?.Culture ?? "ru");
            if (petDB is not null && !userDB.IsInAppleGame)
            {
                _appServices.BotControlService.SetMyCommandsAsync(_userId,
                                                                  Extensions.GetCommands(true),
                                                                  scope: new BotCommandScopeChat() { ChatId = _userId });
            }
            else if (userDB?.IsInAppleGame ?? false)
            {
                _appServices.BotControlService.SetMyCommandsAsync(_userId,
                                                                  Extensions.GetInApplegameCommands(),
                                                                  scope: new BotCommandScopeChat() { ChatId = _userId });
            }

        }
    }
}
