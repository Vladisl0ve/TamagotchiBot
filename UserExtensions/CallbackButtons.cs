using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TamagotchiBot.Models;
using static TamagotchiBot.UserExtensions.Constants;

namespace TamagotchiBot.UserExtensions
{
    public static class CallbackButtons
    {
        public static class KitchenCommand
        {
            public static CallbackModel KitchenCommandInlineBread = new CallbackModel ()
            {
                Text = "🍞",
                CallbackData = "kitchenCommandInlineBread"
            };

            public static CallbackModel KitchenCommandInlineRedApple = new CallbackModel ()
            {
                Text = "🍎",
                CallbackData = "kitchenCommandInlineRedApple"
            };

            public static CallbackModel KitchenCommandInlineChocolate = new CallbackModel ()
            {
                Text = "🍫",
                CallbackData = "kitchenCommandInlineChocolate"
            };

            public static CallbackModel KitchenCommandInlineLollipop = new CallbackModel ()
            {
                Text = "🍭",
                CallbackData = "kitchenCommandInlineLollipop"
            };
        }
        public static class GameroomCommand
        {
            public static CallbackModel GameroomCommandInlineAppleGame = new CallbackModel ()
            {
                Text = "🍏",
                CallbackData = "gameroomCommandInlineAppleGame"
            };

            public static CallbackModel GameroomCommandInlineDice = new CallbackModel ()
            {
                Text = "🎲",
                CallbackData = "gameroomCommandInlineDice"
            };
        }
        public static class SleepCommand
        {
            public static CallbackModel SleepCommandInlinePutToSleep(TimeSpan remainsTime) => new CallbackModel ()
            {
                Text = string.Format(Resources.Resources.sleepCommandInlineShowTime, new DateTime().AddTicks(remainsTime.Ticks).ToString("HH:mm:ss")),
                CallbackData = "sleepCommandInlinePutToSleep"
            };
        }
        public static class BathroomCommand
        {
            public static CallbackModel BathroomCommandBrushTeeth = new CallbackModel ()
            {
                Text = Resources.Resources.bathroomCommandBrushTeeth,
                CallbackData = "bathroomCommandBrushTeeth"
            };
            public static CallbackModel BathroomCommandTakeShower = new CallbackModel ()
            {
                Text = Resources.Resources.bathroomCommandTakeShower,
                CallbackData = "bathroomCommandTakeShower"
            };
        }
        public static class HospitalCommand
        {
            public static CallbackModel HospitalCommandCurePills = new CallbackModel ()
            {
                Text = Resources.Resources.hospitalCommandCurePills,
                CallbackData = "hospitalCommandCurePills"
            };
        }
        public static class WorkCommand
        {
            public static CallbackModel WorkCommandInlineShowTime(TimeSpan remainedTime) => new CallbackModel ()
            {
                Text = string.Format(Resources.Resources.workCommandInlineShowTime,
                                     new DateTime(remainedTime.Ticks).ToString("HH:mm:ss")),
                CallbackData = "workCommandInlineShowTime"
            };

            public static CallbackModel WorkCommandInlineWorkOnPC = new CallbackModel ()
            {
                Text = Resources.Resources.workCommandInlinePC,
                CallbackData = "workCommandInlineWorkOnPC"
            };
        }
        public static class RanksCommand
        {
            public static CallbackModel RanksCommandInlineLevel = new CallbackModel ()
            {
                Text = Resources.Resources.ranksCommandInlineLevel,
                CallbackData = "ranksCommandInlineLevel"
            };

            public static CallbackModel RanksCommandInlineGold = new CallbackModel ()
            {
                Text = Resources.Resources.ranksCommandInlineGold,
                CallbackData = "ranksCommandInlineGold"
            };

            public static CallbackModel RanksCommandInlineApples = new CallbackModel ()
            {
                Text = Resources.Resources.ranksCommandInlineApples,
                CallbackData = "ranksCommandInlineApples"
            };
        }
        public static class RewardsCommand
        {
            public static CallbackModel RewardCommandDailyRewardInlineShowTime(TimeSpan remainedTime) => new CallbackModel ()
            {
                Text = string.Format(Resources.Resources.rewardCommandDailyRewardInlineShowTime, new DateTime(remainedTime.Ticks).ToString("HH:mm:ss")),
                CallbackData = "rewardCommandDailyRewardInlineShowTime"
            };

            public static CallbackModel RewardCommandInlineDailyReward = new CallbackModel ()
            {
                Text = Resources.Resources.rewardCommandInlineDailyReward,
                CallbackData = "rewardCommandInlineDailyReward"
            };
        }
        public static class PetCommand
        {
            public static CallbackModel PetCommandInlineBasicInfo = new CallbackModel ()
            {
                Text = Resources.Resources.petCommandInlineBasicInfo,
                CallbackData = "petCommandInlineBasicInfo"
            };
            public static CallbackModel PetCommandInlineExtraInfo = new CallbackModel ()
            {
                Text = Resources.Resources.petCommandInlineExtraInfo,
                CallbackData = "petCommandInlineExtraInfo"
            };
        }
    }
}
