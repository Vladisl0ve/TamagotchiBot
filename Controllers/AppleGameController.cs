using System;
using System.Collections.Generic;
using System.Globalization;
using TamagotchiBot.Models.Answers;
using TamagotchiBot.UserExtensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static TamagotchiBot.Resources.Resources;
using static TamagotchiBot.UserExtensions.Constants;
using TamagotchiBot.Services.Interfaces;
using Extensions = TamagotchiBot.UserExtensions.Extensions;
using Serilog;
using System.Threading.Tasks;
using TamagotchiBot.Models.Mongo.Games;
using System.Text;

namespace TamagotchiBot.Controllers
{
    public class AppleGameController : ControllerBase
    {
        private readonly IApplicationServices _appServices;

        private readonly Message _message;
        private readonly CallbackQuery _callback;
        private readonly CultureInfo CultureUser;

        private readonly long _userId = 0;

        private const int APPLE_COUNTER_DEFAULT = 24;

        private readonly string _userInfo;

        private AppleGameController(IApplicationServices services, Message message = null, CallbackQuery callback = null)
        {
            _callback = callback;
            _message = message;
            _userId = message?.From?.Id ?? callback.From.Id;
            _appServices = services;

            _userInfo = Extensions.GetLogUser(_appServices.UserService.Get(_userId));

            CultureUser = new CultureInfo(_appServices.UserService.Get(_userId)?.Culture ?? "ru");
            AppleCounter = _appServices.AppleGameDataService.Get(_userId)?.CurrentAppleCounter ?? 1;
        }

        public AppleGameController(IApplicationServices services,
                                   CallbackQuery callback) : this(services, null, callback)
        {
        }

        public AppleGameController(IApplicationServices services,
                                   Message message) : this(services, message, null)
        {
        }

        public int AppleCounter { get; set; }
        private List<string> MenuCommands => new()
        {
            nameof(againText).UseCulture(CultureUser),
            nameof(statisticsText).UseCulture(CultureUser),
            nameof(quitText).UseCulture(CultureUser)
        };
        private List<string> ApplesToChoose
        {
            get
            {
                return AppleCounter switch
                {
                    1 => MenuCommands,
                    2 => new List<string>() { "🍎", nameof(ConcedeText).UseCulture(CultureUser) },
                    3 => new List<string>() { "🍎", "🍎🍎", nameof(ConcedeText).UseCulture(CultureUser) },
                    _ => new List<string>() { "🍎", "🍎🍎", "🍎🍎🍎", nameof(ConcedeText).UseCulture(CultureUser) },
                };
            }
        }

        private string ApplesIcons
        {
            get
            {
                string greenApple = "🍏";
                string redApple = "🍎";

                StringBuilder result = new(greenApple);
                for (int i = 1; i < AppleCounter; i++)
                {
                    result.Append($"{redApple}");
                }
                return result.ToString();
            }
        }

        public bool IsOver
        {
            get => AppleCounter <= 1;
        }

        private ReplyKeyboardMarkup KeyboardOptimizer(List<string> names)
        {
            int x = 2;
            int y = (int)Math.Ceiling((double)names.Count / x);
            int counter = 0;

            KeyboardButton[][] keyboard = new KeyboardButton[y][];

            for (int i = 0; i < keyboard.Length; i++)
            {
                if (i + 1 == keyboard.Length)
                    keyboard[i] = new KeyboardButton[names.Count - counter];
                else
                    keyboard[i] = new KeyboardButton[x];

                for (int j = 0; j < keyboard[i].Length; j++)
                {
                    if (counter < names.Count)
                        keyboard[i][j] = new KeyboardButton("");
                    counter++;
                }
            }

            for (int i = 0; i < names.Count; i++)
                keyboard[i / x][i % x] = names[i];

            return new ReplyKeyboardMarkup(keyboard) { ResizeKeyboard = true };
        }

        public AnswerMessage StartGame()
        {
            var appleDataToUpdate = _appServices.AppleGameDataService.Get(_userId);
            appleDataToUpdate.CurrentAppleCounter =
            AppleCounter = APPLE_COUNTER_DEFAULT;
            _appServices.AppleGameDataService.Update(appleDataToUpdate);

            string text = $"{nameof(appleGameHelpText).UseCulture(CultureUser)}\n\n{string.Format(nameof(remainingApplesText).UseCulture(CultureUser), AppleCounter)}\n{ApplesIcons}";
            var keyboard = KeyboardOptimizer(ApplesToChoose);
            return new AnswerMessage()
            {
                Text = text,
                ReplyMarkup = keyboard
            };
        }

