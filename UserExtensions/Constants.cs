using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TamagotchiBot.Models;
using Telegram.Bot.Types.ReplyMarkups;

namespace TamagotchiBot.UserExtensions
{
    public static class Constants
    {
        public enum Language
        {
            [Display(ShortName = "🇨🇷", Name = "be")] Belarusian,
            [Display(ShortName = "🇷🇺", Name = "ru")] Russian,
            [Display(ShortName = "🇺🇸", Name = "en")] English,
            [Display(ShortName = "🇵🇱", Name = "pl")] Polish,
        }

        public enum CurrentStatus
        {
            Active,
            Sleeping,
            WorkingOnPC
        }

        public enum Fatigue
        {
            Fresh,
            Rested,
            SlightlyTired,
            Tired,
            Sleepy
        }

        public enum MessageAudience
        {
            Unknown,
            Private,
            Group
        }

        public struct Languages
        {
            public const string LanguageBe = "be";
            public const string LanguagePl = "pl";
            public const string LanguageRu = "ru";
            public const string LanguageEn = "en";
        }

        public struct Factors //per minute
        {
            public const int ExpFactor = 1;
            public const int ExpToLvl = 100;
            public const double StarvingFactor = 0.2;
            public const double FatigueFactor = 0.19;
            public const double RestFactor = 10;
            public const double JoyFactor = 0.3;
            public const double HygieneFactor = 0.11;
            //public const double HygieneFactor = 5;

            public const int CardGameFatigueFactor = 20;
            public const int CardGameJoyFactor = 20;
            public const int DiceGameFatigueFactor = 5;
            public const int DiceGameJoyFactor = 10;

            public const int WorkOnPCFatigueFactor = 70;

            public const int PillHPFactor = 20;
            public const int PillJoyFactor = -10;
        }

        public struct Rewards //in gold
        {
            public const int WorkOnPCGoldReward = 100;
            public const int DailyGoldReward = 100;

            //Referal
            public const int ReferalAdded = 500;
        }

        public struct Costs //in gold
        {
            //Food
            public const int Bread = 50;
            public const int Apple = 1;
            public const int Lollipop = 0;
            public const int Chocolate = 0;

            //Games
            public const int AppleGame = 20;
            public const int DiceGame = 5;

            //Resurrect
            public const int ResurrectPet = 1000;

            //Rename
            public const int RenamePet = 500;
        }

        public class TimesToWait
        {
            public TimeSpan WorkOnPCToWait = new(0, 2, 0);
            public TimeSpan DailyRewardToWait = new(24, 0, 0);
            public TimeSpan SleepToWait = new(0, 10, 0);
        }

        public struct Limits
        {
            public const int ToRestMinLimitOfFatigue = 20;
        }

        public struct FoodFactors
        {
            public const double BreadHungerFactor = 50; //🍞
            public const double RedAppleHungerFactor = 5; //🍎
            public const double ChocolateHungerFactor = 2; //🍫
            public const double LollipopHungerFactor = 1; //🍭
        }

        public struct HygieneFactors
        {
            public const int ShowerFactor = 80;
            public const int TeethFactor = 20;
        }

        public struct Commands
        {
            public const string KitchenCommand = "kitchen";
            public const string PetCommand = "pet";
            public const string LanguageCommand = "language";
            public const string SleepCommand = "sleep";
            public const string GameroomCommand = "gameroom";
            public const string RanksCommand = "ranks";
            public const string RenameCommand = "rename";
            public const string HospitalCommand = "hospital";
            public const string RewardCommand = "reward";
            public const string ReferalCommand = "referal";
            public const string BathroomCommand = "bathroom";
            public const string HelpCommand = "help";
            public const string MenuCommand = "menu";
            public const string WorkCommand = "work";
            public const string QuitCommand = "quit";
            public const string ChangelogCommand = "changelog";
        }

        public struct CommandsMP
        {
            public const string ShowPetCommand = "show_pet";
        }

