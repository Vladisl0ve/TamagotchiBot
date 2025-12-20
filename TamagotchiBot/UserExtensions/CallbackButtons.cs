using System;
using System.Globalization;
using TamagotchiBot.Models;
using static TamagotchiBot.UserExtensions.Constants;

namespace TamagotchiBot.UserExtensions
{
    public static class CallbackButtons
    {
        public static class KitchenCommand
        {
            public static CallbackModel KitchenCommandInlineBread
            {
                get => _kitchenCommandInlineBread;
            }
            private static CallbackModel _kitchenCommandInlineBread = new CallbackModel()
            {
                Text = "🍞",
                CallbackData = nameof(KitchenCommandInlineBread)
            };

            public static CallbackModel KitchenCommandInlineRedApple
            {
                get => _kitchenCommandInlineRedApple;
            }
            private static CallbackModel _kitchenCommandInlineRedApple = new CallbackModel()
            {
                Text = "🍎",
                CallbackData = nameof(KitchenCommandInlineRedApple)
            };

            public static CallbackModel KitchenCommandInlineChocolate
            {
                get => _kitchenCommandInlineChocolate;
            }
            private static CallbackModel _kitchenCommandInlineChocolate = new CallbackModel()
            {
                Text = "🍫",
                CallbackData = nameof(KitchenCommandInlineChocolate)
            };

            public static CallbackModel KitchenCommandInlineLollipop
            {
                get => _kitchenCommandInlineLollipop;
            }
            private static CallbackModel _kitchenCommandInlineLollipop = new CallbackModel()
            {
                Text = "🍭",
                CallbackData = nameof(KitchenCommandInlineLollipop)
            };
        }

        public static class FarmCommand
        {
            public static CallbackModel FarmCommandInlineEnableAutoFeed(CultureInfo culture) => new CallbackModel()
            {
                Text = nameof(Resources.Resources.FarmCommandInlineEnableAutoFeed).UseCulture(culture),
                CallbackData = nameof(FarmCommandInlineEnableAutoFeed)
            };

            public static CallbackModel FarmCommandInlineDisableAutoFeed(CultureInfo culture) => new CallbackModel()
            {
                Text = nameof(Resources.Resources.FarmCommandInlineDisableAutoFeed).UseCulture(culture),
                CallbackData = nameof(FarmCommandInlineDisableAutoFeed)
            };
        }
        public static class GameroomCommand
        {
            public static CallbackModel GameroomCommandInlineAppleGame { get => _gameroomCommandInlineAppleGame; }
            private static CallbackModel _gameroomCommandInlineAppleGame = new CallbackModel()
            {
                Text = "🍏",
                CallbackData = nameof(GameroomCommandInlineAppleGame)
            };

            public static CallbackModel GameroomCommandInlineDice { get => _gameroomCommandInlineDice; }
            private static CallbackModel _gameroomCommandInlineDice = new CallbackModel()
            {
                Text = "🎲",
                CallbackData = nameof(GameroomCommandInlineDice)
            };

            public static CallbackModel GameroomCommandInlineTicTacToe { get => _gameroomCommandInlineTicTacToe; }
            private static CallbackModel _gameroomCommandInlineTicTacToe = new CallbackModel()
            {
                Text = "❌⭕",
                CallbackData = nameof(GameroomCommandInlineTicTacToe)
            };

