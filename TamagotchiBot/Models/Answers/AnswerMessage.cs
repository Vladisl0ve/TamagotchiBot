using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TamagotchiBot.Models.Answers
{
    public class AnswerMessage
    {
        public AnswerMessage() { }
        public AnswerMessage(string textToSend, string stickerIdToSend, ReplyMarkup replyMarkup, InlineKeyboardMarkup keyboardMarkup, ParseMode? parse = null)
        {
            Text = textToSend;
            StickerId = stickerIdToSend;
            ReplyMarkup = replyMarkup;
            InlineKeyboardMarkup = keyboardMarkup;
            ParseMode = parse ?? ParseMode.None;
        }

        public string Text { get; set; }
        public string StickerId { get; set; }
        public ReplyMarkup ReplyMarkup { get; set; }
        public InlineKeyboardMarkup InlineKeyboardMarkup { get; set; }
        public ParseMode ParseMode { get; set; } = ParseMode.None;
        public int? replyToMsgId { get; set; } = null;
        public int? msgThreadId { get; set; } = null;
        public System.IO.Stream? PhotoStream { get; set; } = null;
    }
}