        public async Task Menu()
        {
            var appleDataToUpdate = _appServices.AppleGameDataService.Get(_userId);

            if (appleDataToUpdate == null)
            {
                await _appServices.UserService.UpdateAppleGameStatus(_userId, false);
                await new MenuController(_appServices, _message).ProcessMessage("/pet");
                return;
            }

            var msgText = _message.Text.ToLower();
            if (msgText.StartsWith('/'))
                msgText = msgText.Substring(1);

            if (GetAllTranslatedAndLowered(nameof(statisticsText)).Contains(msgText) && appleDataToUpdate.IsGameOvered)
            {
                var toSendText = string.Format(nameof(appleGameStatisticsCommand).UseCulture(CultureUser),
                                               appleDataToUpdate.TotalWins,
                                               appleDataToUpdate.TotalLoses,
                                               appleDataToUpdate.TotalDraws);

                var toSend = new AnswerMessage()
                {
                    Text = toSendText,
                    ReplyMarkup = KeyboardOptimizer(MenuCommands)
                };

                Log.Debug($"Ending AppleGame {_userInfo}");

                await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
                return;
            }

            if (GetAllTranslatedAndLowered(nameof(ConcedeText)).Contains(msgText))
            {
                appleDataToUpdate.TotalDraws += 1;
                appleDataToUpdate.IsGameOvered = true;
                _appServices.AppleGameDataService.Update(appleDataToUpdate);

                string toSendText = $"{nameof(appleGameHelpText).UseCulture(CultureUser)}";

                var toSend = new AnswerMessage()
                {
                    Text = toSendText,
                    ReplyMarkup = KeyboardOptimizer(MenuCommands)
                };

                Log.Debug($"Conceded AppleGame {_userInfo}");

                await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
                return;
            }

            if (GetAllTranslatedAndLowered(nameof(quitText)).Contains(msgText) || msgText == "quit")
            {
                appleDataToUpdate.TotalLoses += 1;
                appleDataToUpdate.IsGameOvered = true;
                _appServices.AppleGameDataService.Update(appleDataToUpdate);

                await _appServices.UserService.UpdateAppleGameStatus(_userId, false);

                Log.Debug($"Quit AppleGame {_userInfo}");

                await new MenuController(_appServices, _message).ProcessMessage("/gameroom");
                return;
            }

            if (GetAllTranslatedAndLowered(nameof(againText)).Contains(msgText))
            {
                Log.Debug($"Play again AppleGame {_userInfo}");

                await _appServices.BotControlService.SendAnswerMessageAsync(StartGame(), _userId, false);
                return;
            }

            if (msgText != "🍎" && msgText != "🍎🍎" && msgText != "🍎🍎🍎")
            {
                var toSend = new AnswerMessage() { Text = nameof(appleGameUndefiendText).UseCulture(CultureUser), ReplyMarkup = KeyboardOptimizer(ApplesToChoose) };
                Log.Debug($"Wrong message {msgText} in AppleGame {_userInfo}");

                await _appServices.BotControlService.SendAnswerMessageAsync(toSend, _userId, false);
                return;
            }

            Log.Debug($"Make move in AppleGame {_userInfo}");
            await _appServices.BotControlService.SendAnswerMessageAsync(MakeMove(_message), _userId, false);
        }

