using System.Globalization;
using TamagotchiBot.Services.Interfaces;
using Telegram.Bot.Types;
using static TamagotchiBot.UserExtensions.Constants;
using Extensions = TamagotchiBot.UserExtensions.Extensions;

namespace TamagotchiBot.Controllers
{
    public class SetCommandController
    {

        private IApplicationServices _appServices;
        private readonly Message _message = null;
        private readonly CallbackQuery _callback = null;
        private readonly long _userId;
        private readonly long _chatId;

        public SetCommandController(IApplicationServices services, Message message = null, CallbackQuery callback = null)
        {
            _callback = callback;
            _message = message;
            _userId = callback?.From.Id ?? message.From.Id;
            _chatId = callback?.Message?.Chat.Id ?? message.Chat.Id;

            _appServices = services;

            Resources.Resources.Culture = new CultureInfo(_appServices.UserService.Get(_userId)?.Culture ?? "ru");
        }
        public void UpdateCommands(MessageAudience messageAudience)
        {
            var userDB = _appServices.UserService.Get(_userId);
            var petDB = _appServices.PetService.Get(_userId);


            Resources.Resources.Culture = new CultureInfo(userDB?.Culture ?? "ru");

            switch (messageAudience)
            {
                case MessageAudience.Private:
                    {
                        UpdateCommandsForPrivate(userDB, petDB);
                        break;
                    }
                case MessageAudience.Group:
                    {
                        UpdateCommandsForGroup();
                        break;
                    }
            }

            void UpdateCommandsForPrivate(Models.Mongo.User userDB, Models.Mongo.Pet petDB)
            {
                if (petDB is not null && !userDB.IsInAppleGame)
                {
                    _appServices.BotControlService.SetMyCommandsAsync(Extensions.GetCommands(true),
                                                                      scope: new BotCommandScopeChat() { ChatId = _userId });
                }
                else if (userDB?.IsInAppleGame ?? false)
                {
                    _appServices.BotControlService.SetMyCommandsAsync(Extensions.GetInApplegameCommands(),
                                                                      scope: new BotCommandScopeChat() { ChatId = _userId });
                }
            }
            void UpdateCommandsForGroup()
            {
                _appServices.BotControlService.SetMyCommandsAsync(Extensions.GetMultiplayerCommands(),
                                                                  scope: new BotCommandScopeChatMember() {ChatId = _chatId, UserId = _userId });
            }
        }
    }
}
