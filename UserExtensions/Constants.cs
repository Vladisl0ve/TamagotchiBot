using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using TamagotchiBot.Models;
using Telegram.Bot.Types.ReplyMarkups;

namespace TamagotchiBot.UserExtensions
{
    public static class Constants
    {
        public const int QA_MAX_COUNTER = 20;
        public const int QA_TO_FEED_COUNTER = 5;
        public enum Languages
        {
            [Display(ShortName = "🇨🇷", Name = "be")] Belarusian,
            [Display(ShortName = "🇺🇦", Name = "uk")] Ukrainian,
            [Display(ShortName = "🇷🇺", Name = "ru")] Russian,
            [Display(ShortName = "🇺🇸", Name = "en")] English,
            [Display(ShortName = "🇵🇱", Name = "pl")] Polish,
        }

        public enum CurrentStatus
        {
            Active,
            Sleeping,
            Working
        }

        public enum JobType
        {
            None = 0,
            WorkingOnPC = 1,
            FlyersDistributing = 2
        }

        public enum PetType
        {
            UNKNOWN = -1,
            Cat = 0,
            Dog = 1,
            Mouse = 2,
            Fox = 3,
            Panda = 4
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

        public struct Factors //per minute
        {
            public const int ExpFactor = 1;
            public const int ExpToLvl = 50;
            public const double StarvingFactor = 0.1;
            public const double FatigueFactor = 0.19;
            public const double RestFactor = 10;
            public const double JoyFactor = 0.3;
            public const double HygieneFactor = 0.11;

            public const int CardGameFatigueFactor = 20;
            public const int CardGameJoyFactor = 20;
            public const int DiceGameFatigueFactor = 5;
            public const int DiceGameJoyFactor = 10;
            public const int TicTacToeGameFatigueFactor = 5;
            public const int TicTacToeGameJoyFactor = 20;
            public const int HangmanGameFatigueFactor = 5;
            public const int HangmanGameJoyFactor = 20;

            public const int WorkOnPCFatigueFactor = 50;
            public const int FlyersDistributingFatigueFactor = 20;

            public const int PillHPFactor = 20;
            public const int PillJoyFactor = -10;
        }

        public struct Rewards //in gold
        {
            public const int WorkOnPCGoldReward = 100;
            public const int FlyersDistributingGoldReward = 40;
            public const int DailyGoldReward = 250;

            //Referal
            public const int ReferalAdded = 1000;

            //Multiplayer
            public const int WonDuel = 150;
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
            public const int TicTacToeGame = 25;
            public const int HangmanGame = 40;

            //Resurrect
            public const int ResurrectPet = 1000;

            //Change type
            public const int ChangePetType = 5000;

            //Rename
            public const int RenamePet = 500;

            //Multiplayer
            public const int DuelGold = 100;
            public const int DuelHP = 100; //HP limit?
            public const int FeedMP = 20;

            //AutoFeed
            public const int AutoFeedCost = 500;
            public const int AutoFeedCostDiamonds = 100;
        }

        public struct AutoFeed
        {
            public const int AutoFeedAmount = 100;
            public const int AutoFeedChargesInitial = 5;
        }

        public struct CronSchedule
        {
            public const string AutoFeedCron = "0 0 0/4 * * ?";
            //public const string AutoFeedCron = "0 * * * * ?"; //DEBUG
        }

        public static class TimesToWait
        {
            public readonly static TimeSpan WorkOnPCToWait = new(0, 2, 0);
            public readonly static TimeSpan FlyersDistToWait = new(0, 0, 30);
            public readonly static TimeSpan DailyRewardToWait = new(24, 0, 0);
            public readonly static TimeSpan SleepToWait = new(0, 10, 0);
            public readonly static TimeSpan DuelCDToWait = new(0, 5, 0);
            public readonly static TimeSpan FeedMPCDToWait = new(6, 0, 0);

            public readonly static TimeSpan CooldownOnMessage = new TimeSpan(0, 0, 0, 0, 500);
            public readonly static TimeSpan OldMessageDelta = new TimeSpan(0, 0, 30);
            public readonly static TimeSpan GeminiTimeout = new TimeSpan(0, 30, 0);
        }

        public struct Limits
        {
            public const int ToRestMinLimitOfFatigue = 20;
        }

        public struct GoldForTopExpRanking
        {
            public const int Top1 = 1000;
            public const int Top2 = 500;
            public const int Top3 = 300;
            public const int Top4_10 = 100;
        }

        public struct ExpForAction
        {
            public const int FeedingBread = 20;
            public const int FeedingApple = 20;

            public const int PlayApple = 20;
            public const int Play = 5;

            public const int Hygiene = 10;

            public const int Sleep = 30;

            public const int WorkPC = 25;
            public const int WorkFlyers = 10;
        }

        public struct FoodFactors
        {
            public const double BreadHungerFactor = 50; //🍞
            public const double RedAppleHungerFactor = 5; //🍎
            public const double ChocolateHungerFactor = 2; //🍫
            public const double LollipopHungerFactor = 1; //🍭

            //Multiplayer
            public const int MPFeedFactor = 100; //🍭
        }

        public struct HygieneFactors
        {
            public const int ShowerFactor = 80;
            public const int TeethFactor = 20;
            public const int PoopFactor = 20;
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
            public const string FarmCommand = "farm";
            public const string BathroomCommand = "bathroom";
            public const string HelpCommand = "help";
            public const string MenuCommand = "menu";
            public const string WorkCommand = "work";
            public const string QuitCommand = "quit";
            public const string ChangelogCommand = "changelog";

            public const string GeminiCommand = "gemini";

            //ADMIN
            public const string CheckCommand = "check";
            public const string KillCommand = "kill";
            public const string RestartCommand = "restart";
            public const string GoldCommand = "gold";
            public const string StartBotstatCheckCommand = "start_botstat_check";
            public const string StatusBotstatCheckCommand = "status_botstat_check";
        }

        public struct CommandsMP
        {
            public const string ShowPetCommand = "show_pet";
            public const string StartDuelCommand = "start_duel";
            public const string FeedMPCommand = "feed_pet";
            public const string ShowChatRanksMPCommand = "ranks";
        }

