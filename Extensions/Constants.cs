using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;

namespace TamagotchiBot.UserExtensions
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

        public enum Fatigue
        {
            Fresh,
            Rested,
            SlightlyTired,
            Tired,
            Sleepy
        }

        public static string LanguageBe = "be";
        public static string LanguagePl = "pl";
        public static string LanguageRu = "ru";
        public static string LanguageEn = "en";

        #region Factors
        public static int ExpFactor = 1;
        public static int ExpToLvl = 100;
        public static double StarvingFactor = 0.138;
        public static double FatigueFactor = 0.083;
        public static double RestFactor = 0.42;
        #endregion

        #region FoodFactors
        public static double BreadHungerFactor = 10; //🍞
        public static double RedAppleHungerFactor = 5; //🍎
        public static double ChocolateHungerFactor = 4; //🍫
        public static double LollipopHungerFactor = 1; //🍭
        #endregion

        #region Commands
        public static string KitchenCommand = "kitchen";
        public static string PetCommand = "pet";
        public static string LanguageCommand = "language";
        //public static string KitchenCommand = "kitchen";
        #endregion


        #region StickersId
        //Common
        public static string WelcomeSticker = "CAACAgIAAxkBAAEDHvdhcG0r5WOkfladhV2zTUYwN6LyOQACUwADr8ZRGjkySUcbM1VLIQQ";
        public static string ChangeLanguageSticker = "CAACAgIAAxkBAAEDIdRhcygJqmnt4ibdxEVejHOQ4Ya7pwACbAIAAladvQoqGV6cxNDenyEE";
        public static string DevelopWarningSticker = "CAACAgIAAxkBAAEDHxNhcHJP59QL8Fe9GaY3POWBIeII6QACUQADLMqqByX_VpH__oXBIQQ";
        public static string PolishLanguageSetSticker = "CAACAgIAAxkBAAEDHxVhcHU6BuzdT1sw-MZB0uBR35h5iAACKwEAAr8DyQQgsxfQYO--ECEE";
        public static string EnglishLanguageSetSticker = "CAACAgIAAxkBAAEDHxdhcHV4y14-CyrH_D1YujHDCBROUQAC6AADvwPJBGHtqaDNJtEyIQQ";
        public static string RussianLanguageSetSticker = "CAACAgIAAxkBAAEDHxlhcHWCiuvBtQ-IZJknE2hlBlZ-TwAC4gADvwPJBOLja80qqucgIQQ";
        public static string BelarussianLanguageSetSticker = "CAACAgIAAxkBAAEDIdJhcyf3ErjEmUZRgDJgMsCtstPpGAACYQIAAladvQq0dN7WdBr5ViEE";

        //Cat
        public static string PetCreated_Cat = "CAACAgIAAxkBAAEDHvlhcG2oG4rLAAGPvREkKoykMsNnYzsAAlsQAAKlvUhKsth-8cNoWVghBA";
        public static string PetInfo_Cat = "CAACAgIAAxkBAAEDHwFhcG3C-_owIcuMOR9GTlE4MeoTOAACvRIAAhxUSUo2xUCLEnwQHiEE";
        public static string PetChooseName_Cat = "CAACAgIAAxkBAAEDHwthcG-wxtTfvF_S-6mqam-KwksPnQAC5RAAAowt_QftGb7TeRsiTyEE";
        public static string PetConfirmedName_Cat = "CAACAgIAAxkBAAEDHw1hcHBpvQQti1cmSC1LVKRNOtV3FwACjBIAAtJ0SUqCGw6E9UM1giEE";
        public static string PetKitchen_Cat = "CAACAgIAAxkBAAEDIFVhcfZFjhITgwR6llMbPY-58IL_RAACxA4AA7xBSg8_gz8dIW-OIQQ";


        //Tests
        public static List<Tuple<string, string>> inlineParts = new List<Tuple<string, string>>()
                {
                    new Tuple<string, string>("🍞", "kitchenCommandInlineBread"),
                    new Tuple<string, string>("🍎", "kitchenCommandInlineRedApple"),
                    new Tuple<string, string>("🍫", "kitchenCommandInlineChocolate"),
                    new Tuple<string, string>("🍭", "kitchenCommandInlineLollipop")
                };

        #endregion
        public static ReplyKeyboardMarkup LanguagesMarkup = Extensions.ReplyKeyboardOptimizer(Extensions.LanguagesWithFlags());
    }
}
