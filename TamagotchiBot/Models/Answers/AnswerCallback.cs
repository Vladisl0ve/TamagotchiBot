using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TamagotchiBot.Models.Answers
{
    public class AnswerCallback
    {
        public AnswerCallback(string textToSend, InlineKeyboardMarkup keyboardMarkup, ParseMode? parseMode = null)
        {
            Text = textToSend;
            InlineKeyboardMarkup = keyboardMarkup;
            ParseMode = parseMode;
        }

        public string Text { get; set; }
        public InlineKeyboardMarkup InlineKeyboardMarkup { get; set; }
        public ParseMode? ParseMode { get; set; } = null;
    }
}
