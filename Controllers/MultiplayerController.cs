using System.Globalization;
using Extensions = TamagotchiBot.UserExtensions.Extensions;
using TamagotchiBot.Services.Interfaces;
using Telegram.Bot.Types;
using static TamagotchiBot.Resources.Resources;
using TamagotchiBot.Models.Answers;
using Telegram.Bot.Types.ReplyMarkups;
using static TamagotchiBot.UserExtensions.CallbackButtons;
using Serilog;
using System.Threading.Tasks;
using System.Web;
using System.Linq;

namespace TamagotchiBot.Controllers
{
    public class MultiplayerController
    {
        private IApplicationServices _appServices;
        private readonly Message _message = null;
        private readonly CallbackQuery _callback = null;
        private readonly long _userId;
        private readonly long _chatId;
        private readonly CultureInfo _userCulture;

        private readonly string _chatName;
        private readonly string _userName;
        private readonly string _userLogInfo;

        public MultiplayerController(IApplicationServices services, Message message = null, CallbackQuery callback = null)
        {
            _callback = callback;
            _message = message;
            _userId = callback?.From.Id ?? message.From.Id;
            _chatId = callback?.Message?.Chat?.Id ?? message.Chat.Id;
            _appServices = services;

            _userName = message?.From?.FirstName ?? callback?.From?.FirstName;
            _chatName = message?.Chat?.Title ?? callback?.Message.Chat.Title;
            _userLogInfo = Extensions.GetLogUser(_appServices.UserService.Get(_userId) ?? new Models.Mongo.User() { UserId = _userId });

            Culture = _userCulture = new CultureInfo(_appServices.UserService.Get(_userId)?.Culture ?? "ru");
        }
        public Task CommandHandler(string customText = null)
        {
            string textReceived = customText ?? _message.Text;
            if (textReceived == null)
                return Task.CompletedTask;

            textReceived = textReceived.ToLower();
            if (textReceived.Contains('@'))
                textReceived = textReceived.Split('@').First();

            var petDB = _appServices.PetService.Get(_userId);
            var userDB = _appServices.UserService.Get(_userId);

            if (textReceived == "/show_pet")
            {
                ShowPetMP(petDB, userDB);
            }

            return Task.CompletedTask;

            async void ShowPetMP(Models.Mongo.Pet petDB, Models.Mongo.User userDB)
            {
                var botUsername = (await _appServices.SInfoService.GetBotUserInfo()).Username;
                var encodedPetName = HttpUtility.HtmlEncode(petDB.Name);
                string toSendText = string.Format(MultiplayerShowPet,
                                                  encodedPetName,
                                                  petDB.HP,
                                                  petDB.Satiety,
                                                  petDB.Hygiene,
                                                  petDB.Fatigue,
                                                  petDB.Joy,
                                                  petDB.Level,
                                                  userDB.Gold,
                                                  "`personalLink`");

                toSendText = toSendText.Replace("`personalLink`", Extensions.GetPersonalLink(_userId, _userName));

                AnswerMessage answerMessage = new AnswerMessage()
                {
                    InlineKeyboardMarkup = new (
                        InlineKeyboardButton.WithUrl(
                            new InviteMuliplayerCommand().InviteReferalMultiplayerButton(userDB.FirstName).Text,
                            Extensions.GetReferalLink(_userId, botUsername)
                            )),
                    ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                    Text = toSendText
                };

                _appServices.BotControlService.SendAnswerMessageGroupAsync(answerMessage, _chatId, false);
                Log.Debug($"MP: called showPet by {_userLogInfo}");
            }
        }

        public async void SendInviteForUnregistered()
        {
            string toSendText = string.Format(InviteGlobalMultiplayerText, "`personalLink`");
            toSendText = HttpUtility.HtmlEncode(toSendText);
            toSendText = toSendText.Replace("`personalLink`", Extensions.GetPersonalLink(_userId, _userName));
            var botUsername = (await _appServices.SInfoService.GetBotUserInfo()).Username;

            AnswerMessage answerMessage = new AnswerMessage()
            {
                InlineKeyboardMarkup = new ( InlineKeyboardButton.WithUrl(new InviteMuliplayerCommand().InviteGlobalMultiplayerButton.Text, $"https://t.me/{botUsername}")),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                Text = toSendText
            };

            _appServices.BotControlService.SendAnswerMessageGroupAsync(answerMessage, _chatId, false);
            Log.Debug($"MP: called SendInviteForUnregistered by unregistered ID: {_userId}");
        }
        public async void SendWelcomeMessageOnStart()
        {
            var encodedChatName = HttpUtility.HtmlEncode(_chatName);
            string toSendText = string.Format(ShowWelcomeMessageMultiplayer, encodedChatName);
            var botUsername = (await _appServices.SInfoService.GetBotUserInfo()).Username;

            AnswerMessage answerMessage = new AnswerMessage()
            {
                InlineKeyboardMarkup = new ( InlineKeyboardButton.WithUrl(new InviteMuliplayerCommand().InviteGlobalMultiplayerButton.Text, Extensions.GetReferalLink(_userId, botUsername))),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                Text = toSendText
            };

            _appServices.BotControlService.SendAnswerMessageGroupAsync(answerMessage, _chatId, false);
            Log.Debug($"MP: called SendWelcomeMessageOnStart, invited by ID: {_userId}");
        }
    }
}
