using System;
using System.Collections.Generic;
using System.Globalization;
using TamagotchiBot.Models;
using TamagotchiBot.Models.Answers;
using TamagotchiBot.UserExtensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static TamagotchiBot.Resources.Resources;
using static TamagotchiBot.UserExtensions.Constants;
using TamagotchiBot.Services.Interfaces;
using Extensions = TamagotchiBot.UserExtensions.Extensions;

namespace TamagotchiBot.Controllers
{
    public class AppleGameController
    {
        private readonly IApplicationServices _appServices;

        private readonly Message _message;
        private readonly CallbackQuery _callback;

        private readonly long _userId = 0;

        private const int APPLE_COUNTER_DEFAULT = 24;

        private AppleGameController(IApplicationServices services, Message message = null, CallbackQuery callback = null)
        {
            _callback = callback;
            _message = message;
            _userId = message?.From?.Id ?? callback.From.Id;
            _appServices = services;

            Culture = new CultureInfo(_appServices.UserService.Get(_userId)?.Culture ?? "ru");
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
        private List<string> MenuCommands => new List<string>() { againText, statisticsText, quitText };
        private List<string> ApplesToChoose
        {
            get
            {
                return AppleCounter switch
                {
                    1 => new List<string>() { againText, statisticsText, quitText },
                    2 => new List<string>() { "🍎", ConcedeText },
                    3 => new List<string>() { "🍎", "🍎🍎", ConcedeText },
                    _ => new List<string>() { "🍎", "🍎🍎", "🍎🍎🍎", ConcedeText },
                };
            }
        }

        private string ApplesIcons
        {
            get
            {
                string greenApple = "🍏";
                string redApple = "🍎";

                string result = greenApple;
                for (int i = 1; i < AppleCounter; i++)
                {
                    result += $"{redApple}";
                }
                return result;
            }
        }

        public bool IsOver
        {
            get => AppleCounter <= 1;
        }

        private ReplyKeyboardMarkup KeyboardOptimizer(List<string> names)
        {
            int x = 2;
            int y = (int)Math.Ceiling((double)(names.Count) / x);
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

            return new ReplyKeyboardMarkup(keyboard) { ResizeKeyboard = true, OneTimeKeyboard = true };
        }

        public AnswerMessage StartGame()
        {
            var appleDataToUpdate = _appServices.AppleGameDataService.Get(_userId);
            appleDataToUpdate.CurrentAppleCounter =
            AppleCounter = APPLE_COUNTER_DEFAULT;
            _appServices.AppleGameDataService.Update(appleDataToUpdate);

            string text = $"{appleGameHelpText}\n\n{string.Format(remainingApplesText, AppleCounter)}\n{ApplesIcons}";
            var keyboard = KeyboardOptimizer(ApplesToChoose);
            return new AnswerMessage()
            {
                Text = text,
                ReplyMarkup = keyboard
            };
        }

        public AnswerMessage Menu()
        {
            var appleDataToUpdate = _appServices.AppleGameDataService.Get(_userId);
            var petDB = _appServices.PetService.Get(_userId);
            var userDB = _appServices.UserService.Get(_userId);

            if (appleDataToUpdate == null)
            {
                _appServices.UserService.UpdateAppleGameStatus(_userId, false);
                new MenuController(_appServices, _message).ProcessMessage("/pet");
                return null;
            }

            if (_message.Text == statisticsText && appleDataToUpdate.IsGameOvered)
            {
                var toSendText = string.Format(appleGameStatisticsCommand,
                                               appleDataToUpdate.TotalWins,
                                               appleDataToUpdate.TotalLoses,
                                               appleDataToUpdate.TotalDraws);

                return new AnswerMessage()
                {
                    Text = toSendText,
                    ReplyMarkup = KeyboardOptimizer(MenuCommands)
                };
            }

            if (_message.Text == ConcedeText)
            {
                appleDataToUpdate.TotalDraws += 1;
                appleDataToUpdate.IsGameOvered = true;
                _appServices.AppleGameDataService.Update(appleDataToUpdate);

                string toSendText = $"{appleGameHelpText}";

                return new AnswerMessage()
                {
                    Text = toSendText,
                    ReplyMarkup = KeyboardOptimizer(MenuCommands)
                };
            }

            if (_message.Text == quitText || _message.Text == "/quit")
            {
                appleDataToUpdate.TotalLoses += 1;
                appleDataToUpdate.IsGameOvered = true;
                _appServices.AppleGameDataService.Update(appleDataToUpdate);

                _appServices.UserService.UpdateAppleGameStatus(_userId, false);

                string toSendText = string.Format(gameroomCommand,
                                                  petDB.Fatigue,
                                                  petDB.Joy,
                                                  userDB.Gold,
                                                  Factors.CardGameJoyFactor,
                                                  Costs.AppleGame,
                                                  Factors.DiceGameJoyFactor,
                                                  Costs.DiceGame);

                List<CallbackModel> inlineParts = new InlineItems().InlineGames;
                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

                return new AnswerMessage()
                {
                    Text = toSendText,
                    StickerId = StickersId.PetGameroom_Cat,
                    InlineKeyboardMarkup = toSendInline,
                    ReplyMarkup = new ReplyKeyboardRemove()
                };
            }

            if (_message.Text == againText)
            {
                return StartGame();
            }

            if (_message.Text != "🍎" && _message.Text != "🍎🍎" && _message.Text != "🍎🍎🍎")
                return new AnswerMessage() { Text = appleGameUndefiendText, ReplyMarkup = KeyboardOptimizer(ApplesToChoose) };

            return MakeMove(_message);
        }

        public AnswerMessage MakeMove(Message message)
        {
            var appleDataToUpdate = _appServices.AppleGameDataService.Get(_userId);
            var petDB = _appServices.PetService.Get(_userId);
            var aud = _appServices.AllUsersDataService.Get(_userId);

            int toRemove = message.Text == "🍎" ? 1 : message.Text == "🍎🍎" ? 2 : message.Text == "🍎🍎🍎" ? 3 : 0;
            AppleCounter -= toRemove;
            int systemRemove = 0;
            string textToSay = "";

            switch (AppleCounter)
            {
                case 1:
                    break;
                case 2:
                    systemRemove = 1;
                    AppleCounter -= systemRemove;
                    break;
                case 3:
                    systemRemove = 2;
                    AppleCounter -= systemRemove;
                    break;
                case 4:
                    systemRemove = 3;
                    AppleCounter -= systemRemove;
                    break;
                case 5:
                    systemRemove = 1;
                    AppleCounter -= systemRemove;
                    break;
                case 6:
                    systemRemove = 1;
                    AppleCounter -= systemRemove;
                    break;
                case 7:
                    systemRemove = new Random().Next(1, 3);
                    AppleCounter -= systemRemove;
                    break;
                case 8:
                    systemRemove = 3;
                    AppleCounter -= systemRemove;
                    break;
                default:
                    systemRemove = new Random().Next(1, 4);
                    AppleCounter -= systemRemove;
                    break;
            }

            textToSay += $"{string.Format(appleGameSysEaten, systemRemove)}";

            textToSay += $"\n\n{ApplesIcons}\n";

            switch (AppleCounter)
            {
                case 1 when systemRemove != 0:
                    textToSay += appleGameLoseText;
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
                    textToSay += appleGameWinText;
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
                    textToSay += string.Format(remainingApplesText, AppleCounter);
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

        public void PreStart()
        {
            var petDB = _appServices.PetService.Get(_userId);
            var userDB = _appServices.UserService.Get(_userId);

            if (petDB == null || userDB == null)
                return;

            if (petDB?.Fatigue >= 100)
            {
                string anwser = string.Format(Resources.Resources.tooTiredText);
                _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id,
                                                                        _userId,
                                                                        anwser,
                                                                        true);
                return;
            }
            if (petDB?.Joy >= 100)
            {
                string anwser = string.Format(Resources.Resources.PetIsFullOfJoyText);
                _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id,
                                                                        _userId,
                                                                        anwser,
                                                                        true);
                return;
            }

            if (userDB?.Gold <= Constants.Costs.AppleGame)
            {
                string anwser = string.Format(Resources.Resources.goldNotEnough);
                _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id,
                                                                        _userId,
                                                                        anwser,
                                                                        true);
                return;
            }

            _appServices.UserService.UpdateGold(_userId, userDB.Gold - Constants.Costs.AppleGame);

            _appServices.UserService.UpdateAppleGameStatus(_userId, true);
            _appServices.BotControlService.SetMyCommandsAsync(_userId,
                                                              Extensions.GetInApplegameCommands(),
                                                              scope: new BotCommandScopeChat() { ChatId = _userId });
            var appleData = _appServices.AppleGameDataService.Get(_userId);

            if (appleData == null)
                _appServices.AppleGameDataService.Create(new Models.Mongo.Games.AppleGameData()
                {
                    UserId = _userId,
                    CurrentAppleCounter = 24,
                    TotalDraws = 0,
                    TotalLoses = 0,
                    TotalWins = 0,
                    IsGameOvered = false,
                });

            var toSendAnswer = StartGame();
            _appServices.BotControlService.SendAnswerMessageAsync(toSendAnswer, _userId);
            return;
        }
    }
}
