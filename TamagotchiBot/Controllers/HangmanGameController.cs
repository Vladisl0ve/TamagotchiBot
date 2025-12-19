using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TamagotchiBot.Models.Answers;
using TamagotchiBot.Models.Mongo.Games;
using TamagotchiBot.Services.Interfaces;
using TamagotchiBot.UserExtensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static TamagotchiBot.UserExtensions.Constants;

namespace TamagotchiBot.Controllers
{
    public class HangmanGameController : ControllerBase
    {
        private readonly IApplicationServices _appServices;
        private readonly Message _message;
        private readonly CallbackQuery _callback;
        private readonly long _userId;
        private readonly CultureInfo _userCulture;
        private readonly PetType _userPetType;

        public HangmanGameController(IApplicationServices services, Message message = null, CallbackQuery callback = null)
        {
            _appServices = services;
            _message = message;
            _callback = callback;
            _userId = message?.From?.Id ?? callback.From.Id;
            _userCulture = new CultureInfo(_appServices.UserService.Get(_userId)?.Culture ?? "ru");
            _userPetType = Extensions.GetEnumPetType(_appServices.PetService.Get(_userId)?.Type ?? -1);
        }

        public async Task Menu()
        {
            var gameData = _appServices.HangmanGameDataService.Get(_userId);

            if (gameData == null)
            {
                await _appServices.UserService.UpdateHangmanGameStatus(_userId, false);
                await new MenuController(_appServices, null, _message).ProcessMessage("/gameroom");
                return;
            }

            var msgText = _message.Text;

            // Handle Post-Game Menu
            if (gameData.IsGameOver)
            {
                if (msgText.Contains(nameof(Resources.Resources.PlayAgain_Hangman).UseCulture(_userCulture)))
                {
                    await PreStart();
                    return;
                }

                if (msgText == nameof(Resources.Resources.statisticsText).UseCulture(_userCulture))
                {
                    string stats = string.Format(nameof(Resources.Resources.HangmanGameStatisticsCommand).UseCulture(_userCulture),
                                                        gameData.TotalWins,
                                                        gameData.TotalLoses,
                                                        "");
                    await _appServices.BotControlService.SendAnswerMessageAsync(new AnswerMessage()
                    {
                        Text = stats,
                        ReplyMarkup = GetPostGameKeyboard()
                    }, _userId, false);
                    return;
                }

                if (msgText == nameof(Resources.Resources.quitText).UseCulture(_userCulture))
                {
                    await _appServices.UserService.UpdateHangmanGameStatus(_userId, false);
                    await new MenuController(_appServices, null, _message).ProcessMessage("/gameroom");
                    return;
                }

                if (msgText == "/quit")
                {
                    await _appServices.UserService.UpdateHangmanGameStatus(_userId, false);
                    await new MenuController(_appServices, null, _message).ProcessMessage("/gameroom");
                    return;
                }

                return;
            }

            if (msgText == "/quit")
            {
                gameData.TotalLoses++;
                gameData.IsGameOver = true;
                _appServices.HangmanGameDataService.Update(gameData);
                await _appServices.UserService.UpdateHangmanGameStatus(_userId, false);
                await new MenuController(_appServices, null, _message).ProcessMessage("/gameroom");
                return;
            }

            if (msgText == nameof(Resources.Resources.ConcedeText).UseCulture(_userCulture))
            {
                await EndGame(gameData, false);
                return;
            }

            // Handle Guess
            if (!string.IsNullOrWhiteSpace(msgText) && msgText.Length == 1)
            {
                char guess = char.ToLower(msgText[0], _userCulture);
                if (!char.IsLetter(guess))
                {
                    await SendGameStatus(gameData, nameof(Resources.Resources.hangmanGameInvalidGuess).UseCulture(_userCulture));
                    return;
                }

                await MakeGuess(guess);
            }
            else
            {
                await SendGameStatus(gameData, nameof(Resources.Resources.hangmanGameInvalidGuess).UseCulture(_userCulture));
            }
        }

        public async Task PreStart()
        {
            var petDB = _appServices.PetService.Get(_userId);
            var userDB = _appServices.UserService.Get(_userId);
            if (userDB.Gold < Costs.HangmanGame)
            {
                string anwser = nameof(Resources.Resources.NotEnoughGold).UseCulture(_userCulture);
                await _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwser, true);
                return;
            }

