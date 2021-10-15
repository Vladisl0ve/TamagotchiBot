using System.Threading;
using System.Threading.Tasks;
using Serilog;
using Telegram.Bots.Extensions.Polling;
using Telegram.Bots.Requests;
using Telegram.Bots.Types;

namespace Telegram.Bots.Example
{
    public class UpdateHandler : IUpdateHandler
    {
        public Task HandleAsync(IBotClient bot, Update update, CancellationToken token)
        {
            Task task = update switch
            {
                MessageUpdate u when u.Data is TextMessage message => Echo(message),
                _ => Task.CompletedTask
            };
            return task;

            Task Echo(TextMessage message)
            {
                Log.Information($"Sending to @{message.Chat.Username}: {message.Text}");
                return bot.HandleAsync(new SendText(message.Chat.Id, message.Text), token);
            }
        }
    }
}