        public struct StickersId
        {
            //Common
            public const string WelcomeSticker = "CAACAgIAAxkBAAEDHvdhcG0r5WOkfladhV2zTUYwN6LyOQACUwADr8ZRGjkySUcbM1VLIQQ";
            public const string HelpCommandSticker = "CAACAgIAAxkBAAEIEd9kCluWEaE86RH_SAr0tnJcJf_A4AACiXAAAp7OCwAB00mUUVh4ERkvBA";
            public const string ChangelogCommandSticker = "CAACAgIAAxkBAAEKbMBlGKE7cGtOkD35P_1Qf3dm0XVQKQACcR8AAvMoYEpm_8ahsriuMzAE";
            public const string ReferalCommandSticker = "CAACAgIAAxkBAAEIMmVkFxFa_IOB62mjlU6QjY8xAfFC8gACZxcAAqLdcElp3-Tq2zyHiS8E";
            public const string MenuCommandSticker = "CAACAgIAAxkBAAEKaWZlFgnHbAsn58aipIdHeZIzAZz5UQAC9C0AAnsrwEmLPHbDg_W8YTAE";
            public const string ChangeLanguageSticker = "CAACAgIAAxkBAAEDIdRhcygJqmnt4ibdxEVejHOQ4Ya7pwACbAIAAladvQoqGV6cxNDenyEE";
            public const string DevelopWarningSticker = "CAACAgIAAxkBAAEDHxNhcHJP59QL8Fe9GaY3POWBIeII6QACUQADLMqqByX_VpH__oXBIQQ";
            public const string DroppedPetSticker = "CAACAgIAAxkBAAEIDftkCODBW8d3hT4S-iBjBJnpuSbGjwACcBIAAt6p8Et8ICHIsOd3qy4E";
            public const string RenamePetSticker = "CAACAgIAAxkBAAEIDjxkCP5MTi3jeoVyqqptSecoJc0B3AACbRQAAvh48Ev_35tLbqKxRy4E";
            public const string PolishLanguageSetSticker = "CAACAgIAAxkBAAEDHxVhcHU6BuzdT1sw-MZB0uBR35h5iAACKwEAAr8DyQQgsxfQYO--ECEE";
            public const string EnglishLanguageSetSticker = "CAACAgIAAxkBAAEDHxdhcHV4y14-CyrH_D1YujHDCBROUQAC6AADvwPJBGHtqaDNJtEyIQQ";
            public const string RussianLanguageSetSticker = "CAACAgIAAxkBAAEDHxlhcHWCiuvBtQ-IZJknE2hlBlZ-TwAC4gADvwPJBOLja80qqucgIQQ";
            public const string BelarussianLanguageSetSticker = "CAACAgIAAxkBAAEDIdJhcyf3ErjEmUZRgDJgMsCtstPpGAACYQIAAladvQq0dN7WdBr5ViEE";

            public const string ChangelogSticker = "CAACAgIAAxkBAAIoiWQfmY19TqmIZL38KrfWnSS9frV0AAIrKwACnQhYSBduaR-WJLE7LwQ";
            public const string MaintanceProblems = "CAACAgIAAxkBAAEK0sVlY46CnU9WiXGRp7t0MXqbkJhwvwAC6jUAAvnOCElTk5NEHcWxQjME";
            public const string DailyRewardSticker = "CAACAgIAAxkBAAEJz1Fkv8OAYwKlhmZ7CAvYJkg0EQ7Z-wAClhEAAv5tgEmdX9KNHziRpS8E";

            public const string DailyRewardNotificationSticker_1 = "CAACAgIAAxkBAAEJ0C5kwAtlsmG3MWC5dzWl-t4e3YJjvgACbBYAAnwtgEk-JR1KRey6xy8E";
            public const string DailyRewardNotificationSticker_2 = "CAACAgIAAxkBAAEJ0DBkwAuwGXbO_cfYuR1mV7Yfqsd6twACfQADjwveESrxHx6BqwesLwQ";
            public const string DailyRewardNotificationSticker_3 = "CAACAgIAAxkBAAEJ0DJkwAvRzozDjxSn1dbcqprOrrRS1wAC_hYAAuxlgUmSKA23ZDM9xy8E";
            public const string DailyRewardNotificationSticker_4 = "CAACAgIAAxkBAAEJ0DRkwAv9bEVLiNoW7wUCeTmK9L8GDwACVQADkp8eEe9UptCrIZBuLwQ";
            public const string DailyRewardNotificationSticker_5 = "CAACAgIAAxkBAAEJ0DZkwAwQiF4RlJusmqRCnKbA4SH7VwACOAEAAhZ8aAP0b0MaIxsr8S8E";

