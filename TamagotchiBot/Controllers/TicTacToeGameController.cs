using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using TamagotchiBot.Models.Answers;
using TamagotchiBot.Models.Mongo.Games;
using TamagotchiBot.Services.Interfaces;
using TamagotchiBot.UserExtensions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static TamagotchiBot.UserExtensions.Constants;
using Extensions = TamagotchiBot.UserExtensions.Extensions;
using Serilog;

namespace TamagotchiBot.Controllers
{
    public class TicTacToeGameController : ControllerBase
    {
        private readonly IApplicationServices _appServices;
        private readonly Message _message;
        private readonly CallbackQuery _callback;
        private readonly long _userId;
        private readonly CultureInfo _userCulture;
        private readonly string _userInfo;
        private readonly PetType _userPetType;

        public TicTacToeGameController(IApplicationServices services, Message message = null, CallbackQuery callback = null)
        {
            _appServices = services;
            _message = message;
            _callback = callback;
            _userId = message?.From?.Id ?? callback.From.Id;
            _userInfo = Extensions.GetLogUser(_appServices.UserService.Get(_userId));
            _userCulture = new CultureInfo(_appServices.UserService.Get(_userId)?.Culture ?? "ru");
            _userPetType = Extensions.GetEnumPetType(_appServices.PetService.Get(_userId)?.Type ?? -1);
        }

        public async Task Menu()
        {
            var gameData = _appServices.TicTacToeGameDataService.Get(_userId);

            if (gameData == null) // Just in case
            {
                await _appServices.UserService.UpdateTicTacToeGameStatus(_userId, false);
                await new MenuController(_appServices, null, _message).ProcessMessage("/gameroom");
                return;
            }

            var msgText = _message.Text;

            // Handle Post-Game Menu
            if (gameData.IsGameOver)
            {
                if (msgText.Contains(nameof(Resources.Resources.PlayAgain_TicTakToe).UseCulture(_userCulture))) // Check partial match for "Play again" as it might have emoji
                {
                    await PreStart();
                    return;
                }

                if (msgText == nameof(Resources.Resources.statisticsText).UseCulture(_userCulture))
                {
                    string stats = string.Format(nameof(Resources.Resources.TicTakToeGameStatisticsCommand).UseCulture(_userCulture),
                                               gameData.TotalWins,
                                               gameData.TotalLoses,
                                               gameData.TotalDraws);
                    await _appServices.BotControlService.SendAnswerMessageAsync(new AnswerMessage()
                    {
                        Text = stats,
                        ReplyMarkup = GetPostGameKeyboard()
                    }, _userId, false);
                    return;
                }

                if (msgText == nameof(Resources.Resources.quitText).UseCulture(_userCulture))
                {
                    await _appServices.UserService.UpdateTicTacToeGameStatus(_userId, false);
                    await new MenuController(_appServices, null, _message).ProcessMessage("/gameroom");
                    return;
                }

                if (msgText == "/quit")
                {
                    await _appServices.UserService.UpdateTicTacToeGameStatus(_userId, false);
                    await new MenuController(_appServices, null, _message).ProcessMessage("/gameroom");
                    return;
                }


                // If unknown command in game over state, maybe re-show stats or just ignore?
                // For now, assume re-show menu
                return;
            }

            if (msgText == "/quit")
            {
                gameData.TotalLoses++;
                gameData.IsGameOver = true;
                _appServices.TicTacToeGameDataService.Update(gameData);
                await _appServices.UserService.UpdateTicTacToeGameStatus(_userId, false);
                await new MenuController(_appServices, null, _message).ProcessMessage("/gameroom");
                return;
            }

            if (msgText == nameof(Resources.Resources.ConcedeText).UseCulture(_userCulture))
            {
                await EndGame(gameData, 2);
                return;
            }

            // Process move
            // Button text format: "⬜ 1", "⬜ 2", etc.
            var splitText = msgText.Split(' ');
            if (splitText.Length > 1 && int.TryParse(splitText[splitText.Length - 1], out int cellIndex))
            {
                await MakeMove(cellIndex - 1); // 0-8
            }
            else
            {
                // Ignore invalid input, maybe show board again?
                await SendBoard(gameData, nameof(Resources.Resources.InvalidMove_text).UseCulture(_userCulture));
            }
        }

        public async Task PreStart()
        {
            var userDB = _appServices.UserService.Get(_userId);
            var petDB = _appServices.PetService.Get(_userId);
            if (userDB.Gold < Costs.TicTacToeGame)
            {
                string anwser = nameof(Resources.Resources.NotEnoughGold).UseCulture(_userCulture);
                if (_callback != null)
                    await _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwser, true);
                else
                    await _appServices.BotControlService.SendAnswerMessageAsync(new AnswerMessage() { Text = anwser }, _userId, false);
                return;
            }