        public struct StickersId
        {
            //Common
            public const string WelcomeSticker = "CAACAgIAAxkBAAEDHvdhcG0r5WOkfladhV2zTUYwN6LyOQACUwADr8ZRGjkySUcbM1VLIQQ";
            public const string FarmSticker = "CAACAgIAAxkBAAELURxlv-IkydD8-3BK8XslqNTCAc-7PwACAhEAAjSLWwYP0_s-zhcRSzQE";
            public const string HelpCommandSticker = "CAACAgIAAxkBAAEIEd9kCluWEaE86RH_SAr0tnJcJf_A4AACiXAAAp7OCwAB00mUUVh4ERkvBA";
            public const string ChangelogCommandSticker = "CAACAgIAAxkBAAEKbMBlGKE7cGtOkD35P_1Qf3dm0XVQKQACcR8AAvMoYEpm_8ahsriuMzAE";
            public const string ReferalCommandSticker = "CAACAgIAAxkBAAEIMmVkFxFa_IOB62mjlU6QjY8xAfFC8gACZxcAAqLdcElp3-Tq2zyHiS8E";
            public const string MenuCommandSticker = "CAACAgIAAxkBAAEKaWZlFgnHbAsn58aipIdHeZIzAZz5UQAC9C0AAnsrwEmLPHbDg_W8YTAE";
            public const string ChangeLanguageSticker = "CAACAgIAAxkBAAEDIdRhcygJqmnt4ibdxEVejHOQ4Ya7pwACbAIAAladvQoqGV6cxNDenyEE";
            public const string DevelopWarningSticker = "CAACAgIAAxkBAAEDHxNhcHJP59QL8Fe9GaY3POWBIeII6QACUQADLMqqByX_VpH__oXBIQQ";
            public const string DroppedPetSticker = "CAACAgIAAxkBAAEIDftkCODBW8d3hT4S-iBjBJnpuSbGjwACcBIAAt6p8Et8ICHIsOd3qy4E";
            public const string RenamePetSticker = "CAACAgIAAxkBAAEIDjxkCP5MTi3jeoVyqqptSecoJc0B3AACbRQAAvh48Ev_35tLbqKxRy4E";
            public const string PetDoesntLikeNameSticker = "CAACAgEAAxkBAAEKzlNlX6I8gelMuHkWo5lf5lJ4GFkvhQACiAIAAoVVWEYa_0H43ss_kTME";
            public const string PolishLanguageSetSticker = "CAACAgIAAxkBAAEDHxVhcHU6BuzdT1sw-MZB0uBR35h5iAACKwEAAr8DyQQgsxfQYO--ECEE";
            public const string EnglishLanguageSetSticker = "CAACAgIAAxkBAAEDHxdhcHV4y14-CyrH_D1YujHDCBROUQAC6AADvwPJBGHtqaDNJtEyIQQ";
            public const string RussianLanguageSetSticker = "CAACAgIAAxkBAAEDHxlhcHWCiuvBtQ-IZJknE2hlBlZ-TwAC4gADvwPJBOLja80qqucgIQQ";
            public const string UkrainianLanguageSetSticker = "CAACAgIAAxkBAAELj5Zl3OLG_wbkg3_IB0pPTym49SpAxwACCgAD3pSWESEndgrQxfhnNAQ";
            public const string BelarussianLanguageSetSticker = "CAACAgIAAxkBAAEDIdJhcyf3ErjEmUZRgDJgMsCtstPpGAACYQIAAladvQq0dN7WdBr5ViEE";

            public const string ChangeTypePetSticker = "CAACAgIAAxkBAAELUTFlv-y-GF6diGzs6trFkSR13nqCbwAC-RAAAjSLWwaVH1nmaD8TjDQE";
            public const string ChangeTypeErrorSticker = "CAACAgIAAxkBAAELUzVlwUL-5fyYwZJCGyjXYWDqDMPX3gACrUUAAgsn4En6llfN6C4pCjQE";
            public const string ChangeTypeErrorGoldSticker = "CAACAgIAAxkBAAELUzdlwUQlfiUpuAPpKwejhj8JUrtamAAC6z4AAr4b2UkCVdfVjLsgnDQE";

            //public const string ChangelogSticker = "CAACAgIAAxkBAAIoiWQfmY19TqmIZL38KrfWnSS9frV0AAIrKwACnQhYSBduaR-WJLE7LwQ"; //usual changelog panda jumps
            public const string ChangelogSticker = "CAACAgIAAxkBAAELb2Rl0nuZd49DEzvd2J9VRgABL_CKEvQAAlwCAAKQWXYt3NXqzbbMLvU0BA"; //referal notify ring

            public const string MaintanceProblems = "CAACAgIAAxkBAAEK0sVlY46CnU9WiXGRp7t0MXqbkJhwvwAC6jUAAvnOCElTk5NEHcWxQjME";
            public const string DailyRewardSticker = "CAACAgIAAxkBAAEJz1Fkv8OAYwKlhmZ7CAvYJkg0EQ7Z-wAClhEAAv5tgEmdX9KNHziRpS8E";
            public const string MonthlyRewardSticker = "CAACAgIAAxkBAAEPlMNo8OMqNkqUW5f3hq8MbfAM9J66hQACtUMAAuWBYUjP53LZfBfr2TYE";

            public const string DailyRewardNotificationSticker_1 = "CAACAgIAAxkBAAEJ0C5kwAtlsmG3MWC5dzWl-t4e3YJjvgACbBYAAnwtgEk-JR1KRey6xy8E";
            public const string DailyRewardNotificationSticker_2 = "CAACAgIAAxkBAAEJ0DBkwAuwGXbO_cfYuR1mV7Yfqsd6twACfQADjwveESrxHx6BqwesLwQ";
            public const string DailyRewardNotificationSticker_3 = "CAACAgIAAxkBAAEJ0DJkwAvRzozDjxSn1dbcqprOrrRS1wAC_hYAAuxlgUmSKA23ZDM9xy8E";
            public const string DailyRewardNotificationSticker_4 = "CAACAgIAAxkBAAEJ0DRkwAv9bEVLiNoW7wUCeTmK9L8GDwACVQADkp8eEe9UptCrIZBuLwQ";
            public const string DailyRewardNotificationSticker_5 = "CAACAgIAAxkBAAEJ0DZkwAwQiF4RlJusmqRCnKbA4SH7VwACOAEAAhZ8aAP0b0MaIxsr8S8E";

            public const string RandomEventFriendMet = "CAACAgIAAxkBAAEJ4aNkyVVxEVb4P2gnkHEOk0v8wCmQFAAC8yIAAiBUaEoXiGadMBWCMi8E";
            public const string RandomEventHotdog = "CAACAgIAAxkBAAEJ4aVkyVW0iRMNwELIRJro-sZS-VZ6RQACNiIAAhWCIEtQ3d8mDP4l2S8E";
            public const string RandomEventRainbow = "CAACAgIAAxkBAAEJ4atkyVYlkg62ZFQuhaZmcV80BHO0oAACzF4BAAFji0YMijnpL-ZkBfgvBA";
            public const string RandomEventStomachache = "CAACAgIAAxkBAAEJ4a9kyVZprtYWi9S4TB_ulxdAV2rA6gACyyQAAmtxYEqsdF5ojjc4_C8E";
            public const string RandomEventWatermelon = "CAACAgIAAxkBAAEKbMVlGLDT9J9Ql-yGfYpyq5C-P7AgMwAC8iIAAs8xYEq2fxKZoAABMy4wBA";
            public const string RandomEventNiceFlower = "CAACAgIAAxkBAAEKbNBlGLEocjOzKAxImeZMs-6ZFPBMVwAC2h8AArbUIEvaRpkVMvRVkzAE";

            public const string BannedSticker = "CAACAgIAAxkBAAEIn9VkPlGMflkimxiV4BhDptaNOBhgjgACmwUAAlOx9wNCvw--ehyldy8E";

            public const string FlyerADSSticker = "CAACAgIAAxkBAAELWDFlw-nwTUsoBQJykSjLmv8aNpq3vgACKzMAArZHcEpkCdiTnaUfWjQE";

            public const string PetAutoFeederUsedSticker = "CAACAgUAAxkBAAEP8_FpNXhcKT04cEL1uKLU5qxFqUSQ8QAChwQAAs9WWFWOxvbq2XR8uTYE";

            public const string MPDuelStarted = "CAACAgIAAxkBAAEK0n9lY2dwZDww-D6OZW-5aD4SdN8BcgACGCUAAvYaiEvT6OrPxdApCzME";

            public static string GetStickerByType(string stickerName, int? petType)
                => GetStickerByType(stickerName, Extensions.GetEnumPetType(petType));
            public static string GetStickerByType(string stickerName, PetType petType)
            {
                string postfix = "_";
                stickerName = stickerName.Split("_").FirstOrDefault();

                if (stickerName == null)
                {
                    Log.Fatal($"ERROR IN GetStickerByType: can not find sticker with name {stickerName}");
                    return WelcomeSticker;
                }

                postfix += petType switch
                {
                    PetType.Cat => nameof(PetType.Cat),
                    PetType.Dog => nameof(PetType.Dog),
                    PetType.Fox => nameof(PetType.Fox),
                    PetType.Panda => nameof(PetType.Panda),
                    PetType.Mouse => nameof(PetType.Mouse),
                    _ => nameof(PetType.UNKNOWN)
                };

                if (PetDependedStickers.Contains(stickerName + postfix))
                    return PetDependedStickers[stickerName + postfix].Single();
                else
                {
                    Log.Fatal($"ERROR IN GetStickerByType: sticker with name {stickerName} is not in depended stickers fot petType [{petType}:{(int)petType}]");
                    return WelcomeSticker;
                }
            }

