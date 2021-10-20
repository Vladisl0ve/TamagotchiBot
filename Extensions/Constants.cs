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

        public static string LanguageBe = "be";
        public static string LanguagePl = "pl";
        public static string LanguageRu = "ru";
        public static string LanguageEn = "en";

        #region StickersId
        //Common
        public static string WelcomeSticker = "CAACAgIAAxkBAAEDHvdhcG0r5WOkfladhV2zTUYwN6LyOQACUwADr8ZRGjkySUcbM1VLIQQ";
        public static string ChangeLanguageSticker = "CAACAgIAAxkBAAEDHxFhcHG_oQwmuwnfp6kWn_CfBpGA8QACdQAD2ZOeGVbYJ6ArCLcyIQQ";
        public static string DevelopWarningSticker = "CAACAgIAAxkBAAEDHxNhcHJP59QL8Fe9GaY3POWBIeII6QACUQADLMqqByX_VpH__oXBIQQ";
        public static string PolishLanguageSetSticker = "CAACAgIAAxkBAAEDHxVhcHU6BuzdT1sw-MZB0uBR35h5iAACKwEAAr8DyQQgsxfQYO--ECEE";
        public static string EnglishLanguageSetSticker = "CAACAgIAAxkBAAEDHxdhcHV4y14-CyrH_D1YujHDCBROUQAC6AADvwPJBGHtqaDNJtEyIQQ";
        public static string RussianLanguageSetSticker = "CAACAgIAAxkBAAEDHxlhcHWCiuvBtQ-IZJknE2hlBlZ-TwAC4gADvwPJBOLja80qqucgIQQ";
        public static string BelarussianLanguageSetSticker = "CAACAgIAAxkBAAEDHxthcHWcDAoUNKey3svQnX0Nr0eVUAAC9wADUomRI6J6Ym0_4ftHIQQ";

        //Cat
        public static string PetCreated_Cat = "CAACAgIAAxkBAAEDHvlhcG2oG4rLAAGPvREkKoykMsNnYzsAAlsQAAKlvUhKsth-8cNoWVghBA";
        public static string PetInfo_Cat = "CAACAgIAAxkBAAEDHwFhcG3C-_owIcuMOR9GTlE4MeoTOAACvRIAAhxUSUo2xUCLEnwQHiEE";
        public static string PetChooseName_Cat = "CAACAgIAAxkBAAEDHwthcG-wxtTfvF_S-6mqam-KwksPnQAC5RAAAowt_QftGb7TeRsiTyEE";
        public static string PetConfirmedName_Cat = "CAACAgIAAxkBAAEDHw1hcHBpvQQti1cmSC1LVKRNOtV3FwACjBIAAtJ0SUqCGw6E9UM1giEE";

        #endregion
        public static ReplyKeyboardMarkup LanguagesMarkup = Extensions.ReplyKeyboardOptimizer(Extensions.LanguagesWithFlags());
    }
}
