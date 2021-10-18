using System.Threading;
using System.Threading.Tasks;
using Serilog;
using TamagotchiBot.Controllers;
using TamagotchiBot.Resources;
using TamagotchiBot.Services;
using Telegram.Bots.Extensions.Polling;
using Telegram.Bots.Requests;
using Telegram.Bots.Types;

namespace Telegram.Bots.Example
{
    public class UpdateHandler : IUpdateHandler
    {
        UserService _userService;
        public UpdateHandler(UserService userService)
        {
            _userService = userService;
        }

        public Task HandleAsync(IBotClient bot, Update update, CancellationToken token)
        {
            Task task = update switch
            {
                MessageUpdate u when u.Data is TextMessage message => Echo2(message),
                _ => Task.CompletedTask
            };
            return task;

            // Testing new feature
            Task Echo2(TextMessage message)
            {
                var user = new UserController(_userService, message).Update();

                switch (user.Culture)
                {
                    case "be":
                        Resources.Culture = new System.Globalization.CultureInfo("be");
                        break;
                    case "ru":
                        Resources.Culture = new System.Globalization.CultureInfo("ru");
                        break;
                    case "pl":
                        Resources.Culture = new System.Globalization.CultureInfo("pl");
                        break;
                    default:
                        Resources.Culture = new System.Globalization.CultureInfo("en");
                        break;
                }

                Log.Information($"Sending to @{message.Chat.Username}: {Resources.Test}");
                return bot.HandleAsync(new SendText(message.Chat.Id, Resources.Test), token);
            }


            Task Echo(TextMessage message)
            {
                Log.Information($"Sending to @{message.Chat.Username}: {message.Text}");
                return bot.HandleAsync(new Send, token);
            }
        }
    }
}