            public static readonly ILookup<string, string> PetDependedStickers = new Dictionary<string, string>()
            {
                //CAT
                {nameof(PetCreatedSticker_Cat), PetCreatedSticker_Cat },
                {nameof(PetInfoSticker_Cat), PetInfoSticker_Cat },
                {nameof(PetChooseNameSticker_Cat), PetChooseNameSticker_Cat },
                {nameof(PetConfirmedNameSticker_Cat), PetConfirmedNameSticker_Cat },
                {nameof(PetAskForConfirmNameSticker_Cat), PetAskForConfirmNameSticker_Cat },
                {nameof(PetKitchenSticker_Cat), PetKitchenSticker_Cat },
                {nameof(PetBathroomSticker_Cat), PetBathroomSticker_Cat },
                {nameof(PetGameroomSticker_Cat), PetGameroomSticker_Cat },
                {nameof(PetSleepSticker_Cat), PetSleepSticker_Cat },
                {nameof(PetBusySticker_Cat), PetBusySticker_Cat },
                {nameof(PetWorkSticker_Cat), PetWorkSticker_Cat },
                {nameof(PetWorkOnPCSticker_Cat), PetWorkOnPCSticker_Cat },
                {nameof(PetFlyersJobSticker_Cat), PetFlyersJobSticker_Cat },
                {nameof(PetRanksSticker_Cat), PetRanksSticker_Cat },
                {nameof(PetHospitalLowHPSticker_Cat), PetHospitalLowHPSticker_Cat },
                {nameof(PetHospitalMidHPSticker_Cat), PetHospitalMidHPSticker_Cat },
                {nameof(PetHospitalHighHPSticker_Cat), PetHospitalHighHPSticker_Cat },
                {nameof(PetGoneSticker_Cat), PetGoneSticker_Cat },
                {nameof(PetBoredSticker_Cat), PetBoredSticker_Cat },
                {nameof(PetEpilogueSticker_Cat), PetEpilogueSticker_Cat },
                {nameof(PetChangeTypeSticker_Cat), PetChangeTypeSticker_Cat },
                {nameof(PetResurrectedSticker_Cat), PetResurrectedSticker_Cat },
                {nameof(PetDailyRewardSticker_Cat), PetDailyRewardSticker_Cat },
                {nameof(RandomEventStepOnFootSticker_Cat), RandomEventStepOnFootSticker_Cat },
                {nameof(RandomEventPlayComputerSticker_Cat), RandomEventPlayComputerSticker_Cat },

                //DOG
                {nameof(PetCreatedSticker_Dog), PetCreatedSticker_Dog },
                {nameof(PetInfoSticker_Dog), PetInfoSticker_Dog },
                {nameof(PetChooseNameSticker_Dog), PetChooseNameSticker_Dog },
                {nameof(PetConfirmedNameSticker_Dog), PetConfirmedNameSticker_Dog },
                {nameof(PetAskForConfirmNameSticker_Dog), PetAskForConfirmNameSticker_Dog },
                {nameof(PetKitchenSticker_Dog), PetKitchenSticker_Dog },
                {nameof(PetBathroomSticker_Dog), PetBathroomSticker_Dog },
                {nameof(PetGameroomSticker_Dog), PetGameroomSticker_Dog },
                {nameof(PetSleepSticker_Dog), PetSleepSticker_Dog },
                {nameof(PetBusySticker_Dog), PetBusySticker_Dog },
                {nameof(PetWorkSticker_Dog), PetWorkSticker_Dog },
                {nameof(PetWorkOnPCSticker_Dog), PetWorkOnPCSticker_Dog },
                {nameof(PetFlyersJobSticker_Dog), PetFlyersJobSticker_Dog },
                {nameof(PetRanksSticker_Dog), PetRanksSticker_Dog },
                {nameof(PetHospitalLowHPSticker_Dog), PetHospitalLowHPSticker_Dog },
                {nameof(PetHospitalMidHPSticker_Dog), PetHospitalMidHPSticker_Dog },
                {nameof(PetHospitalHighHPSticker_Dog), PetHospitalHighHPSticker_Dog },
                {nameof(PetGoneSticker_Dog), PetGoneSticker_Dog },
                {nameof(PetBoredSticker_Dog), PetBoredSticker_Dog },
                {nameof(PetEpilogueSticker_Dog), PetEpilogueSticker_Dog },
                {nameof(PetChangeTypeSticker_Dog), PetChangeTypeSticker_Dog },
                {nameof(PetResurrectedSticker_Dog), PetResurrectedSticker_Dog },
                {nameof(PetDailyRewardSticker_Dog), PetDailyRewardSticker_Dog },
                {nameof(RandomEventStepOnFootSticker_Dog), RandomEventStepOnFootSticker_Dog },
                {nameof(RandomEventPlayComputerSticker_Dog), RandomEventPlayComputerSticker_Dog },

                //FOX
                {nameof(PetCreatedSticker_Fox), PetCreatedSticker_Fox },
                {nameof(PetInfoSticker_Fox), PetInfoSticker_Fox },
                {nameof(PetChooseNameSticker_Fox), PetChooseNameSticker_Fox },
                {nameof(PetConfirmedNameSticker_Fox), PetConfirmedNameSticker_Fox },
                {nameof(PetAskForConfirmNameSticker_Fox), PetAskForConfirmNameSticker_Fox },
                {nameof(PetKitchenSticker_Fox), PetKitchenSticker_Fox },
                {nameof(PetBathroomSticker_Fox), PetBathroomSticker_Fox },
                {nameof(PetGameroomSticker_Fox), PetGameroomSticker_Fox },
                {nameof(PetSleepSticker_Fox), PetSleepSticker_Fox },
                {nameof(PetBusySticker_Fox), PetBusySticker_Fox },
                {nameof(PetWorkSticker_Fox), PetWorkSticker_Fox },
                {nameof(PetWorkOnPCSticker_Fox), PetWorkOnPCSticker_Fox },
                {nameof(PetFlyersJobSticker_Fox), PetFlyersJobSticker_Fox },
                {nameof(PetRanksSticker_Fox), PetRanksSticker_Fox },
                {nameof(PetHospitalLowHPSticker_Fox), PetHospitalLowHPSticker_Fox },
                {nameof(PetHospitalMidHPSticker_Fox), PetHospitalMidHPSticker_Fox },
                {nameof(PetHospitalHighHPSticker_Fox), PetHospitalHighHPSticker_Fox },
                {nameof(PetGoneSticker_Fox), PetGoneSticker_Fox },
                {nameof(PetBoredSticker_Fox), PetBoredSticker_Fox },
                {nameof(PetEpilogueSticker_Fox), PetEpilogueSticker_Fox },
                {nameof(PetChangeTypeSticker_Fox), PetChangeTypeSticker_Fox },
                {nameof(PetResurrectedSticker_Fox), PetResurrectedSticker_Fox },
                {nameof(PetDailyRewardSticker_Fox), PetDailyRewardSticker_Fox },
                {nameof(RandomEventStepOnFootSticker_Fox), RandomEventStepOnFootSticker_Fox },
                {nameof(RandomEventPlayComputerSticker_Fox), RandomEventPlayComputerSticker_Fox },

                //PANDA                
                {nameof(PetCreatedSticker_Panda), PetCreatedSticker_Panda },
                {nameof(PetInfoSticker_Panda), PetInfoSticker_Panda },
                {nameof(PetChooseNameSticker_Panda), PetChooseNameSticker_Panda },
                {nameof(PetConfirmedNameSticker_Panda), PetConfirmedNameSticker_Panda },
                {nameof(PetAskForConfirmNameSticker_Panda), PetAskForConfirmNameSticker_Panda },
                {nameof(PetKitchenSticker_Panda), PetKitchenSticker_Panda },
                {nameof(PetBathroomSticker_Panda), PetBathroomSticker_Panda },
                {nameof(PetGameroomSticker_Panda), PetGameroomSticker_Panda },
                {nameof(PetSleepSticker_Panda), PetSleepSticker_Panda },
                {nameof(PetBusySticker_Panda), PetBusySticker_Panda },
                {nameof(PetWorkSticker_Panda), PetWorkSticker_Panda },
                {nameof(PetWorkOnPCSticker_Panda), PetWorkOnPCSticker_Panda },
                {nameof(PetFlyersJobSticker_Panda), PetFlyersJobSticker_Panda },
                {nameof(PetRanksSticker_Panda), PetRanksSticker_Panda },
                {nameof(PetHospitalLowHPSticker_Panda), PetHospitalLowHPSticker_Panda },
                {nameof(PetHospitalMidHPSticker_Panda), PetHospitalMidHPSticker_Panda },
                {nameof(PetHospitalHighHPSticker_Panda), PetHospitalHighHPSticker_Panda },
                {nameof(PetGoneSticker_Panda), PetGoneSticker_Panda },
                {nameof(PetBoredSticker_Panda), PetBoredSticker_Panda },
                {nameof(PetEpilogueSticker_Panda), PetEpilogueSticker_Panda },
                {nameof(PetChangeTypeSticker_Panda), PetChangeTypeSticker_Panda },
                {nameof(PetResurrectedSticker_Panda), PetResurrectedSticker_Panda },
                {nameof(PetDailyRewardSticker_Panda), PetDailyRewardSticker_Panda },
                {nameof(RandomEventStepOnFootSticker_Panda), RandomEventStepOnFootSticker_Panda },
                {nameof(RandomEventPlayComputerSticker_Panda), RandomEventPlayComputerSticker_Panda },
                
                //MOUSE
                {nameof(PetCreatedSticker_Mouse), PetCreatedSticker_Mouse },
                {nameof(PetInfoSticker_Mouse), PetInfoSticker_Mouse },
                {nameof(PetChooseNameSticker_Mouse), PetChooseNameSticker_Mouse },
                {nameof(PetConfirmedNameSticker_Mouse), PetConfirmedNameSticker_Mouse },
                {nameof(PetAskForConfirmNameSticker_Mouse), PetAskForConfirmNameSticker_Mouse },
                {nameof(PetKitchenSticker_Mouse), PetKitchenSticker_Mouse },
                {nameof(PetBathroomSticker_Mouse), PetBathroomSticker_Mouse },
                {nameof(PetGameroomSticker_Mouse), PetGameroomSticker_Mouse },
                {nameof(PetSleepSticker_Mouse), PetSleepSticker_Mouse },
                {nameof(PetBusySticker_Mouse), PetBusySticker_Mouse },
                {nameof(PetWorkSticker_Mouse), PetWorkSticker_Mouse },
                {nameof(PetWorkOnPCSticker_Mouse), PetWorkOnPCSticker_Mouse },
                {nameof(PetFlyersJobSticker_Mouse), PetFlyersJobSticker_Mouse },
                {nameof(PetRanksSticker_Mouse), PetRanksSticker_Mouse },
                {nameof(PetHospitalLowHPSticker_Mouse), PetHospitalLowHPSticker_Mouse },
                {nameof(PetHospitalMidHPSticker_Mouse), PetHospitalMidHPSticker_Mouse },
                {nameof(PetHospitalHighHPSticker_Mouse), PetHospitalHighHPSticker_Mouse },
                {nameof(PetGoneSticker_Mouse), PetGoneSticker_Mouse },
                {nameof(PetBoredSticker_Mouse), PetBoredSticker_Mouse },
                {nameof(PetEpilogueSticker_Mouse), PetEpilogueSticker_Mouse },
                {nameof(PetChangeTypeSticker_Mouse), PetChangeTypeSticker_Mouse },
                {nameof(PetResurrectedSticker_Mouse), PetResurrectedSticker_Mouse },
                {nameof(PetDailyRewardSticker_Mouse), PetDailyRewardSticker_Mouse },
                {nameof(RandomEventStepOnFootSticker_Mouse), RandomEventStepOnFootSticker_Mouse },
                {nameof(RandomEventPlayComputerSticker_Mouse), RandomEventPlayComputerSticker_Mouse },

            }.ToLookup(x => x.Key, x => x.Value);

