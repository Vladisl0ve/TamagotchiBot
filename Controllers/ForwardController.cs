using Serilog;
using System;
using System.Threading.Tasks;
using TamagotchiBot.Services.Interfaces;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TamagotchiBot.Controllers
{
    internal class ForwardController : ControllerBase
    {
        private readonly IApplicationServices _appService;
        private readonly Update update = default;

        public ForwardController(IApplicationServices services, Update update)
        {
            _appService = services;
            this.update = update;
        }

        public ForwardController(IApplicationServices services, Message message)
        {
            _appService = services;
            update = new Update()
            {
                Message = message
            };
        }

        public void StartForwarding(bool noTextOnly = false)
        {
            try
            {
                ForwardUpdate(noTextOnly);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Forwarding error");
            }
        }
        private async void ForwardUpdate(bool noTextOnly = false)
        {
            try
            {
                _ = update.Type switch
                {
                    UpdateType.Message            => await ForwardMessage(update.Message, noTextOnly),
                    UpdateType.InlineQuery        => false,
                    UpdateType.ChosenInlineResult => false,
                    UpdateType.CallbackQuery      => false,
                    UpdateType.EditedMessage      => false,
                    UpdateType.ChannelPost        => false,
                    UpdateType.EditedChannelPost  => false,
                    UpdateType.ShippingQuery      => false,
                    UpdateType.PreCheckoutQuery   => false,
                    UpdateType.Poll               => false,
                    UpdateType.PollAnswer         => false,
                    UpdateType.MyChatMember       => false,
                    UpdateType.ChatMember         => false,
                    UpdateType.ChatJoinRequest    => false,
                    _                             => false,
                };

            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"[{nameof(ForwardUpdate)}] Forwarding error: ");
            }
        }

        private async Task<bool> ForwardMessage(Message message, bool noTextOnly = false)
        {
            if (message.Chat.Id <= 0)
                return false;

            if (noTextOnly && message.Text != null)
                return false;

            var msgThread = _appService.MetaUserService.GetDebugMessageThreadId(update.Message.From.Id);
            if (msgThread == 0)
            {
                int newMsgThreadId = await _appService.BotControlService.CreateNewThreadInDebugChat(update.Message.From);
                if (newMsgThreadId == 0)
                    throw new ArgumentException("newMsgThreadId is 0!");

                _appService.MetaUserService.UpdateDebugMessageThreadId(update.Message.From.Id, newMsgThreadId);
                msgThread = newMsgThreadId;
            }

            var result = await _appService.BotControlService.ForwardMessageToDebugChat(message, msgThread);
            return result != default;
        }
    }
}