            public static CallbackModel GameroomCommandInlineHangman { get => _gameroomCommandInlineHangman; }
            private static CallbackModel _gameroomCommandInlineHangman = new CallbackModel()
            {
                Text = "😵",
                CallbackData = nameof(GameroomCommandInlineHangman)
            };
        }
        public static class SleepCommand
        {
            public static CallbackModel SleepCommandInlinePutToSleep(TimeSpan remainsTime, CultureInfo culture) => new CallbackModel()
            {
                Text = string.Format(nameof(Resources.Resources.sleepCommandInlineShowTime).UseCulture(culture), new DateTime().AddTicks(remainsTime.Ticks).ToString("HH:mm:ss")),
                CallbackData = nameof(SleepCommandInlinePutToSleep)
            };
        }
        public static class BathroomCommand
        {
            public static CallbackModel BathroomCommandBrushTeeth(CultureInfo culture) => new CallbackModel()
            {
                Text = string.Format(nameof(Resources.Resources.bathroomCommandBrushTeeth).UseCulture(culture)),
                CallbackData = nameof(BathroomCommandBrushTeeth)
            };
            public static CallbackModel BathroomCommandTakeShower(CultureInfo culture) => new CallbackModel()
            {
                Text = string.Format(nameof(Resources.Resources.bathroomCommandTakeShower).UseCulture(culture)),
                CallbackData = nameof(BathroomCommandTakeShower)
            };
            public static CallbackModel BathroomCommandMakePoo(CultureInfo culture) => new CallbackModel()
            {
                Text = string.Format(nameof(Resources.Resources.bathroomCommandMakePoo).UseCulture(culture)),
                CallbackData = nameof(BathroomCommandMakePoo)
            };
        }
        public static class HospitalCommand
        {
            public static CallbackModel HospitalCommandCurePills(CultureInfo culture) => new CallbackModel()
            {
                Text = string.Format(nameof(Resources.Resources.hospitalCommandCurePills).UseCulture(culture)),
                CallbackData = nameof(HospitalCommandCurePills)
            };
        }
        public static class WorkCommand
        {
            public static CallbackModel WorkCommandInlineShowTime(TimeSpan remainedTime, JobType jobType, CultureInfo culture)
            {
                string timeToShow;
                if (remainedTime > TimeSpan.Zero)
                    timeToShow = new DateTime(remainedTime.Ticks).ToString("HH:mm:ss");
                else
                    timeToShow = new DateTime(0).ToString("HH:mm:ss");

                return jobType switch
                {
                    JobType.WorkingOnPC => new CallbackModel()
                    {
                        Text = string.Format(nameof(Resources.Resources.workCommandInlineShowTime).UseCulture(culture), timeToShow),
                        CallbackData = nameof(WorkCommandInlineShowTime)
                    },
                    JobType.FlyersDistributing => new CallbackModel()
                    {
                        Text = string.Format(nameof(Resources.Resources.workCommandInlineShowTime).UseCulture(culture), timeToShow),
                        CallbackData = nameof(WorkCommandInlineShowTime)
                    },
                    JobType.McDonalds => new CallbackModel()
                    {
                        Text = string.Format(nameof(Resources.Resources.workCommandInlineShowTime).UseCulture(culture), timeToShow),
                        CallbackData = nameof(WorkCommandInlineShowTime)
                    },
                    JobType.MakeUpArtist => new CallbackModel()
                    {
                        Text = string.Format(nameof(Resources.Resources.workCommandInlineShowTime).UseCulture(culture), timeToShow),
                        CallbackData = nameof(WorkCommandInlineShowTime)
                    },
                    JobType.FoodDelivery => new CallbackModel()
                    {
                        Text = string.Format(nameof(Resources.Resources.workCommandInlineShowTime).UseCulture(culture), timeToShow),
                        CallbackData = nameof(WorkCommandInlineShowTime)
                    },
                    JobType.Accountant => new CallbackModel()
                    {
                        Text = string.Format(nameof(Resources.Resources.workCommandInlineShowTime).UseCulture(culture), timeToShow),
                        CallbackData = nameof(WorkCommandInlineShowTime)
                    },
                    JobType.Engineer => new CallbackModel()
                    {
                        Text = string.Format(nameof(Resources.Resources.workCommandInlineShowTime).UseCulture(culture), timeToShow),
                        CallbackData = nameof(WorkCommandInlineShowTime)
                    },
                    JobType.Pilot => new CallbackModel()
                    {
                        Text = string.Format(nameof(Resources.Resources.workCommandInlineShowTime).UseCulture(culture), timeToShow),
                        CallbackData = nameof(WorkCommandInlineShowTime)
                    },

                    _ => new CallbackModel()
                    {
                        Text = "ERROR",
                        CallbackData = "ERROR"
                    }
                };
            }
            public static CallbackModel WorkCommandInlineWorkOnPC(CultureInfo culture) => new CallbackModel()
            {
                Text = nameof(Resources.Resources.workCommandInlinePC).UseCulture(culture),
                CallbackData = nameof(WorkCommandInlineWorkOnPC)
            };
            public static CallbackModel WorkCommandInlineDistributeFlyers(CultureInfo culture) => new CallbackModel()
            {
                Text = nameof(Resources.Resources.workCommandInlineFlyers).UseCulture(culture),
                CallbackData = nameof(WorkCommandInlineDistributeFlyers)
            };
            public static CallbackModel WorkCommandInlineMcDonalds(CultureInfo culture) => new CallbackModel()
            {
                Text = nameof(Resources.Resources.workCommandInlineMcDonalds).UseCulture(culture),
                CallbackData = nameof(WorkCommandInlineMcDonalds)
            };
            public static CallbackModel WorkCommandInlineMakeUpArtist(CultureInfo culture) => new CallbackModel()
            {
                Text = nameof(Resources.Resources.workCommandInlineMakeUpArtist).UseCulture(culture),
                CallbackData = nameof(WorkCommandInlineMakeUpArtist)
            };
            public static CallbackModel WorkCommandInlineFoodDelivery(CultureInfo culture) => new CallbackModel()
            {
                Text = nameof(Resources.Resources.workCommandInlineFoodDelivery).UseCulture(culture),
                CallbackData = nameof(WorkCommandInlineFoodDelivery)
            };
            public static CallbackModel WorkCommandInlineAccountant(CultureInfo culture) => new CallbackModel()
            {
                Text = nameof(Resources.Resources.workCommandInlineAccountant).UseCulture(culture),
                CallbackData = nameof(WorkCommandInlineAccountant)
            };
            public static CallbackModel WorkCommandInlineEngineer(CultureInfo culture) => new CallbackModel()
            {
                Text = nameof(Resources.Resources.workCommandInlineEngineer).UseCulture(culture),
                CallbackData = nameof(WorkCommandInlineEngineer)
            };
            public static CallbackModel WorkCommandInlinePilot(CultureInfo culture) => new CallbackModel()
            {
                Text = nameof(Resources.Resources.workCommandInlinePilot).UseCulture(culture),
                CallbackData = nameof(WorkCommandInlinePilot)
            };
        }
        public static class RanksCommand
        {
            public static CallbackModel RanksCommandInlineLevel(CultureInfo culture) => new CallbackModel()
            {
                Text = nameof(Resources.Resources.ranksCommandInlineLevel).UseCulture(culture),
                CallbackData = nameof(RanksCommandInlineLevel)
            };