            if (petDB.Fatigue >= 100)
            {
                string anwser = nameof(Resources.Resources.tooTiredText).UseCulture(_userCulture);
                if (_callback != null)
                    await _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwser, true);
                else
                    await _appServices.BotControlService.SendAnswerMessageAsync(new AnswerMessage() { Text = anwser }, _userId, false);
                return;
            }
            if (petDB.Joy >= 100)
            {
                string anwser = nameof(Resources.Resources.PetIsFullOfJoyText).UseCulture(_userCulture);
                if (_callback != null)
                    await _appServices.BotControlService.AnswerCallbackQueryAsync(_callback.Id, _userId, anwser, true);
                else
                    await _appServices.BotControlService.SendAnswerMessageAsync(new AnswerMessage() { Text = anwser }, _userId, false);
                return;
            }


            _appServices.UserService.UpdateGold(_userId, userDB.Gold - Costs.TicTacToeGame);
            await _appServices.UserService.UpdateTicTacToeGameStatus(_userId, true);

            var gameData = _appServices.TicTacToeGameDataService.Get(_userId);
            if (gameData == null)
            {
                gameData = new TicTacToeGameData()
                {
                    UserId = _userId,
                    TotalWins = 0,
                    TotalLoses = 0,
                    TotalDraws = 0
                };
                _appServices.TicTacToeGameDataService.Create(gameData);
            }

            // Reset for new game
            gameData.Board = "000000000";
            gameData.IsGameOver = false;
            gameData.CurrentTurn = 1; // User starts
            _appServices.TicTacToeGameDataService.Update(gameData);