            #region Cat
            public const string PetCreatedSticker_Cat = "CAACAgIAAxkBAAEDHvlhcG2oG4rLAAGPvREkKoykMsNnYzsAAlsQAAKlvUhKsth-8cNoWVghBA";
            public const string PetInfoSticker_Cat = "CAACAgIAAxkBAAEDHwFhcG3C-_owIcuMOR9GTlE4MeoTOAACvRIAAhxUSUo2xUCLEnwQHiEE";
            public const string PetChooseNameSticker_Cat = "CAACAgIAAxkBAAEDHwthcG-wxtTfvF_S-6mqam-KwksPnQAC5RAAAowt_QftGb7TeRsiTyEE";
            public const string PetConfirmedNameSticker_Cat = "CAACAgIAAxkBAAEDHw1hcHBpvQQti1cmSC1LVKRNOtV3FwACjBIAAtJ0SUqCGw6E9UM1giEE";
            public const string PetAskForConfirmNameSticker_Cat = "CAACAgIAAxkBAAEKOGJk9hzg8ZHelRKjXjRGuFAvNp3BOQACVBgAAsk7iUnMmgikdCwdijAE";
            public const string PetKitchenSticker_Cat = "CAACAgIAAxkBAAEDIFVhcfZFjhITgwR6llMbPY-58IL_RAACxA4AA7xBSg8_gz8dIW-OIQQ";
            public const string PetBathroomSticker_Cat = "CAACAgEAAxkBAAEJ5DhkypVqJ21uFEQqFQABvk3K_ykK7PoAAmcAA6EFDA0eRPMjja-FFS8E";
            public const string PetGameroomSticker_Cat = "CAACAgIAAxkBAAEDnIhh1LTJGdhUdSU1y0PFrMmr0wJ3EwAC_RIAAjV1SEq7O0eiJ48IqCME";
            public const string PetSleepSticker_Cat = "CAACAgIAAxkBAAEDuq1h6xbXEQHcyTH6hf6bDcluqK2-bgAC4ScAAvVFSEo8b-MRtutFhiME";
            public const string PetBusySticker_Cat = "CAACAgIAAxkBAAEDLJJherSnCEKTmK9t5i1x9shxgGVzuwACdBIAAuAOQEqBqm_p74rsAAEhBA";
            public const string PetWorkSticker_Cat = "CAACAgIAAxkBAAEK1yBlZlxHrCC3KI92GdDnsv7ml2BpyAACbxgAAh1UyUtNxQKfdLqZ7zME";
            public const string PetWorkOnPCSticker_Cat = "CAACAgIAAxkBAAEK1xxlZlWln8UKh7QT3VELzQPUB3ORyQAC9RgAAk91yUtrMUFsJhd4XjME";
            public const string PetFlyersJobSticker_Cat = "CAACAgIAAxkBAAEK1x5lZlXZ6mDQC3ZYi6TeTiijYz-fFgACbhgAAsUnyUsEceKwVvyxWjME";
            public const string PetRanksSticker_Cat = "CAACAgIAAxkBAAEDuydh6-QrBh7ZWsJ08P5JPbuhEbhIlAAC6hAAAowt_QeFBFvPjWUsjyME";
            public const string PetHospitalLowHPSticker_Cat = "CAACAgIAAxkBAAEIEa1kCkgUfc3lvy1OnyY5LneOAz3tQwAC2hAAAowt_QeJ21KeBteIlS8E";
            public const string PetHospitalMidHPSticker_Cat = "CAACAgIAAxkBAAEIEbFkCkhUqHOSaEfmY85yxF98gaUZhwAC7BAAAowt_QdvxODKmdLpri8E";
            public const string PetHospitalHighHPSticker_Cat = "CAACAgIAAxkBAAEIEbVkCkhxJUXWAkJ0yUyghSK6L2C5kgAC6xAAAowt_QdeNV1SjgQwPi8E";
            public const string PetGoneSticker_Cat = "CAACAgIAAxkBAAEINstkGKuoCNpoeRthX9rvkQyYw8aGIQAC2hAAAowt_QeJ21KeBteIlS8E";
            public const string PetBoredSticker_Cat = "CAACAgIAAxkBAAEIOhdkGhWlP20cd5VazW0bzgnCFu14TwAC7RAAAowt_Qc5_hbrTG3BAS8E";
            public const string PetEpilogueSticker_Cat = "CAACAgIAAxkBAAEINs1kGKvlnOEEu_6Mk1gDWEiXI2MaDQAC6RAAAowt_QcWUbbRSyZNxS8E";
            public const string PetChangeTypeSticker_Cat = "CAACAgIAAxkBAAELUR5lv-nhjujL-EjpGugIQQY7qeUQTgACSUMAAges2EllTD73Xi5RejQE";
            public const string PetResurrectedSticker_Cat = "CAACAgIAAxkBAAEKAqBk2QhbPJzmYZG1tOdSmWvlW5RYNAACpR4AAsL7YUo1ZV8nKeb1XDAE";
            public const string PetDailyRewardSticker_Cat = "CAACAgIAAxkBAAELWBBlw91kJ672n4H0GYXs1y4FCI4BOwACjEQAAuCL2Ek0FIIAAXscqIw0BA";
            public const string RandomEventStepOnFootSticker_Cat = "CAACAgIAAxkBAAEJ4a1kyVY4a-KjYopDd5RsJ8--GavNKgACbh8AAtZFYUroJ9qKMdWRaC8E";
            public const string RandomEventPlayComputerSticker_Cat = "CAACAgIAAxkBAAEKbMdlGLDluFHYtgK0ETXFm_3aV1YDBAACWCMAAlDEYUoc38PCUwS5CDAE";

