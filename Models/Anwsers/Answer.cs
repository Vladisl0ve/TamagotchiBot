using Telegram.Bot.Types.ReplyMarkups;

namespace TamagotchiBot.Models.Anwsers
{
    public class Answer
    {
        public Answer() { }
        public Answer(string textToSend, string stickerIdToSend, IReplyMarkup replyMarkup, InlineKeyboardMarkup keyboardMarkup)
        {
            Text = textToSend;
            StickerId = stickerIdToSend;
            ReplyMarkup = replyMarkup;
            InlineKeyboardMarkup = keyboardMarkup;
        }

        public string Text { get; set; }
        public string StickerId { get; set; }
        public IReplyMarkup ReplyMarkup { get; set; }
        public InlineKeyboardMarkup InlineKeyboardMarkup { get; set; }
    }
}
