using Telegram.Bot.Types.ReplyMarkups;

namespace TamagotchiBot.Models.Answers
{
    public class AnswerCallback
    {

        public AnswerCallback(string textToSend, InlineKeyboardMarkup keyboardMarkup)
        {
            Text = textToSend;
            InlineKeyboardMarkup = keyboardMarkup;
        }

        public string Text { get; set; }
        public InlineKeyboardMarkup InlineKeyboardMarkup { get; set; }
    }
}
