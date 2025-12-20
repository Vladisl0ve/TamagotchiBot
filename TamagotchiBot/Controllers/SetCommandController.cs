using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        public async void UpdateCommands(MessageAudience messageAudience, string culture)
        {
            switch (messageAudience)
            {
                case MessageAudience.Private:
                    {
                        await UpdateCommandsForPrivate();
                        break;
                    }
                case MessageAudience.Group:
                    {
                        await UpdateCommandsForGroup();
                        break;
                    }
            }

            async Task UpdateCommandsForPrivate()
            {
                var userDB = _appServices.UserService.Get(_userId);
                var petDB = _appServices.PetService.Get(_userId);

                if (userDB is not null &&
                    !userDB.IsInAppleGame &&
                    !userDB.IsInTicTacToeGame &&
                    !userDB.IsInHangmanGame &&
                    Extensions.ParseString(_envs.AlwaysNotifyUsers).Exists(u => u == userDB.UserId))
                {
                    await _appServices.BotControlService.SetMyCommandsAsync(Extensions.GetCommandsAdmin(culture, true),
                                                  scope: new BotCommandScopeChat() { ChatId = _userId });
                }
                else if (petDB is not null &&
                    !userDB.IsInAppleGame &&
                    !userDB.IsInHangmanGame &&
                    !userDB.IsInTicTacToeGame)
                {
                    await _appServices.BotControlService.SetMyCommandsAsync(Extensions.GetCommands(culture, true),
                                                                      scope: new BotCommandScopeChat() { ChatId = _userId });
                }
                else if (userDB?.IsInAppleGame ?? false)
                {
                    await _appServices.BotControlService.SetMyCommandsAsync(Extensions.GetInApplegameCommands(culture),
                                                                      scope: new BotCommandScopeChat() { ChatId = _userId });
                }
                else if (userDB?.IsInTicTacToeGame ?? false)
                {
                    await _appServices.BotControlService.SetMyCommandsAsync(Extensions.GetInTicTacToeGameCommands(culture),
                                                                      scope: new BotCommandScopeChat() { ChatId = _userId });
                }
                else if (userDB?.IsInHangmanGame ?? false)
                {
                    await _appServices.BotControlService.SetMyCommandsAsync(Extensions.GetInHangmanGameCommands(culture),
                                                                      scope: new BotCommandScopeChat() { ChatId = _userId });
                }
            }
            async Task UpdateCommandsForGroup()
            {
                await _appServices.BotControlService.SetMyCommandsAsync(Extensions.GetMultiplayerCommands(culture),
                                                  scope: new BotCommandScopeChatMember() { ChatId = _chatId, UserId = _userId });
            }
        }
        public async Task UpdateCommandsForThisChat(string culture)
        {
            await _appServices.BotControlService.SetMyCommandsAsync(Extensions.GetMultiplayerCommands(culture),
                                  scope: new BotCommandScopeChat() { ChatId = _chatId });
        }
    }
}