            public static CallbackModel RanksCommandInlineLevelAll(CultureInfo culture) => new CallbackModel()
            {
                Text = nameof(Resources.Resources.ranksCommandInlineLevelAll).UseCulture(culture),
                CallbackData = nameof(RanksCommandInlineLevelAll)
            };

            public static CallbackModel RanksCommandInlineGold(CultureInfo culture) => new CallbackModel()
            {
                Text = nameof(Resources.Resources.ranksCommandInlineGold).UseCulture(culture),
                CallbackData = nameof(RanksCommandInlineGold)
            };

            public static CallbackModel RanksCommandInlineDiamonds(CultureInfo culture) => new CallbackModel()
            {
                Text = nameof(Resources.Resources.ranksCommandInlineDiamonds).UseCulture(culture),
                CallbackData = nameof(RanksCommandInlineDiamonds)
            };

            public static CallbackModel RanksCommandInlineApples(CultureInfo culture) => new CallbackModel()
            {
                Text = nameof(Resources.Resources.ranksCommandInlineApples).UseCulture(culture),
                CallbackData = nameof(RanksCommandInlineApples)
            };
            public static CallbackModel RanksCommandInlineTicTakToe(CultureInfo culture) => new CallbackModel()
            {
                Text = nameof(Resources.Resources.ranksCommandInlineTicTakToe).UseCulture(culture),
                CallbackData = nameof(RanksCommandInlineTicTakToe)
            };
        }
        public static class RewardsCommand
        {
            public static CallbackModel RewardCommandDailyRewardInlineShowTime(TimeSpan remainedTime, CultureInfo culture) => new CallbackModel()
            {
                Text = string.Format(nameof(Resources.Resources.rewardCommandDailyRewardInlineShowTime).UseCulture(culture), new DateTime(remainedTime.Ticks).ToString("HH:mm:ss")),
                CallbackData = nameof(RewardCommandDailyRewardInlineShowTime)
            };