            #endregion
            #region Dog
            public const string PetCreatedSticker_Dog = "CAACAgIAAxkBAAELVudlw8RIbryAKKzBCZ9WzlPcQMN4VQAC-TYAAlPFkEvv4r2vj754UzQE";
            public const string PetInfoSticker_Dog = "CAACAgIAAxkBAAELVwRlw8tSloMuB5MO4hq8lEa7LjHCWAACcAkAAhhC7ggNwYQs9skKgzQE";
            public const string PetChooseNameSticker_Dog = "CAACAgIAAxkBAAELVvplw8pKDRQZO_D0ox1TRleEa4bWiAACZQkAAhhC7ghRNmsqi1F8QDQE";
            public const string PetConfirmedNameSticker_Dog = "CAACAgIAAxkBAAELVv5lw8pwBXF2_87ZfQxGe2FF_GKgYAACXAkAAhhC7gh5RWRhM3yStDQE";
            public const string PetAskForConfirmNameSticker_Dog = "CAACAgIAAxkBAAELVvxlw8pVoqK3hN6r0QqVg-U1d-3fNgACYAkAAhhC7ggkkrp5Mwb6BzQE";
            public const string PetKitchenSticker_Dog = "CAACAgIAAxkBAAELVwABZcPKg0SfCCL_R8Kx8EnXdvjMDf0AAqNGAAJQEPBJ3uLhUuCtoq80BA";
            public const string PetBathroomSticker_Dog = "CAACAgIAAxkBAAELVwJlw8svUIyx1KJKJu6NsUg7sX4_5AAC4QQAAs9fiwfcm2Fsk5If5DQE";
            public const string PetGameroomSticker_Dog = "CAACAgIAAxkBAAELVvhlw8ojzUWZHG5zjYLBGzfntutBRwACWAkAAhhC7ghdxPPSm9_SQjQE";
            public const string PetSleepSticker_Dog = "CAACAgIAAxkBAAELVwZlw8uNU5dDaKNrrLa0LDzeV9Zo2QACbAkAAhhC7ggQ76mmibN2TDQE";
            public const string PetBusySticker_Dog = "CAACAgIAAxkBAAELVwhlw8uou0b3lQSzISCfI0TIvbsVgwACZgkAAhhC7gg-JYyYtmdxtTQE";
            public const string PetWorkSticker_Dog = "CAACAgIAAxkBAAELVwplw8u40zElGtBJV-cJrbPS9rn6VgACaQkAAhhC7gia--ItXVObQjQE";
            public const string PetWorkOnPCSticker_Dog = "CAACAgIAAxkBAAELVwxlw8vPpPt2UoCdoyXw1moOX-y8hwAC1QQAAs9fiwesDLS1kbvcOzQE";
            public const string PetFlyersJobSticker_Dog = "CAACAgIAAxkBAAELVxNlw8vezGQ_b-oatdxGUuBGaOELjQAC8AQAAs9fiweXrmTGtaCl9DQE";
            public const string PetRanksSticker_Dog = "CAACAgIAAxkBAAELVxVlw8vzncNi6KBh17QNmMAoVnhJvQACXgkAAhhC7ggHlhjlQIXYVTQE";
            public const string PetHospitalLowHPSticker_Dog = "CAACAgIAAxkBAAELVxdlw8wEokh8Iv_ojDPLRdyeVPNjaAACaAkAAhhC7ghoeeMl5GfTeDQE";
            public const string PetHospitalMidHPSticker_Dog = "CAACAgIAAxkBAAELVx1lw8w02bLj_IjyPRrrQcVmFEoOaAACbgkAAhhC7gjgnqdbZMkyTTQE";
            public const string PetHospitalHighHPSticker_Dog = "CAACAgIAAxkBAAELVxtlw8wfcUtiRJ-WbVAfeOEQJPVeTgACXwkAAhhC7ghT5M1UMfGUOjQE";
            public const string PetGoneSticker_Dog = "CAACAgIAAxkBAAELVxllw8wROdi8RGLMZ_3_R4ZeRDtYQgACWQkAAhhC7ghPMS2Lq8xGUjQE";
            public const string PetBoredSticker_Dog = "CAACAgIAAxkBAAELVyRlw8xTEtXywM1BDnm5Yx4BiELi8wACZwkAAhhC7gjEukZwVQulCjQE";
            public const string PetEpilogueSticker_Dog = "CAACAgIAAxkBAAELVyZlw8xrWUK8WQ1UQ_1_0kCHxaI-cQACZQkAAhhC7ghRNmsqi1F8QDQE";
            public const string PetChangeTypeSticker_Dog = "CAACAgIAAxkBAAELVyhlw8yIsr2Atm2ZBdXiVcEMFETligACXgkAAhhC7ggHlhjlQIXYVTQE";
            public const string PetResurrectedSticker_Dog = "CAACAgIAAxkBAAELV91lw9f6qr6XdK-cXD-XGFBYRk206wACWgkAAhhC7ggbWogCtqz9qTQE";
            public const string PetDailyRewardSticker_Dog = "CAACAgIAAxkBAAELWA5lw91FJRKHCGXW7aJW29wWeXYleAAC2gQAAs9fiweFFmx0EB9FSTQE";
            public const string RandomEventStepOnFootSticker_Dog = "CAACAgIAAxkBAAELWBJlw-Nuah1Wy8gpZ8PMwAYS5hBlrAACagkAAhhC7gic251gxtCu3zQE";
            public const string RandomEventPlayComputerSticker_Dog = "CAACAgIAAxkBAAELWCVlw-faKdImb2InG8m6-W1Mly28ogAC6TAAAvENkUveMT_i_hxFOzQE";


