using TamagotchiBot.Database;
using TamagotchiBot.Services.Interfaces;
using Telegram.Bot.Types;
using static TamagotchiBot.UserExtensions.Constants;
using Extensions = TamagotchiBot.UserExtensions.Extensions;

namespace TamagotchiBot.Controllers
{
    public class SetCommandController
    {
        private readonly IApplicationServices _appServices;
        private readonly IEnvsSettings _envs;
        private readonly long _userId;
        private readonly long _chatId;

        public SetCommandController(IApplicationServices services, IEnvsSettings envs, long userId, long chatId)
        {
            _userId = userId;
            _chatId = chatId;

            _envs = envs;
            _appServices = services;
        }
        public void UpdateCommands(MessageAudience messageAudience)
        {
            switch (messageAudience)
            {
                case MessageAudience.Private:
                    {
                        UpdateCommandsForPrivate();
                        break;
                    }
                case MessageAudience.Group:
                    {
                        UpdateCommandsForGroup();
                        break;
                    }
            }

            void UpdateCommandsForPrivate()
            {
                var userDB = _appServices.UserService.Get(_userId);
                var petDB = _appServices.PetService.Get(_userId);

                if (userDB is not null && Extensions.ParseString(_envs.AlwaysNotifyUsers).Exists(u => u == userDB.UserId))
                {
                    _appServices.BotControlService.SetMyCommandsAsync(Extensions.GetCommandsAdmin(true),
                                                  scope: new BotCommandScopeChat() { ChatId = _userId });
                }
                else if (petDB is not null && !userDB.IsInAppleGame)
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
                                                  scope: new BotCommandScopeChatMember() { ChatId = _chatId, UserId = _userId });
            }
        }
        public void UpdateCommandsForThisChat()
        {
            _appServices.BotControlService.SetMyCommandsAsync(Extensions.GetMultiplayerCommands(),
                                  scope: new BotCommandScopeChat() { ChatId = _chatId });
        }
    }
}
