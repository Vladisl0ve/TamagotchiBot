using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;

namespace TamagotchiBot.Extensions
{
    public static class Constants
    {
        public enum Language
        {
            [Display(ShortName = "🇵🇱", Name = "pl")] Polski,
            [Display(ShortName = "🇺🇸", Name = "en")] English,
            [Display(ShortName = "🇰🇵", Name = "be")] Беларуская,
            [Display(ShortName = "🇷🇺", Name = "ru")] Русский
        }

        public static string LanguageBe = "be";
        public static string LanguagePl = "pl";
        public static string LanguageRu = "ru";
        public static string LanguageEn = "en";


        public static ReplyKeyboardMarkup LanguagesMarkup = Extensions.KeyboardOptimizer(Extensions.LanguagesWithFlags());
    }
}