            if (petDB.Fatigue >= 100)
            {
                string anwser = nameof(Resources.Resources.tooTiredText).UseCulture(_userCulture);
                await _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id,
                                                                        _userId,
                                                                        anwser,
                                                                        true);
                return;
            }
            if (petDB.Joy >= 100)
            {
                string anwser = nameof(Resources.Resources.PetIsFullOfJoyText).UseCulture(_userCulture);
                await _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id,
                                                                        _userId,
                                                                        anwser,
                                                                        true);
                return;
            }


            _appServices.UserService.UpdateGold(_userId, userDB.Gold - Costs.HangmanGame);

            var newJoy = petDB.Joy + Factors.HangmanGameJoyFactor;
            if (newJoy > 100) newJoy = 100;
            _appServices.PetService.UpdateJoy(_userId, newJoy);

            var newFatigue = petDB.Fatigue + Factors.HangmanGameFatigueFactor;
            if (newFatigue > 100) newFatigue = 100;
            _appServices.PetService.UpdateFatigue(_userId, newFatigue);


            await _appServices.UserService.UpdateHangmanGameStatus(_userId, true);

            var gameData = _appServices.HangmanGameDataService.Get(_userId);
            if (gameData == null)
            {
                gameData = new HangmanGameData()
                {
                    UserId = _userId,
                    TotalWins = 0,
                    TotalLoses = 0
                };
                _appServices.HangmanGameDataService.Create(gameData);
            }

            // Reset
            gameData.Word = GetRandomWord();
            gameData.GuessedLetters = new List<char>();
            gameData.IsGameOver = false;
            _appServices.HangmanGameDataService.Update(gameData);

            await SendGameStatus(gameData, string.Format(nameof(Resources.Resources.hangmanGameStart).UseCulture(_userCulture), GetHiddenWord(gameData), 0, 7), false);
        }

        private async Task MakeGuess(char letter)
        {
            var gameData = _appServices.HangmanGameDataService.Get(_userId);

            if (gameData.GuessedLetters.Contains(letter))
            {
                await SendGameStatus(gameData, nameof(Resources.Resources.hangmanGameAlreadyGuessed).UseCulture(_userCulture));
                return;
            }

            gameData.GuessedLetters.Add(letter);
            _appServices.HangmanGameDataService.Update(gameData);

            if (dataChecksWin(gameData))
            {
                await EndGame(gameData, true);
                return;
            }

            if (GetWrongGuessesCount(gameData) >= 7)
            {
                await EndGame(gameData, false);
                return;
            }

            await SendGameStatus(gameData, nameof(Resources.Resources.hangmanGameGuessLetter).UseCulture(_userCulture));
        }

        private bool dataChecksWin(HangmanGameData data)
        {
            foreach (char c in data.Word)
            {
                if (!data.GuessedLetters.Contains(c))
                    return false;
            }
            return true;
        }

        private int GetWrongGuessesCount(HangmanGameData data)
        {
            int wrong = 0;
            foreach (char c in data.GuessedLetters)
            {
                if (!data.Word.Contains(c))
                    wrong++;
            }
            return wrong;
        }

        private string GetHiddenWord(HangmanGameData data)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in data.Word)
            {
                if (data.GuessedLetters.Contains(c))
                    sb.Append(c + " ");
                else
                    sb.Append("_ ");
            }
            return sb.ToString().Trim();
        }

        private string GetRandomWord()
        {
            try
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Hangman_dicts", "russian.txt");
                var lines = System.IO.File.ReadAllLines(path);
                if (lines.Length == 0) return "кот"; // Fallback
                return lines[new Random().Next(lines.Length)].ToLower().Trim();
            }
            catch
            {
                return "собака"; // Fallback
            }
        }

        private async Task EndGame(HangmanGameData gameData, bool win)
        {
            gameData.IsGameOver = true;
            string message;
            string hiddenWord = GetHiddenWord(gameData);
            int errors = GetWrongGuessesCount(gameData);
            if (win)
            {
                gameData.TotalWins++;
                var petDB = _appServices.PetService.Get(_userId);
                var newJoy = petDB.Joy + Factors.HangmanGameJoyFactor;
                if (newJoy > 100) newJoy = 100;
                _appServices.PetService.UpdateJoy(_userId, newJoy);

                message = string.Format(nameof(Resources.Resources.hangmanGameWin).UseCulture(_userCulture), gameData.Word, Factors.HangmanGameJoyFactor);
                hiddenWord = gameData.Word.ToUpper();
            }
            else
            {
                gameData.TotalLoses++;
                message = string.Format(nameof(Resources.Resources.hangmanGameLose).UseCulture(_userCulture), gameData.Word);
                hiddenWord = gameData.Word.ToUpper();
                errors = 7;
            }

            _appServices.HangmanGameDataService.Update(gameData);

            await _appServices.BotControlService.SendAnswerMessageAsync(new AnswerMessage()
            {
                Text = message,
                ReplyMarkup = GetPostGameKeyboard(),
                PhotoStream = TamagotchiBot.Services.Helpers.HangmanImageGenerator.GenerateImage(errors, hiddenWord)
            }, _userId, false);
        }

        private ReplyKeyboardMarkup GetPostGameKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton(nameof(Resources.Resources.PlayAgain_Hangman).UseCulture(_userCulture)) },
                new[]
                {
                    new KeyboardButton(nameof(Resources.Resources.statisticsText).UseCulture(_userCulture)),
                    new KeyboardButton(nameof(Resources.Resources.quitText).UseCulture(_userCulture))
                }
            })
            { ResizeKeyboard = true };
        }

        private async Task SendGameStatus(HangmanGameData gameData, string messageHeader, bool addFooter = true)
        {
            string wordStatus = GetHiddenWord(gameData);
            int errors = GetWrongGuessesCount(gameData);

            string text = addFooter ? string.Format(nameof(Resources.Resources.hangmanFooter).UseCulture(_userCulture), messageHeader, wordStatus, errors)
                                    : messageHeader;

            await _appServices.BotControlService.SendAnswerMessageAsync(new AnswerMessage()
            {
                Text = text,
                ReplyMarkup = GetGameKeyboard(),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html,
                PhotoStream = TamagotchiBot.Services.Helpers.HangmanImageGenerator.GenerateImage(errors, wordStatus)
            }, _userId, false);
        }

        private ReplyKeyboardMarkup GetGameKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                 new[] { new KeyboardButton(nameof(Resources.Resources.ConcedeText).UseCulture(_userCulture)) }
             })
            { ResizeKeyboard = true };
        }

    }
}