            public const string RandomEventFriendMet = "CAACAgIAAxkBAAEJ4aNkyVVxEVb4P2gnkHEOk0v8wCmQFAAC8yIAAiBUaEoXiGadMBWCMi8E";
            public const string RandomEventHotdog = "CAACAgIAAxkBAAEJ4aVkyVW0iRMNwELIRJro-sZS-VZ6RQACNiIAAhWCIEtQ3d8mDP4l2S8E";
            public const string RandomEventRainbow = "CAACAgIAAxkBAAEJ4atkyVYlkg62ZFQuhaZmcV80BHO0oAACzF4BAAFji0YMijnpL-ZkBfgvBA";
            public const string RandomEventStepOnFoot = "CAACAgIAAxkBAAEJ4a1kyVY4a-KjYopDd5RsJ8--GavNKgACbh8AAtZFYUroJ9qKMdWRaC8E";
            public const string RandomEventStomachache = "CAACAgIAAxkBAAEJ4a9kyVZprtYWi9S4TB_ulxdAV2rA6gACyyQAAmtxYEqsdF5ojjc4_C8E";
            public const string RandomEventWatermelon = "CAACAgIAAxkBAAEKbMVlGLDT9J9Ql-yGfYpyq5C-P7AgMwAC8iIAAs8xYEq2fxKZoAABMy4wBA";
            public const string RandomEventNiceFlower = "CAACAgIAAxkBAAEKbNBlGLEocjOzKAxImeZMs-6ZFPBMVwAC2h8AArbUIEvaRpkVMvRVkzAE";
            public const string RandomEventPlayComputerGames = "CAACAgIAAxkBAAEKbMdlGLDluFHYtgK0ETXFm_3aV1YDBAACWCMAAlDEYUoc38PCUwS5CDAE";

            public const string BannedSticker = "CAACAgIAAxkBAAEIn9VkPlGMflkimxiV4BhDptaNOBhgjgACmwUAAlOx9wNCvw--ehyldy8E";

            public const string ResurrectedPetSticker = "CAACAgIAAxkBAAEKAqBk2QhbPJzmYZG1tOdSmWvlW5RYNAACpR4AAsL7YUo1ZV8nKeb1XDAE";

            //Cat
            public const string PetCreated_Cat = "CAACAgIAAxkBAAEDHvlhcG2oG4rLAAGPvREkKoykMsNnYzsAAlsQAAKlvUhKsth-8cNoWVghBA";
            public const string PetInfo_Cat = "CAACAgIAAxkBAAEDHwFhcG3C-_owIcuMOR9GTlE4MeoTOAACvRIAAhxUSUo2xUCLEnwQHiEE";
            public const string PetChooseName_Cat = "CAACAgIAAxkBAAEDHwthcG-wxtTfvF_S-6mqam-KwksPnQAC5RAAAowt_QftGb7TeRsiTyEE";
            public const string PetConfirmedName_Cat = "CAACAgIAAxkBAAEDHw1hcHBpvQQti1cmSC1LVKRNOtV3FwACjBIAAtJ0SUqCGw6E9UM1giEE";
            public const string PetAskForConfirmName_Cat = "CAACAgIAAxkBAAEKOGJk9hzg8ZHelRKjXjRGuFAvNp3BOQACVBgAAsk7iUnMmgikdCwdijAE";
            public const string PetKitchen_Cat = "CAACAgIAAxkBAAEDIFVhcfZFjhITgwR6llMbPY-58IL_RAACxA4AA7xBSg8_gz8dIW-OIQQ";
            public const string PetBathroom_Cat = "CAACAgEAAxkBAAEJ5DhkypVqJ21uFEQqFQABvk3K_ykK7PoAAmcAA6EFDA0eRPMjja-FFS8E";
            public const string PetGameroom_Cat = "CAACAgIAAxkBAAEDnIhh1LTJGdhUdSU1y0PFrMmr0wJ3EwAC_RIAAjV1SEq7O0eiJ48IqCME";
            public const string PetSleep_Cat = "CAACAgIAAxkBAAEDuq1h6xbXEQHcyTH6hf6bDcluqK2-bgAC4ScAAvVFSEo8b-MRtutFhiME";
            public const string PetBusy_Cat = "CAACAgIAAxkBAAEDLJJherSnCEKTmK9t5i1x9shxgGVzuwACdBIAAuAOQEqBqm_p74rsAAEhBA";
            public const string PetWork_Cat = "CAACAgIAAxkBAAEIm5BkPBgi8nYhbfCXNX4we5SCqnlT3QAC8RAAAowt_Qf8Tl-qgXK7Oy8E";
            public const string PetRanks_Cat = "CAACAgIAAxkBAAEDuydh6-QrBh7ZWsJ08P5JPbuhEbhIlAAC6hAAAowt_QeFBFvPjWUsjyME";
            public const string PetHospitalLowHP_Cat = "CAACAgIAAxkBAAEIEa1kCkgUfc3lvy1OnyY5LneOAz3tQwAC2hAAAowt_QeJ21KeBteIlS8E";
            public const string PetHospitalMidHP_Cat = "CAACAgIAAxkBAAEIEbFkCkhUqHOSaEfmY85yxF98gaUZhwAC7BAAAowt_QdvxODKmdLpri8E";
            public const string PetHospitalHighHP_Cat = "CAACAgIAAxkBAAEIEbVkCkhxJUXWAkJ0yUyghSK6L2C5kgAC6xAAAowt_QdeNV1SjgQwPi8E";
            public const string PetGone_Cat = "CAACAgIAAxkBAAEINstkGKuoCNpoeRthX9rvkQyYw8aGIQAC2hAAAowt_QeJ21KeBteIlS8E";
            public const string PetBored_Cat = "CAACAgIAAxkBAAEIOhdkGhWlP20cd5VazW0bzgnCFu14TwAC7RAAAowt_Qc5_hbrTG3BAS8E";
            public const string PetEpilogue_Cat = "CAACAgIAAxkBAAEINs1kGKvlnOEEu_6Mk1gDWEiXI2MaDQAC6RAAAowt_QcWUbbRSyZNxS8E";
        }

