using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TamagotchiBot.Models;
using Telegram.Bot.Types.ReplyMarkups;

namespace TamagotchiBot.UserExtensions
{
    public static class Constants
    {
        public enum Language
        {
            [Display(ShortName = "🇵🇱", Name = "pl")] Polish,
            [Display(ShortName = "🇺🇸", Name = "en")] English,
            [Display(ShortName = "🇨🇷", Name = "be")] Belarusian,
            [Display(ShortName = "🇷🇺", Name = "ru")] Russian
        }

        public enum CurrentStatus
        {
            Active,
            Sleeping
        }

        public enum Fatigue
        {
            Fresh,
            Rested,
            SlightlyTired,
            Tired,
            Sleepy
        }

        public struct Languages
        {
            public const string LanguageBe = "be";
            public const string LanguagePl = "pl";
            public const string LanguageRu = "ru";
            public const string LanguageEn = "en";
        }

        public struct Factors
        {
            public const int ExpFactor = 1;
            public const int ExpToLvl = 100;
            public const double StarvingFactor = 0.1;
            public const double FatigueFactor = 0.083;
            public const double RestFactor = 10;
            public const double JoyFactor = 0.3;

            public const int CardGameFatigueFactor = 20;
            public const int CardGameJoyFactor = 20;
            public const int DiceGameFatigueFactor = 5;
            public const int DiceGameJoyFactor = 10;

            public const int PillHPFactor = 20;
            public const int PillJoyFactor = -10;
        }


        public struct Limits
        {
            public static int ToRestMinLimitOfFatigue = 20;
        }

        public struct FoodFactors
        {
            public static double BreadHungerFactor = 10; //🍞
            public static double RedAppleHungerFactor = 5; //🍎
            public static double ChocolateHungerFactor = 4; //🍫
            public static double LollipopHungerFactor = 1; //🍭
        }

        public struct Commands
        {
            public static string KitchenCommand = "kitchen";
            public static string PetCommand = "pet";
            public static string LanguageCommand = "language";
            public static string SleepCommand = "sleep";
            public static string GameroomCommand = "gameroom";
            public static string RanksCommand = "ranks";
            public static string RenameCommand = "rename";
            public static string HospitalCommand = "hospital";
            public static string BathroomCommand = "bathroom";
            public static string HelpCommand = "help";
            public static string MenuCommand = "menu";
            public static string QuitCommand = "quit";
        }