            await SendBoard(gameData, nameof(Resources.Resources.startGameTicTacToeText).UseCulture(_userCulture));
            Log.Debug($"Started TicTacToeGame {_userInfo}");
        }

        private async Task MakeMove(int cellIndex)
        {
            Log.Information($"TicTacToeGame MakeMove {_userInfo}");
            var gameData = _appServices.TicTacToeGameDataService.Get(_userId);
            if (cellIndex < 0 || cellIndex > 8 || gameData.Board[cellIndex] != '0')
            {
                await SendBoard(gameData, nameof(Resources.Resources.InvalidMove_text).UseCulture(_userCulture));
                return;
            }

            // User move
            var boardChars = gameData.Board.ToCharArray();
            boardChars[cellIndex] = '1';
            gameData.Board = new string(boardChars);

            if (CheckWin(gameData.Board, '1'))
            {
                await EndGame(gameData, 1);
                return;
            }
            if (!gameData.Board.Contains('0'))
            {
                await EndGame(gameData, 0); // Draw
                return;
            }

            // Bot move
            gameData.CurrentTurn = 2;
            int botMove = GetBotMove(gameData.Board);
            boardChars = gameData.Board.ToCharArray();
            boardChars[botMove] = '2';
            gameData.Board = new string(boardChars);

            if (CheckWin(gameData.Board, '2'))
            {
                await EndGame(gameData, 2);
                return;
            }
            if (!gameData.Board.Contains('0'))
            {
                await EndGame(gameData, 0); // Draw
                return;
            }

            gameData.CurrentTurn = 1;
            _appServices.TicTacToeGameDataService.Update(gameData);
            string msg = string.Format(nameof(Resources.Resources.YourTurn_text).UseCulture(_userCulture), Extensions.GetTypeEmoji(_userPetType));

            await SendBoard(gameData, msg);
        }

        private int GetBotMove(string board)
        {
            // If it's the very first move of the game (board is empty or only 1 move made), 
            // taking center or corner is usually best. Minimax covers this, but we can speed it up or add variety.
            // For now, let's just use pure Minimax for optimal play.

            int bestVal = -1000;
            int bestMove = -1;
            char[] boardArr = board.ToCharArray();

            for (int i = 0; i < boardArr.Length; i++)
            {
                if (boardArr[i] == '0')
                {
                    boardArr[i] = '2'; // Bot makes move
                    int moveVal = Minimax(new string(boardArr), 0, false);
                    boardArr[i] = '0'; // Undo

                    if (moveVal > bestVal)
                    {
                        bestMove = i;
                        bestVal = moveVal;
                    }
                }
            }
            return bestMove;
        }

        private int Minimax(string board, int depth, bool isMax)
        {
            if (CheckWin(board, '2')) return 10 - depth;
            if (CheckWin(board, '1')) return -10 + depth;
            if (!board.Contains('0')) return 0;

            if (isMax)
            {
                int best = -1000;
                char[] boardArr = board.ToCharArray();
                for (int i = 0; i < boardArr.Length; i++)
                {
                    if (boardArr[i] == '0')
                    {
                        boardArr[i] = '2';
                        best = Math.Max(best, Minimax(new string(boardArr), depth + 1, !isMax));
                        boardArr[i] = '0';
                    }
                }
                return best;
            }
            else
            {
                int best = 1000;
                char[] boardArr = board.ToCharArray();
                for (int i = 0; i < boardArr.Length; i++)
                {
                    if (boardArr[i] == '0')
                    {
                        boardArr[i] = '1';
                        best = Math.Min(best, Minimax(new string(boardArr), depth + 1, !isMax));
                        boardArr[i] = '0';
                    }
                }
                return best;
            }
        }

        private bool CheckWin(string board, char player)
        {
            int[][] wins = new int[][]
            {
                new[] {0,1,2}, new[] {3,4,5}, new[] {6,7,8}, // Rows
                new[] {0,3,6}, new[] {1,4,7}, new[] {2,5,8}, // Cols
                new[] {0,4,8}, new[] {2,4,6}                 // Diags
            };

            foreach (var line in wins)
            {
                if (board[line[0]] == player && board[line[1]] == player && board[line[2]] == player)
                    return true;
            }
            return false;
        }

        private async Task EndGame(TicTacToeGameData gameData, int winner)
        {
            gameData.IsGameOver = true;
            string message = "";
            if (winner == 1)
            {
                gameData.TotalWins++;
                message = string.Format(nameof(Resources.Resources.YouWon).UseCulture(_userCulture), Extensions.GetTypeEmoji(_userPetType));
            }
            else if (winner == 2)
            {
                gameData.TotalLoses++;
                message = string.Format(nameof(Resources.Resources.YouLost_TicTakToe).UseCulture(_userCulture), Extensions.GetTypeEmoji(_userPetType));
            }
            else
            {
                gameData.TotalDraws++;
                message = string.Format(nameof(Resources.Resources.Draw_TicTakToe).UseCulture(_userCulture), Extensions.GetTypeEmoji(_userPetType));
            }

            var petDB = _appServices.PetService.Get(_userId);
            var newJoy = petDB.Joy + Factors.TicTacToeGameJoyFactor;
            if (newJoy > 100) newJoy = 100;
            _appServices.PetService.UpdateJoy(_userId, newJoy);

            var newFatigue = petDB.Fatigue + Factors.TicTacToeGameFatigueFactor;
            if (newFatigue > 100) newFatigue = 100;
            _appServices.PetService.UpdateFatigue(_userId, newFatigue);

            _appServices.TicTacToeGameDataService.Update(gameData);
            // DO NOT set IsInTicTacToeGame to false heavily yet, as we want to handle the post-game menu. 
            // BUT GameData.IsGameOver is true, which we check in Menu().
            // So we keep user in "game mode" (IsInTicTacToeGame = true) until they click Quit.

            await _appServices.BotControlService.SendAnswerMessageAsync(new AnswerMessage()
            {
                Text = message + "\n",
                ReplyMarkup = GetPostGameKeyboard()
            }, _userId, false);
        }

        private ReplyKeyboardMarkup GetPostGameKeyboard()
        {
            return new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton(string.Format(nameof(Resources.Resources.PlayAgain_TicTakToe).UseCulture(_userCulture), Extensions.GetTypeEmoji(_userPetType))) },
                new[]
                {
                    new KeyboardButton(nameof(Resources.Resources.statisticsText).UseCulture(_userCulture)),
                    new KeyboardButton(nameof(Resources.Resources.quitText).UseCulture(_userCulture))
                }
            })
            { ResizeKeyboard = true };
        }

        private async Task SendBoard(TicTacToeGameData gameData, string message)
        {
            await _appServices.BotControlService.SendAnswerMessageAsync(new AnswerMessage()
            {
                Text = message + "\n",
                ReplyMarkup = GetKeyboard(gameData.Board),
                ParseMode = Telegram.Bot.Types.Enums.ParseMode.Html
            }, _userId, false);
        }

        private ReplyKeyboardMarkup GetKeyboard(string board)
        {
            var rows = new List<KeyboardButton[]>();
            for (int i = 0; i < 3; i++)
            {
                var row = new KeyboardButton[3];
                for (int j = 0; j < 3; j++)
                {
                    int index = i * 3 + j;
                    string text = " ";
                    if (board[index] == '1') text = "❌";
                    else if (board[index] == '2') text = "⭕";
                    else text = $"{GetEmoji(index)} {index + 1}"; // Number or invisible char? Requested logic?
                    // Let's use simple indicators or just cell numbers
                    if (board[index] == '0') text = $"⬜ {index + 1}";

                    row[j] = new KeyboardButton(text);
                }
                rows.Add(row);
            }
            rows.Add(new[] { new KeyboardButton(string.Format(nameof(Resources.Resources.ConcedeText).UseCulture(_userCulture))) });
            return new ReplyKeyboardMarkup(rows) { ResizeKeyboard = true };
        }

        private string GetEmoji(int index)
        {
            return "⬜";
        }
    }
}
