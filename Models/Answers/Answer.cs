using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TamagotchiBot.Models.Answers
{
    public class Answer
    {
        public Answer() { }
        public Answer(string textToSend, string stickerIdToSend, IReplyMarkup replyMarkup, InlineKeyboardMarkup keyboardMarkup, ParseMode? parse = null)
        {
            Text = textToSend;
            StickerId = stickerIdToSend;
            ReplyMarkup = replyMarkup;
            InlineKeyboardMarkup = keyboardMarkup;
            ParseMode = parse;
        }

        public string Text { get; set; }
        public string StickerId { get; set; }
        public IReplyMarkup ReplyMarkup { get; set; }
        public InlineKeyboardMarkup InlineKeyboardMarkup { get; set; }
        public ParseMode? ParseMode { get; set; } = null;
        public bool IsPetGoneMessage { get; set; } = false;
    }
}