            #endregion
            #region Fox
            public const string PetCreatedSticker_Fox = "CAACAgIAAxkBAAELVy9lw846uXHGdDtR9Ejpw525CfzpTwACjDQAAuVOKEjSWphPrEvBrTQE";
            public const string PetInfoSticker_Fox = "CAACAgIAAxkBAAELVzFlw85XzmdMfTYFfk9DtApSL0OcRAACyTIAAp97IUh9cYYuOL2rYjQE";
            public const string PetChooseNameSticker_Fox = "CAACAgIAAxkBAAELVzVlw85p4U-qledsCLnyVwUkT4tEFQACiy0AAuyJIEjYsj1KXl5jjTQE";
            public const string PetConfirmedNameSticker_Fox = "CAACAgIAAxkBAAELV0Flw87G0m4c6dOLCctMJbZgTwbtygAC8jgAAgjoIEj6LRO2YpityzQE";
            public const string PetAskForConfirmNameSticker_Fox = "CAACAgIAAxkBAAELVz9lw86mz3fMv61pf_xg7xPwwfe9fgACUjQAAgawIEgxzp-hTaRq5TQE";
            public const string PetKitchenSticker_Fox = "CAACAgIAAxkBAAELV0Nlw87iuyR8zlOHUIOqWxFWJXGhpQACYDUAAkMXKUgaSvmFR9r-ZjQE";
            public const string PetBathroomSticker_Fox = "CAACAgIAAxkBAAELV09lw89y8IeQ4ygPV83AIXbM8rcIVgACCT0AAgY2KUioemzR5lMUmjQE";
            public const string PetGameroomSticker_Fox = "CAACAgIAAxkBAAELV2tlw9DK6Ujyo3ufnhL60ncjwwrIxAACKDcAAka2IEgp_28yDxN9MDQE";
            public const string PetSleepSticker_Fox = "CAACAgIAAxkBAAELV2llw9A-qQ3E3-75zvZWCvYhzqRERgACbzUAAslPIEhcLHCLep1TvTQE";
            public const string PetBusySticker_Fox = "CAACAgIAAxkBAAELV1Flw89_3U0cw1Z38exbjT7DgQOZrwACCTcAAoFhIEiMddlS390yRzQE";
            public const string PetWorkSticker_Fox = "CAACAgIAAxkBAAELV2dlw9A2xiTHxoxTFHTkP9KQOsicrwACmTIAAtLuIEhPwQSfYQe0ljQE";
            public const string PetWorkOnPCSticker_Fox = "CAACAgIAAxkBAAELV2Vlw9AnYXhXlN5NHDUdDLijfQ345wACyjgAAqU_KEiFGxKHX9QFGTQE";
            public const string PetFlyersJobSticker_Fox = "CAACAgIAAxkBAAELV1Nlw8-Rj0VefwUtl8ckXNv5L4D6-QAC3DYAAtd5IEgAAas3A37LoaI0BA";
            public const string PetRanksSticker_Fox = "CAACAgIAAxkBAAELV1llw8-qdahZPqqmyUznzCLJnpc1HQACCTMAAjXBIUjc3Wnw_bQ7lTQE";
            public const string PetHospitalLowHPSticker_Fox = "CAACAgIAAxkBAAELV1tlw8_GSqjcGiS6yIgPlR4ggvF2iQACdkMAAn3PGErI1rWOGZygsDQE";
            public const string PetHospitalMidHPSticker_Fox = "CAACAgIAAxkBAAELV11lw8_ZY_Rsm12O7IOMnbJFoM22TQACCkUAAm_QEUrlsSa9AAFo8000BA";
            public const string PetHospitalHighHPSticker_Fox = "CAACAgIAAxkBAAELV2Flw9AHbn7ws7TowrXUOtg22YXoDAACtzYAAhsMIUjVYG_MAAFvDRM0BA";
            public const string PetGoneSticker_Fox = "CAACAgIAAxkBAAELV0dlw88V2THPoB8JLl7rI9XRYyrx5QACQDQAAm8LIUhIycWRe_ybWDQE";
            public const string PetBoredSticker_Fox = "CAACAgIAAxkBAAELV01lw89Hdvt5uTxpu8bfjBq__3HifAACdDEAAuw1IEiO1EjRkZsrlzQE";
            public const string PetEpilogueSticker_Fox = "CAACAgIAAxkBAAELV2Nlw9AQg5Rla5S_p9YGdvirVNfdtwACQDEAArFFIUimAwWk5k1--jQE";
            public const string PetChangeTypeSticker_Fox = "CAACAgIAAxkBAAELV0Vlw88G3Ypj3h0LuJqiGutLPydQKQACmzMAAtTOIEiLKrkXpGw65TQE";
            public const string PetResurrectedSticker_Fox = "CAACAgIAAxkBAAELV99lw9hHQ_sMm3oM8IxBsqAPjP_EoAACtzQAAtKeIUjmaRBDRyFonzQE";
            public const string PetDailyRewardSticker_Fox = "CAACAgIAAxkBAAELWAxlw90RYCIGfuZuM_NY8mgruq7fZwACoTwAArrqIEhnwKvheEoTmjQE";
            public const string RandomEventStepOnFootSticker_Fox = "CAACAgIAAxkBAAELWBRlw-OwQIXoyP1pwLpg5iu3702WXQACUDUAAjvJKUjS0uOZC4eeAAE0BA";
            public const string RandomEventPlayComputerSticker_Fox = "CAACAgIAAxkBAAELWCllw-hJh_aKF-UsNyc-lPTkPlCM5QACI0MAAvLpEErnN_bAYHUR3jQE";