        public AnswerMessage MakeMove(Message message)
        {
            var appleDataToUpdate = _appServices.AppleGameDataService.Get(_userId);
            var petDB = _appServices.PetService.Get(_userId);
            var aud = _appServices.AllUsersDataService.Get(_userId);

            int toRemove = message.Text switch
            {
                "🍎"       => 1,
                "🍎🍎"    => 2,
                "🍎🍎🍎"  => 3,
                _          => 0
            };

            AppleCounter -= toRemove;
            int systemRemove = 0;
            string textToSay = "";

            switch (AppleCounter)
            {
                case 1:
                    break;
                case 2:
                case 5:
                case 6:
                    systemRemove = 1;
                    AppleCounter -= systemRemove;
                    break;
                case 3:
                    systemRemove = 2;
                    AppleCounter -= systemRemove;
                    break;
                case 4:
                case 8:
                    systemRemove = 3;
                    AppleCounter -= systemRemove;
                    break;
                case 7:
                    systemRemove = new Random().Next(1, 3);
                    AppleCounter -= systemRemove;
                    break;
                default:
                    systemRemove = new Random().Next(1, 4);
                    AppleCounter -= systemRemove;
                    break;
            }

            textToSay += $"{string.Format(nameof(appleGameSysEaten).UseCulture(CultureUser), systemRemove)}";

            textToSay += $"\n\n{ApplesIcons}\n";

            switch (AppleCounter)
            {
                case 1 when systemRemove != 0:
                    textToSay += nameof(appleGameLoseText).UseCulture(CultureUser);
                    appleDataToUpdate.TotalLoses += 1;
                    appleDataToUpdate.IsGameOvered = true;

                    petDB.Joy += Factors.CardGameJoyFactor;
                    if (petDB.Joy > 100)
                        petDB.Joy = 100;

                    petDB.Fatigue += Factors.CardGameFatigueFactor;
                    if (petDB.Fatigue > 100)
                        petDB.Fatigue = 100;

                    aud.AppleGamePlayedCounter++;
                    _appServices.AllUsersDataService.Update(aud);

                    break;
                case 1 when systemRemove == 0:
                    textToSay += nameof(appleGameWinText).UseCulture(CultureUser);
                    appleDataToUpdate.TotalWins += 1;
                    appleDataToUpdate.IsGameOvered = true;

                    petDB.Joy += Factors.CardGameJoyFactor;
                    if (petDB.Joy > 100)
                        petDB.Joy = 100;

                    petDB.Fatigue += Factors.CardGameFatigueFactor;
                    if (petDB.Fatigue > 100)
                        petDB.Fatigue = 100;

                    aud.AppleGamePlayedCounter++;
                    _appServices.AllUsersDataService.Update(aud);

                    break;
                default:
                    textToSay += string.Format(nameof(remainingApplesText).UseCulture(CultureUser), AppleCounter);
                    appleDataToUpdate.CurrentAppleCounter = AppleCounter;
                    break;
            }

            _appServices.AppleGameDataService.Update(appleDataToUpdate);
            _appServices.PetService.Update(_userId, petDB);
            return new AnswerMessage()
            {
                Text = textToSay,
                ReplyMarkup = KeyboardOptimizer(ApplesToChoose)
            };
        }

        public async Task PreStart()
        {
            var petDB = _appServices.PetService.Get(_userId);
            var userDB = _appServices.UserService.Get(_userId);

            if (petDB == null || userDB == null)
                return;

            if (petDB.Fatigue >= 100)
            {
                string anwser = string.Format(nameof(tooTiredText).UseCulture(CultureUser));
                _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id,
                                                                        _userId,
                                                                        anwser,
                                                                        true);
                return;
            }
            if (petDB.Joy >= 100)
            {
                string anwser = string.Format(nameof(PetIsFullOfJoyText).UseCulture(CultureUser));
                _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id,
                                                                        _userId,
                                                                        anwser,
                                                                        true);
                return;
            }

            if (userDB.Gold <= Costs.AppleGame)
            {
                string anwser = string.Format(nameof(goldNotEnough).UseCulture(CultureUser));
                _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id,
                                                                        _userId,
                                                                        anwser,
                                                                        true);
                return;
            }

            _appServices.UserService.UpdateGold(_userId, userDB.Gold - Constants.Costs.AppleGame);

            await _appServices.UserService.UpdateAppleGameStatus(_userId, true);
            _appServices.BotControlService.SetMyCommandsAsync(Extensions.GetInApplegameCommands(),
                                                              scope: new BotCommandScopeChat() { ChatId = _userId });
            var appleData = _appServices.AppleGameDataService.Get(_userId);

            if (appleData == null)
                _appServices.AppleGameDataService.Create(new AppleGameData()
                {
                    UserId = _userId,
                    CurrentAppleCounter = 24,
                    TotalDraws = 0,
                    TotalLoses = 0,
                    TotalWins = 0,
                    IsGameOvered = false,
                });

            var toSendAnswer = StartGame();

            Log.Debug($"Started AppleGame {_userInfo}");
            await _appServices.BotControlService.SendAnswerMessageAsync(toSendAnswer, _userId, false);
        }
    }
}
