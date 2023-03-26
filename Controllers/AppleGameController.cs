using System;
using System.Collections.Generic;
using System.Globalization;
using TamagotchiBot.Models;
using TamagotchiBot.Models.Answers;
using TamagotchiBot.UserExtensions;
using TamagotchiBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static TamagotchiBot.Resources.Resources;
using static TamagotchiBot.UserExtensions.Constants;

namespace TamagotchiBot.Controllers
{
    public class AppleGameController
    {
        private readonly UserService _userService;
        private readonly PetService _petService;
        private readonly ChatService _chatService;
        private readonly AppleGameDataService _appleGameDataService;
        private readonly BotControlService _bcService;

        private readonly ITelegramBotClient bot;
        private readonly Message message;
        private readonly CallbackQuery callback;

        private readonly long UserId = 0;

        private const int APPLE_COUNTER_DEFAULT = 24;

        private AppleGameController(ITelegramBotClient bot,
                           UserService userService,
                           PetService petService,
                           ChatService chatService,
                           AppleGameDataService appleGameDataService,
                           BotControlService bcService)
        {
            this.bot = bot;
            _userService = userService;
            _petService = petService;
            _chatService = chatService;
            _appleGameDataService = appleGameDataService;

            Culture = new CultureInfo(_userService.Get(UserId)?.Culture ?? "ru");
            _bcService = bcService;
        }

        public AppleGameController(ITelegramBotClient bot,
                                   UserService userService,
                                   PetService petService,
                                   ChatService chatService,
                                   AppleGameDataService appleGameDataService,
                                   BotControlService botControlService,
                                   CallbackQuery callback) : this(bot, userService, petService, chatService, appleGameDataService, botControlService)
        {

            AppleCounter = appleGameDataService.Get(callback.From.Id)?.CurrentAppleCounter ?? 1;
            this.callback = callback;
            UserId = callback.From.Id;
            Culture = new CultureInfo(_userService.Get(UserId)?.Culture ?? "ru");
        }

        public AppleGameController(ITelegramBotClient bot,
                                   UserService userService,
                                   PetService petService,
                                   ChatService chatService,
                                   AppleGameDataService appleGameDataService,
                                   BotControlService botControlService,
                                   Message message) : this(bot, userService, petService, chatService, appleGameDataService, botControlService)
        {
            AppleCounter = appleGameDataService.Get(message.From.Id)?.CurrentAppleCounter ?? 1;
            this.message = message;
            UserId = message.From.Id;
            Culture = new CultureInfo(_userService.Get(UserId)?.Culture ?? "ru");
        }


        public int AppleCounter { get; set; }
        public List<string> MenuCommands => new List<string>() { againText, statisticsText, quitText };
        public List<string> ApplesToChoose
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

        public string ApplesIcons
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

        public Answer StartGame()
        {
            var appleDataToUpdate = _appleGameDataService.Get(UserId);
            appleDataToUpdate.CurrentAppleCounter =
            AppleCounter = APPLE_COUNTER_DEFAULT;
            _appleGameDataService.Update(appleDataToUpdate);

            string text = $"{appleGameHelpText}\n\n{string.Format(remainingApplesText, AppleCounter)}\n{ApplesIcons}";
            var keyboard = KeyboardOptimizer(ApplesToChoose);
            return new Answer()
            {
                Text = text,
                ReplyMarkup = keyboard
            };
        }

        public Answer Menu(Message message)
        {
            var appleDataToUpdate = _appleGameDataService.Get(UserId);
            var petDB = _petService.Get(UserId);
            var chatDB = _chatService.Get(UserId);

            if (message.Text == statisticsText && appleDataToUpdate.IsGameOvered)
            {
                var toSendText = string.Format(appleGameStatisticsCommand, appleDataToUpdate.TotalWins, appleDataToUpdate.TotalLoses, appleDataToUpdate.TotalDraws);

                chatDB.LastMessage = "/statsApple";
                _chatService.Update(chatDB.ChatId, chatDB);
                return new Answer()
                {
                    Text = toSendText,
                    ReplyMarkup = KeyboardOptimizer(MenuCommands)
                };
            }

            if (message.Text == ConcedeText)
            {
                appleDataToUpdate.TotalDraws += 1;
                appleDataToUpdate.IsGameOvered = true;
                _appleGameDataService.Update(appleDataToUpdate);

                string toSendText = $"{appleGameHelpText}";

                return new Answer()
                {
                    Text = toSendText,
                    ReplyMarkup = KeyboardOptimizer(MenuCommands)
                };
            }

            if (message.Text == quitText || message.Text == "/quit")
            {
                appleDataToUpdate.TotalLoses += 1;
                appleDataToUpdate.IsGameOvered = true;
                _appleGameDataService.Update(appleDataToUpdate);

                _userService.UpdateAppleGameStatus(UserId, false);

                string toSendText = string.Format(gameroomCommand, petDB.Fatigue, petDB.Joy);

                List<CommandModel> inlineParts = new InlineItems().InlineGames;
                InlineKeyboardMarkup toSendInline = Extensions.InlineKeyboardOptimizer(inlineParts, 3);

                chatDB.LastMessage = "/quitApple";
                _chatService.Update(chatDB.ChatId, chatDB);
                return new Answer()
                {
                    Text = toSendText,
                    StickerId = StickersId.PetGameroom_Cat,
                    InlineKeyboardMarkup = toSendInline,
                    ReplyMarkup = new ReplyKeyboardRemove()
                };
            }

            if (message.Text == againText)
            {
                return StartGame();
            }

            if (message.Text != "🍎" && message.Text != "🍎🍎" && message.Text != "🍎🍎🍎")
                return new Answer() { Text = appleGameUndefiendText, ReplyMarkup = KeyboardOptimizer(ApplesToChoose) };

            return MakeMove(message);
        }

        public Answer MakeMove(Message message)
        {
            var appleDataToUpdate = _appleGameDataService.Get(UserId);
            var petDB = _petService.Get(UserId);

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

                    break;
                default:
                    textToSay += string.Format(remainingApplesText, AppleCounter);
                    appleDataToUpdate.CurrentAppleCounter = AppleCounter;
                    break;
            }

            _appleGameDataService.Update(appleDataToUpdate);
            _petService.Update(UserId, petDB);
            return new Answer()
            {
                Text = textToSay,
                ReplyMarkup = KeyboardOptimizer(ApplesToChoose)
            };
        }

    }
}