            #endregion
            #region Panda
            public const string PetCreatedSticker_Panda = "CAACAgIAAxkBAAELV3tlw9HfCljgKy6GgOUvFFOu8NgoDQACAhYAAiIRyUsmEzdJ6qGvSjQE";
            public const string PetInfoSticker_Panda = "CAACAgIAAxkBAAELV29lw9E9Bo8lPR9-exu_cnErM_8NwAACAxsAAridwEuPy6kILdeN-DQE";
            public const string PetChooseNameSticker_Panda = "CAACAgIAAxkBAAELV3Vlw9GHccRbFFqgrM_IJa9t81EzfgACQRQAAk0FyUspWF-hdBjmWjQE";
            public const string PetConfirmedNameSticker_Panda = "CAACAgIAAxkBAAELV3Flw9FOqXJzYAo3wYBH0Dt48HKFqAACySEAAhdewUszxCTvFpaaajQE";
            public const string PetAskForConfirmNameSticker_Panda = "CAACAgIAAxkBAAELV3dlw9GXeh9at2d0ULqUPuYtMwE61wACchYAAtd6yEtnHlJCZwzT8DQE";
            public const string PetKitchenSticker_Panda = "CAACAgIAAxkBAAELV3llw9GvnD5QhTwYrnTaOaWh4Rqm1wACHAADWbv8JSrZ6qs79DMoNAQ";
            public const string PetBathroomSticker_Panda = "CAACAgIAAxkBAAELV5dlw9QLFyLsjpnUXHI4_Ql8FMwuSQACMAIAAladvQpjVD01Coog_jQE";
            public const string PetGameroomSticker_Panda = "CAACAgIAAxkBAAELV31lw9JdRT078wFYPJkt-AzwkIDqdQACkBcAAjNzwEu7XdTYyjV2lDQE";
            public const string PetSleepSticker_Panda = "CAACAgIAAxkBAAELV3Nlw9FoF9--wW40js9lsX3eoA8yTQAC0BoAApPTwUv4G5BfqfKOtTQE";
            public const string PetBusySticker_Panda = "CAACAgIAAxkBAAELV4dlw9Mg3RAxhLSRoPVmzJ2dejSX6AAC7BwAAuvtwEsJWntniT-Q2jQE";
            public const string PetWorkSticker_Panda = "CAACAgIAAxkBAAELV4llw9OFVOWfE7gq7yucNFv-qJ17agACEAIAAladvQrPLbC7PfSjKTQE";
            public const string PetWorkOnPCSticker_Panda = "CAACAgIAAxkBAAELV4tlw9OuTGBrkUlScqSlS58zzE502wACIgIAAladvQoWnIBwuI2eCjQE";
            public const string PetFlyersJobSticker_Panda = "CAACAgIAAxkBAAELV41lw9PCaDTalY00_OvjxnfvPKbuPQACEQIAAladvQqSi9pRqYNsWzQE";
            public const string PetRanksSticker_Panda = "CAACAgIAAxkBAAELV49lw9Pblq6IHaSNYcFJkA_9R3LDYwACMQIAAladvQoi_qohWhUl5jQE";
            public const string PetHospitalLowHPSticker_Panda = "CAACAgIAAxkBAAELV5Nlw9Puus6he2DM3YjR5LY7ymbNEAACJAIAAladvQp7NaMbcOWxxzQE";
            public const string PetHospitalMidHPSticker_Panda = "CAACAgIAAxkBAAELV5llw9QWCIrJboEkPiBsGDv20w_ZRQACNAIAAladvQoxjc5OT0KBdzQE";
            public const string PetHospitalHighHPSticker_Panda = "CAACAgIAAxkBAAELV5Flw9PioqrbngZpaXC5L6FTGUfiQQACDwIAAladvQqCVst5aR3_tDQE";
            public const string PetGoneSticker_Panda = "CAACAgIAAxkBAAELV4Flw9J4CW0ac9meqpFTVLeucixoJQACrBgAAuxlwUs29NObc5ky4jQE";
            public const string PetBoredSticker_Panda = "CAACAgIAAxkBAAELV39lw9JtSF7DKy0FdxtmqIk7tNSA8wAC0BsAAjEmwEvb8-S2Aw5LlzQE";
            public const string PetEpilogueSticker_Panda = "CAACAgIAAxkBAAELV4Nlw9KB0J7udS0NhgvzlboBg2ZxawACvBcAAtvxwUtcHRCsuCKKDTQE";
            public const string PetChangeTypeSticker_Panda = "CAACAgIAAxkBAAELV4Vlw9KZZU95dtCbcKHKjkZnuKXBuwACUBkAArxmwUsj9E5j_nz29jQE";
            public const string PetResurrectedSticker_Panda = "CAACAgIAAxkBAAELV-Nlw9htT_e_fmpY3B3q1z2yHU5UUgACLxkAArtJwEugr4UulzhpvTQE";
            public const string PetDailyRewardSticker_Panda = "CAACAgIAAxkBAAELWAplw9zoVdoglLRbnCrefkM7KOvXSQACMwIAAladvQoLbxjsYc379DQE";
            public const string RandomEventStepOnFootSticker_Panda = "CAACAgIAAxkBAAELWBZlw-Ph9SK7Ffrjp0Roe7vxBqFiWwACER4AAjkiwUvvGJjJX5c0XTQE";
            public const string RandomEventPlayComputerSticker_Panda = "CAACAgIAAxkBAAELWCtlw-hiRr9DYsPgfZ8oplKsmb4WAwACLgIAAladvQoci82eqDtHYTQE";


            #endregion
            #region Mouse
            public const string PetCreatedSticker_Mouse = "CAACAgIAAxkBAAELV51lw9VBwPOT7XRw9JhSopFzR8oDzAACTDgAAmykcUrbULO1DqewbjQE";
            public const string PetInfoSticker_Mouse = "CAACAgIAAxkBAAELV9Blw9bfTRMXc8xm8bcyrm-Thfy_CQACjzYAAlapcUoUS_qTvshwQTQE";
            public const string PetChooseNameSticker_Mouse = "CAACAgIAAxkBAAELV85lw9bKXHac6L8dJQS2d-GFT-s6_wACDGoAAsX7cUoHXwLrefy1TzQE";
            public const string PetConfirmedNameSticker_Mouse = "CAACAgIAAxkBAAELV8xlw9a_fz2b83Gmhsj4EAwQbCD59QACBzUAAoqacUoz1Bfk5ZxumjQE";
            public const string PetAskForConfirmNameSticker_Mouse = "CAACAgIAAxkBAAELV8plw9anwJt6p7Uf2mt4itDFupsi-gACPzoAAtamcErakNy0_MI_0DQE";
            public const string PetKitchenSticker_Mouse = "CAACAgIAAxkBAAELV6dlw9WSSEOGZIPT9QJbGs4H6_29KgACfDcAAjlpcUrzB5mQD_5dgzQE";
            public const string PetBathroomSticker_Mouse = "CAACAgIAAxkBAAELV61lw9XjN4ShCKGSj5jq4VeFA3h75AACIjkAAleUcEoFNtSkca00mzQE";
            public const string PetGameroomSticker_Mouse = "CAACAgIAAxkBAAELV69lw9XrP6h3jHxJ2ryfzZE7LATEyAACxDcAAg4acUq0aaTn6eXCujQE";
            public const string PetSleepSticker_Mouse = "CAACAgIAAxkBAAELV7Rlw9YQrJWkvK6mM_Tr5jrR2z4s_gACozoAAgNveUrqZYcPdC1X6TQE";
            public const string PetBusySticker_Mouse = "CAACAgIAAxkBAAELV7Zlw9Ydfu71vHHQCM17qNbaHBHh4wACcUEAAtbwcEpH_LK0WE0ITTQE";
            public const string PetWorkSticker_Mouse = "CAACAgIAAxkBAAELV6Flw9VdRrE90S9GpgfeRl5rqBpBPAACATsAAkpmcUog3oB_4pcCLTQE";
            public const string PetWorkOnPCSticker_Mouse = "CAACAgIAAxkBAAELV59lw9VRYEE8LW73swz4IU8tNJDd_QACdzoAAkNQcUpFU-um8ulorjQE";
            public const string PetFlyersJobSticker_Mouse = "CAACAgIAAxkBAAELV6Nlw9VrtcPlFwxYdH9jJOJKje6rKgACYDkAAn6tcEqIVAmi8sIp0zQE";
            public const string PetRanksSticker_Mouse = "CAACAgIAAxkBAAELV6Vlw9WJo1PsighSMbO4Dksqzj3laQACUDwAAu94cEpu3NybwaQe2jQE";
            public const string PetHospitalLowHPSticker_Mouse = "CAACAgIAAxkBAAELV7plw9Y3XDOGE-g5fK21jKi4lsd0zgACXzQAAveLcUosNxJuBHNtjzQE";
            public const string PetHospitalMidHPSticker_Mouse = "CAACAgIAAxkBAAELV7hlw9YnnK50ofbJNjzm3QF-aq93dQACXUEAAkECcEomrYovwfDw7DQE";
            public const string PetHospitalHighHPSticker_Mouse = "CAACAgIAAxkBAAELV7xlw9ZRLflPu7XSxihiiy1aegbIEwACczQAAteMcEqzRnfCqtdqRTQE";
            public const string PetGoneSticker_Mouse = "CAACAgIAAxkBAAELV8hlw9aZshn5qNqCHsdJNwH6f5d0mwACKzMAArZHcEpkCdiTnaUfWjQE";
            public const string PetBoredSticker_Mouse = "CAACAgIAAxkBAAELV8Rlw9aI4FKsUxfNeOuiLHPvMj2XBAACqjQAAkFScUo2IckSOZ5rWTQE";
            public const string PetEpilogueSticker_Mouse = "CAACAgIAAxkBAAELV8Jlw9Z1V8qmAXkNmRKpIMajpLOcRQACiTsAAvMBcUr2uzNN4y3gwjQE";
            public const string PetChangeTypeSticker_Mouse = "CAACAgIAAxkBAAELV5tlw9U3mRUeWCC2V3sFHWkENwFYrgACsjgAAt5ucUpdU7rIDp-ahDQE";
            public const string PetResurrectedSticker_Mouse = "CAACAgIAAxkBAAELV-dlw9iSNbzLa65RYAexi5duguM-GQAC_jkAAkBMcEr9wDeYbcLDzDQE";
            public const string PetDailyRewardSticker_Mouse = "CAACAgIAAxkBAAELWDFlw-nwTUsoBQJykSjLmv8aNpq3vgACKzMAArZHcEpkCdiTnaUfWjQE";
            public const string RandomEventStepOnFootSticker_Mouse = "CAACAgIAAxkBAAELWBhlw-QFXhuKQSXSyec3yiaUx3ltvAACIEEAAkxQcEqGPo78f8IK5TQE";
            public const string RandomEventPlayComputerSticker_Mouse = "CAACAgIAAxkBAAELWC9lw-jVG8F1DFEqxMm-pHJ9ZGFdzwACEjYAAhJzcUp1235GLY3NNzQE";


