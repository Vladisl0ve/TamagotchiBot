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
                MessageUpdate u when u.Data is TextMessage message => Echo2(message),
                _ => Task.CompletedTask
            };
            return task;

            // Testing new feature
            Task Echo2(TextMessage message)
            {
                var user = _userService.Get(message.From.Id);
                if (user == null)
                    _userService.Create(new TamagotchiBot.Models.User()
                    {
                        UserId = message.From.Id,
                        Username = message.From.Username,
                        FirstName = message.From.FirstName,
                        LastName = message.From.LastName
                    });
                else if (user.Username != message.From.Username || user.LastName != message.From.LastName || user.FirstName != message.From.FirstName)
                    _userService.Update(user.UserId, new TamagotchiBot.Models.User()
                    {
                        Id = user.Id,
                        UserId = message.From.Id,
                        Username = message.From.Username,
                        FirstName = message.From.FirstName,
                        LastName = message.From.LastName
                    });

                Log.Information($"Sending to @{message.Chat.Username}: {message.Text}");
                return bot.HandleAsync(new SendText(message.Chat.Id, message.Text), token);
            }


            Task Echo(TextMessage message)
            {
                Log.Information($"Sending to @{message.Chat.Username}: {message.Text}");
                return bot.HandleAsync(new SendText(message.Chat.Id, message.Text), token);
            }
        }
    }
}