            public static CallbackModel RewardCommandInlineDailyReward(CultureInfo culture) => new CallbackModel()
            {
                Text = string.Format(
                    nameof(Resources.Resources.rewardCommandInlineDailyReward).UseCulture(culture),
                    Rewards.DailyGoldReward),
                CallbackData = nameof(RewardCommandInlineDailyReward)
            };
        }
        public static class PetCommand
        {
            public static CallbackModel PetCommandInlineBasicInfo(CultureInfo culture) => new CallbackModel()
            {
                Text = nameof(Resources.Resources.petCommandInlineBasicInfo).UseCulture(culture),
                CallbackData = nameof(PetCommandInlineBasicInfo)
            };
            public static CallbackModel PetCommandInlineExtraInfo(CultureInfo culture) => new CallbackModel()
            {
                Text = nameof(Resources.Resources.petCommandInlineExtraInfo).UseCulture(culture),
                CallbackData = nameof(PetCommandInlineExtraInfo)
            };
        }
        public static class ReferalCommand
        {
            public static CallbackModel ToAddToNewGroupReferalCommand(CultureInfo culture) => new CallbackModel()
            {
                Text = nameof(Resources.Resources.ToAddToNewGroupReferalCommand).UseCulture(culture),
                CallbackData = nameof(ToAddToNewGroupReferalCommand)
            };

            public static CallbackModel ToShareReferalCommand(CultureInfo culture) => new CallbackModel()
            {
                Text = nameof(Resources.Resources.ToShareReferalCommand).UseCulture(culture),
                CallbackData = nameof(ToShareReferalCommand)
            };
        }
        public static class InviteMuliplayerCommand
        {
            public static CallbackModel InviteGlobalMultiplayerButton(CultureInfo culture)
            {
                var emoji = Extensions.GetTypeEmoji(new Random().Next(5));
                return new CallbackModel()
                {
                    Text = string.Format(nameof(Resources.Resources.InviteGlobalMultiplayerButton).UseCulture(culture), emoji),
                    CallbackData = nameof(InviteGlobalMultiplayerButton)
                };
            }

            public static CallbackModel InviteReferalMultiplayerButton(string refName, CultureInfo culture)
            {
                var emoji = Extensions.GetTypeEmoji(new Random().Next(5));
                return new CallbackModel()
                {
                    Text = string.Format(nameof(Resources.Resources.InviteReferalMultiplayerButton).UseCulture(culture), emoji, refName),
                    CallbackData = nameof(InviteReferalMultiplayerButton)
                };
            }
        }
        public static class DuelMuliplayerCommand
        {
            public static CallbackModel StartDuelMultiplayerButton(CultureInfo culture) => new CallbackModel()
            {
                Text = nameof(Resources.Resources.DuelMPAcceptButton).UseCulture(culture),
                CallbackData = nameof(StartDuelMultiplayerButton)
            };
        }

        public static class RanksMultiplayerCommand
        {
            public static CallbackModel ShowChatRanksMP(CultureInfo culture) => new CallbackModel()
            {
                Text = nameof(Resources.Resources.ShowChatRanksMPButton).UseCulture(culture),
                CallbackData = nameof(ShowChatRanksMP)
            };
            public static CallbackModel ShowGlobalRanksMP(CultureInfo culture) => new CallbackModel()
            {
                Text = nameof(Resources.Resources.ShowGlobalRanksMPButton).UseCulture(culture),
                CallbackData = nameof(ShowGlobalRanksMP)
            };
        }
    }
}