            #endregion
        }

        public static class InlineItems
        {
            public static List<CallbackModel> InlineFarm(CultureInfo culture) => _inlineFarm(culture);
            private static List<CallbackModel> _inlineFarm(CultureInfo culture) => new()
            {
                CallbackButtons.FarmCommand.FarmCommandInlineEnableAutoFeed(culture),
                CallbackButtons.FarmCommand.FarmCommandInlineDisableAutoFeed(culture)
            };

            public static List<CallbackModel> InlineFood => _inlineFood;
            private static List<CallbackModel> _inlineFood = new()
            {
                CallbackButtons.KitchenCommand.KitchenCommandInlineBread,
                CallbackButtons.KitchenCommand.KitchenCommandInlineRedApple,
                CallbackButtons.KitchenCommand.KitchenCommandInlineChocolate,
                CallbackButtons.KitchenCommand.KitchenCommandInlineLollipop
            };

            public static List<CallbackModel> InlineReferal(CultureInfo culture) => _inlineReferal(culture);
            private static List<CallbackModel> _inlineReferal(CultureInfo culture) => new()
            {
                CallbackButtons.ReferalCommand.ToAddToNewGroupReferalCommand(culture),
                CallbackButtons.ReferalCommand.ToShareReferalCommand(culture)
            };

            public static List<CallbackModel> InlineGames => _inlineGames;
            private static List<CallbackModel> _inlineGames = new()
            {
                CallbackButtons.GameroomCommand.GameroomCommandInlineAppleGame,
                CallbackButtons.GameroomCommand.GameroomCommandInlineDice,
                CallbackButtons.GameroomCommand.GameroomCommandInlineTicTacToe,
                CallbackButtons.GameroomCommand.GameroomCommandInlineHangman
            };


            public static List<CallbackModel> InlineHygiene(CultureInfo culture) => _inlineHygiene(culture);
            private static List<CallbackModel> _inlineHygiene(CultureInfo culture) => new()
            {
                CallbackButtons.BathroomCommand.BathroomCommandBrushTeeth(culture),
                CallbackButtons.BathroomCommand.BathroomCommandTakeShower(culture),
                CallbackButtons.BathroomCommand.BathroomCommandMakePoo(culture)
            };

            public static List<CallbackModel> InlineHospital(CultureInfo culture) => _inlineHospital(culture);
            private static List<CallbackModel> _inlineHospital(CultureInfo culture) => new()
            {
                CallbackButtons.HospitalCommand.HospitalCommandCurePills(culture)
            };

            public static List<CallbackModel> InlineWork(CultureInfo culture) => _inlineWork(culture);
            private static List<CallbackModel> _inlineWork(CultureInfo culture) => new()
            {
                CallbackButtons.WorkCommand.WorkCommandInlineWorkOnPC(culture),
                CallbackButtons.WorkCommand.WorkCommandInlineDistributeFlyers(culture),
            };

            public static List<CallbackModel> InlineRanks(CultureInfo culture) => _inlineRanks(culture);
            private static List<CallbackModel> _inlineRanks(CultureInfo culture) => new()
            {
                CallbackButtons.RanksCommand.RanksCommandInlineLevel(culture),
                CallbackButtons.RanksCommand.RanksCommandInlineGold(culture),
                CallbackButtons.RanksCommand.RanksCommandInlineDiamonds(culture),
                CallbackButtons.RanksCommand.RanksCommandInlineApples(culture),
                CallbackButtons.RanksCommand.RanksCommandInlineLevelAll(culture),
            };

            public static List<CallbackModel> InlineRewards(CultureInfo culture) => _inlineRewards(culture);
            private static List<CallbackModel> _inlineRewards(CultureInfo culture) => new()
            {
                CallbackButtons.RewardsCommand.RewardCommandInlineDailyReward(culture)
            };

            public static List<CallbackModel> InlinePet(CultureInfo culture) => _inlinePet(culture);
            private static List<CallbackModel> _inlinePet(CultureInfo culture) => new()
            {
                CallbackButtons.PetCommand.PetCommandInlineExtraInfo(culture)
            };

            public static List<CallbackModel> InlineShowInviteMP(CultureInfo culture) => _inlineShowInviteMP(culture);
            private static List<CallbackModel> _inlineShowInviteMP(CultureInfo culture) => new()
            {
                CallbackButtons.PetCommand.PetCommandInlineExtraInfo(culture)
            };

            public static List<CallbackModel> InlineShowRanksMP(CultureInfo culture) => _inlineShowRanksMP(culture);
            private static List<CallbackModel> _inlineShowRanksMP(CultureInfo culture) => new()
            {
                CallbackButtons.RanksMultiplayerCommand.ShowChatRanksMP(culture),
                CallbackButtons.RanksMultiplayerCommand.ShowGlobalRanksMP(culture)
            };
        }

        public static class ReplyKeyboardItems
        {
            public static ReplyKeyboardMarkup ChangeTypeKeyboardMarkup(CultureInfo culture) => _changeTypeKeyboardMarkup(culture);
            private static ReplyKeyboardMarkup _changeTypeKeyboardMarkup(CultureInfo culture) =>
            Extensions.ReplyKeyboardOptimizer(
                Extensions.GetChangeTypeButtons(culture),
                columnCounter: 3,
                isOneTimeKeyboard: false
                );
            public static ReplyKeyboardMarkup FarmKeyboardMarkup(CultureInfo culture) => _farmKeyboardMarkup(culture);
            private static ReplyKeyboardMarkup _farmKeyboardMarkup(CultureInfo culture) =>
            Extensions.ReplyKeyboardOptimizer(
                Extensions.GetFarmButtons(culture),
                columnCounter: 3,
                isOneTimeKeyboard: false
                );
            public static ReplyKeyboardMarkup MenuKeyboardMarkup(CultureInfo culture) => _menuKeyboardMarkup(culture);
            private static ReplyKeyboardMarkup _menuKeyboardMarkup(CultureInfo culture) =>
            Extensions.ReplyKeyboardOptimizer(
                Extensions.GetMenuButtons(culture),
                columnCounter: 3,
                isOneTimeKeyboard: false
                );

            public static ReplyKeyboardMarkup LanguagesMarkup => _languagesMarkup;
            private static ReplyKeyboardMarkup _languagesMarkup = Extensions.ReplyKeyboardOptimizer(Extensions.LanguagesWithFlags(), isOneTimeKeyboard: true);
        }

    }
}
