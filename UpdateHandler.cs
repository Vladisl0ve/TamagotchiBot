using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
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
                MessageUpdate u when u.Data is TextMessage message => Echo(message),
                _ => Task.CompletedTask
            };
            return task;

/*            Testing new feature
 *            Task Echo2(TextMessage message)
            {
                var test = _userService.Get().First().Username;
                Log.Information($"Sending to @{message.Chat.Username}: {test}");
                return bot.HandleAsync(new SendText(message.Chat.Id, test), token);
            }
*/

            Task Echo(TextMessage message)
            {
                Log.Information($"Sending to @{message.Chat.Username}: {message.Text}");
                return bot.HandleAsync(new SendText(message.Chat.Id, message.Text), token);
            }
        }
    }
}