        public class InlineItems
        {
            public List<CallbackModel> InlineFood = new()
            {
                new CallbackButtons.KitchenCommand().KitchenCommandInlineBread,
                new CallbackButtons.KitchenCommand().KitchenCommandInlineRedApple,
                new CallbackButtons.KitchenCommand().KitchenCommandInlineChocolate,
                new CallbackButtons.KitchenCommand().KitchenCommandInlineLollipop
            };

            public List<CallbackModel> InlineReferal = new()
            {
                new CallbackButtons.ReferalCommand().ToAddToNewGroupReferalCommand,
                new CallbackButtons.ReferalCommand().ToShareReferalCommand
            };

            public List<CallbackModel> InlineGames = new()
            {
                new CallbackButtons.GameroomCommand().GameroomCommandInlineAppleGame,
                new CallbackButtons.GameroomCommand().GameroomCommandInlineDice
            };


            public List<CallbackModel> InlineHygiene = new()
            {
                new CallbackButtons.BathroomCommand().BathroomCommandBrushTeeth,
                new CallbackButtons.BathroomCommand().BathroomCommandTakeShower
            };

            public List<CallbackModel> InlineHospital = new()
            {
                new CallbackButtons.HospitalCommand().HospitalCommandCurePills
            };

            public List<CallbackModel> InlineWork = new()
            {
                new CallbackButtons.WorkCommand().WorkCommandInlineWorkOnPC
            };

            public List<CallbackModel> InlineRanks = new()
            {
                new CallbackButtons.RanksCommand().RanksCommandInlineLevel,
                new CallbackButtons.RanksCommand().RanksCommandInlineGold,
                new CallbackButtons.RanksCommand().RanksCommandInlineApples,
            };

            public List<CallbackModel> InlineRewards = new()
            {
                new CallbackButtons.RewardsCommand().RewardCommandInlineDailyReward
            };

            public List<CallbackModel> InlinePet = new()
            {
                new CallbackButtons.PetCommand().PetCommandInlineExtraInfo
            };

            public List<CallbackModel> InlineShowInviteMP = new()
            {
                new CallbackButtons.PetCommand().PetCommandInlineExtraInfo
            };
        }

        public static ReplyKeyboardMarkup LanguagesMarkup = Extensions.ReplyKeyboardOptimizer(Extensions.LanguagesWithFlags(), isOneTimeKeyboard: true);
    }
}