        public struct StickersId
        {
            //Common
            public static string WelcomeSticker = "CAACAgIAAxkBAAEDHvdhcG0r5WOkfladhV2zTUYwN6LyOQACUwADr8ZRGjkySUcbM1VLIQQ";
            public static string HelpCommandSticker = "CAACAgIAAxkBAAEIEd9kCluWEaE86RH_SAr0tnJcJf_A4AACiXAAAp7OCwAB00mUUVh4ERkvBA";
            public static string MenuCommandSticker = "CAACAgIAAxkBAAEIMmVkFxFa_IOB62mjlU6QjY8xAfFC8gACZxcAAqLdcElp3-Tq2zyHiS8E";
            public static string ChangeLanguageSticker = "CAACAgIAAxkBAAEDIdRhcygJqmnt4ibdxEVejHOQ4Ya7pwACbAIAAladvQoqGV6cxNDenyEE";
            public static string DevelopWarningSticker = "CAACAgIAAxkBAAEDHxNhcHJP59QL8Fe9GaY3POWBIeII6QACUQADLMqqByX_VpH__oXBIQQ";
            public static string DroppedPetSticker = "CAACAgIAAxkBAAEIDftkCODBW8d3hT4S-iBjBJnpuSbGjwACcBIAAt6p8Et8ICHIsOd3qy4E";
            public static string RenamePetSticker = "CAACAgIAAxkBAAEIDjxkCP5MTi3jeoVyqqptSecoJc0B3AACbRQAAvh48Ev_35tLbqKxRy4E";
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
            public static string PetGameroom_Cat = "CAACAgIAAxkBAAEDnIhh1LTJGdhUdSU1y0PFrMmr0wJ3EwAC_RIAAjV1SEq7O0eiJ48IqCME";
            public static string PetSleep_Cat = "CAACAgIAAxkBAAEDuq1h6xbXEQHcyTH6hf6bDcluqK2-bgAC4ScAAvVFSEo8b-MRtutFhiME";
            public static string PetBusy_Cat = "CAACAgIAAxkBAAEDLJJherSnCEKTmK9t5i1x9shxgGVzuwACdBIAAuAOQEqBqm_p74rsAAEhBA";
            public static string PetRanks_Cat = "CAACAgIAAxkBAAEDuydh6-QrBh7ZWsJ08P5JPbuhEbhIlAAC6hAAAowt_QeFBFvPjWUsjyME";
            public static string PetHospitalLowHP_Cat = "CAACAgIAAxkBAAEIEa1kCkgUfc3lvy1OnyY5LneOAz3tQwAC2hAAAowt_QeJ21KeBteIlS8E";
            public static string PetHospitalMidHP_Cat = "CAACAgIAAxkBAAEIEbFkCkhUqHOSaEfmY85yxF98gaUZhwAC7BAAAowt_QdvxODKmdLpri8E";
            public static string PetHospitalHighHP_Cat = "CAACAgIAAxkBAAEIEbVkCkhxJUXWAkJ0yUyghSK6L2C5kgAC6xAAAowt_QdeNV1SjgQwPi8E";
            public static string PetGone_Cat = "CAACAgIAAxkBAAEINstkGKuoCNpoeRthX9rvkQyYw8aGIQAC2hAAAowt_QeJ21KeBteIlS8E";
            public static string PetBored_Cat = "CAACAgIAAxkBAAEIOhdkGhWlP20cd5VazW0bzgnCFu14TwAC7RAAAowt_Qc5_hbrTG3BAS8E";
            public static string PetEpilogue_Cat = "CAACAgIAAxkBAAEINs1kGKvlnOEEu_6Mk1gDWEiXI2MaDQAC6RAAAowt_QcWUbbRSyZNxS8E";
        }

        public class InlineItems
        {
            public List<CommandModel> InlineFood = new()
                {
                    new CommandModel ()
                    {
                        Text = "🍞",
                        CallbackData = "kitchenCommandInlineBread"
                    },
                    new CommandModel ()
                    {
                        Text = "🍎",
                        CallbackData = "kitchenCommandInlineRedApple"
                    },
                    new CommandModel ()
                    {
                        Text = "🍫",
                        CallbackData = "kitchenCommandInlineChocolate"
                    },
                    new CommandModel ()
                    {
                        Text = "🍭",
                        CallbackData = "kitchenCommandInlineLollipop"
                    }
                };

            public List<CommandModel> InlineGames = new()
                {
                   new CommandModel()
                   {
                       Text = "🃏",
                       CallbackData = "gameroomCommandInlineCard"
                   },
                   new CommandModel()
                   {
                       Text = "🎲",
                       CallbackData = "gameroomCommandInlineDice"
                   }
                };

            public List<CommandModel> InlineSleep = new()
                {
                    new CommandModel()
                    {
                        Text = Resources.Resources.sleepCommandInlinePutToSleep,
                        CallbackData = "sleepCommandInlinePutToSleep"
                    }
                };

            public List<CommandModel> InlineHospital = new()
                {
                    new CommandModel()
                    {
                        Text = Resources.Resources.hospitalCommandCurePills,
                        CallbackData = "hospitalCommandCurePills"
                    }
                };
            public List<CommandModel> InlinePet = new()
            {
                new CommandModel()
                {
                    Text = Resources.Resources.petCommandInlineExtraInfo,
                    CallbackData = "petCommandInlineExtraInfo"
                }
            };

        }

        public static ReplyKeyboardMarkup LanguagesMarkup = Extensions.ReplyKeyboardOptimizer(Extensions.LanguagesWithFlags(), isOneTimeKeyboard: true);
    }
}
