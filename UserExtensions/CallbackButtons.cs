using System;
using TamagotchiBot.Models;

namespace TamagotchiBot.UserExtensions
{
    public static class CallbackButtons
    {
        public class KitchenCommand
        {
            public CallbackModel KitchenCommandInlineBread = new CallbackModel ()
            {
                Text = "🍞",
                CallbackData = "kitchenCommandInlineBread"
            };

            public CallbackModel KitchenCommandInlineRedApple = new CallbackModel ()
            {
                Text = "🍎",
                CallbackData = "kitchenCommandInlineRedApple"
            };

            public CallbackModel KitchenCommandInlineChocolate = new CallbackModel ()
            {
                Text = "🍫",
                CallbackData = "kitchenCommandInlineChocolate"
            };

            public CallbackModel KitchenCommandInlineLollipop = new CallbackModel ()
            {
                Text = "🍭",
                CallbackData = "kitchenCommandInlineLollipop"
            };
        }
        public class GameroomCommand
        {
            public CallbackModel GameroomCommandInlineAppleGame = new CallbackModel ()
            {
                Text = "🍏",
                CallbackData = "gameroomCommandInlineAppleGame"
            };

            public CallbackModel GameroomCommandInlineDice = new CallbackModel ()
            {
                Text = "🎲",
                CallbackData = "gameroomCommandInlineDice"
            };
        }
        public class SleepCommand
        {
            public CallbackModel SleepCommandInlinePutToSleep(TimeSpan remainsTime) => new CallbackModel()
            {
                Text = string.Format(Resources.Resources.sleepCommandInlineShowTime, new DateTime().AddTicks(remainsTime.Ticks).ToString("HH:mm:ss")),
                CallbackData = "sleepCommandInlinePutToSleep"
            };
        }
        public class BathroomCommand
        {
            public CallbackModel BathroomCommandBrushTeeth = new CallbackModel ()
            {
                Text = Resources.Resources.bathroomCommandBrushTeeth,
                CallbackData = "bathroomCommandBrushTeeth"
            };
            public CallbackModel BathroomCommandTakeShower = new CallbackModel ()
            {
                Text = Resources.Resources.bathroomCommandTakeShower,
                CallbackData = "bathroomCommandTakeShower"
            };
        }
        public class HospitalCommand
        {
            public CallbackModel HospitalCommandCurePills = new CallbackModel ()
            {
                Text = Resources.Resources.hospitalCommandCurePills,
                CallbackData = "hospitalCommandCurePills"
            };
        }
        public class WorkCommand
        {
            public CallbackModel WorkCommandInlineShowTime(TimeSpan remainedTime)
            {
                string timeToShow;
                if (remainedTime > TimeSpan.Zero)
                    timeToShow = new DateTime(remainedTime.Ticks).ToString("HH:mm:ss");
                else
                    timeToShow = new DateTime(0).ToString("HH:mm:ss");

                var result = new CallbackModel()
                {
                    Text = string.Format(Resources.Resources.workCommandInlineShowTime, timeToShow),
                    CallbackData = "workCommandInlineShowTime"
                };

                return result;
            }

            public CallbackModel WorkCommandInlineWorkOnPC = new CallbackModel ()
            {
                Text = Resources.Resources.workCommandInlinePC,
                CallbackData = "workCommandInlineWorkOnPC"
            };
        }
        public class RanksCommand
        {
            public CallbackModel RanksCommandInlineLevel = new CallbackModel ()
            {
                Text = Resources.Resources.ranksCommandInlineLevel,
                CallbackData = "ranksCommandInlineLevel"
            };

            public CallbackModel RanksCommandInlineGold = new CallbackModel ()
            {
                Text = Resources.Resources.ranksCommandInlineGold,
                CallbackData = "ranksCommandInlineGold"
            };

            public CallbackModel RanksCommandInlineApples = new CallbackModel ()
            {
                Text = Resources.Resources.ranksCommandInlineApples,
                CallbackData = "ranksCommandInlineApples"
            };
        }
        public class RewardsCommand
        {
            public CallbackModel RewardCommandDailyRewardInlineShowTime(TimeSpan remainedTime) => new CallbackModel()
            {
                Text = string.Format(Resources.Resources.rewardCommandDailyRewardInlineShowTime, new DateTime(remainedTime.Ticks).ToString("HH:mm:ss")),
                CallbackData = "rewardCommandDailyRewardInlineShowTime"
            };

            public CallbackModel RewardCommandInlineDailyReward = new CallbackModel ()
            {
                Text = Resources.Resources.rewardCommandInlineDailyReward,
                CallbackData = "rewardCommandInlineDailyReward"
            };
        }
        public class PetCommand
        {
            public CallbackModel PetCommandInlineBasicInfo = new CallbackModel ()
            {
                Text = Resources.Resources.petCommandInlineBasicInfo,
                CallbackData = "petCommandInlineBasicInfo"
            };
            public CallbackModel PetCommandInlineExtraInfo = new CallbackModel ()
            {
                Text = Resources.Resources.petCommandInlineExtraInfo,
                CallbackData = "petCommandInlineExtraInfo"
            };
        }
        public class ReferalCommand
        {
            public CallbackModel ToAddToNewGroupReferalCommand = new CallbackModel ()
            {
                Text = Resources.Resources.ToAddToNewGroupReferalCommand,
                CallbackData = "ToAddToNewGroupReferalCommand"
            };

            public CallbackModel ToShareReferalCommand = new CallbackModel ()
            {
                Text = Resources.Resources.ToShareReferalCommand,
                CallbackData = "ToShareReferalCommand"
            };
        }
        public class InviteMuliplayerCommand
        {
            public CallbackModel InviteGlobalMultiplayerButton = new CallbackModel ()
            {
                Text = Resources.Resources.InviteGlobalMultiplayerButton,
                CallbackData = "InviteGlobalMultiplayerButton"
            };
            public CallbackModel InviteReferalMultiplayerButton(string refName) => new CallbackModel()
            {
                Text = string.Format(Resources.Resources.InviteReferalMultiplayerButton, refName),
                CallbackData = "InviteReferalMultiplayerButton"
            };
        } 
        public class DuelMuliplayerCommand
        {
            public CallbackModel StartDuelMultiplayerButton = new CallbackModel ()
            {
                Text = "GO",
                CallbackData = "StartDuelMultiplayerButton"
            };
        }
    }
}
