using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TamagotchiBot.Models;
using TamagotchiBot.Models.Mongo;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using static TamagotchiBot.UserExtensions.Constants;

namespace TamagotchiBot.UserExtensions
{
    public static class Extensions
    {
        public static ReplyKeyboardMarkup ReplyKeyboardOptimizer(List<string> names, int columnCounter = 2, bool isOneTimeKeyboard = false)
        {
            int x = columnCounter < 2 ? 2 : columnCounter;
            int y = (int)Math.Ceiling((double)(names.Count) / x);
            int counter = 0;

            KeyboardButton[][] keyboard = new KeyboardButton[y][];

            for (int i = 0; i < keyboard.Length; i++)
            {
                if (i + 1 == keyboard.Length)
                    keyboard[i] = new KeyboardButton[names.Count - counter];
                else
                    keyboard[i] = new KeyboardButton[x];

                for (int j = 0; j < keyboard[i].Length; j++)
                {
                    if (counter < names.Count)
                        keyboard[i][j] = new KeyboardButton("-");
                    counter++;
                }
            }

            for (int i = 0; i < names.Count; i++)
                keyboard[i / x][i % x] = names[i];

            return new ReplyKeyboardMarkup(keyboard) { ResizeKeyboard = true, OneTimeKeyboard = isOneTimeKeyboard };
        }

        public static InlineKeyboardMarkup InlineKeyboardOptimizer(List<CallbackModel> names, int columnCounter = 2)
        {
            int x = columnCounter < 2 ? 2 : columnCounter;
            int y = (int)Math.Ceiling((double)(names.Count) / x);
            int counter = 0;

            InlineKeyboardButton[][] keyboard = new InlineKeyboardButton[y][];

            for (int i = 0; i < keyboard.Length; i++)
            {
                if (i + 1 == keyboard.Length)
                    keyboard[i] = new InlineKeyboardButton[names.Count - counter];
                else
                    keyboard[i] = new InlineKeyboardButton[x];

                for (int j = 0; j < keyboard[i].Length; j++)
                {
                    if (counter < names.Count)
                        keyboard[i][j] = new InlineKeyboardButton("-");
                    counter++;
                }
            }

            for (int i = 0; i < names.Count; i++)
            {
                keyboard[i / x][i % x].Text = names[i].Text;
                keyboard[i / x][i % x].CallbackData = names[i].CallbackData;
            }

            return new InlineKeyboardMarkup(keyboard);
        }

        public static string GetDisplayName(this Enum enumValue)
        {
            return enumValue.GetType()
                            .GetMember(enumValue.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()
                            .GetName();
        }

        public static string GetDisplayShortName(this Enum enumValue)
        {
            return enumValue.GetType()
                            .GetMember(enumValue.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()
                            .GetShortName();
        }

        public static List<string> LanguagesWithFlags()
        {
            List<string> result = new();

            foreach (Languages l in Enum.GetValues(typeof(Languages)))
                result.Add($"{l} {l.GetDisplayShortName()}");

            return result;
        }

        public static List<string> GetAllAvailableLanguages()
        {
            List<string> result = new();

            foreach (var l in Enum.GetNames(typeof(Languages)))
                result.Add(l);
            return result;
        }

        public static List<string> GetAllAvailableLanguagesDisplayName()
        {
            List<string> result = new();

            foreach (Languages l in Enum.GetValues(typeof(Languages)))
                result.Add(l.GetDisplayName());

            return result;
        }

        public static string GetCulture(this string flag)
        {
            foreach (Languages l in Enum.GetValues(typeof(Languages)))
                if (l.GetDisplayShortName() == flag)
                    return l.GetDisplayName();

            return null;
        }

        public static Languages Language(this string flag)
        {
            foreach (Languages l in Enum.GetValues(typeof(Languages)))
                if (l.GetDisplayName() == flag)
                    return l;

            return Constants.Languages.English;
        }

        public static List<string> GetMenuButtons()
        {
            return new List<string>()
            {
                Resources.Resources.workCommandDescription,
                Resources.Resources.petCommandDescription,
                Resources.Resources.kitchenCommandDescription,

                Resources.Resources.sleepCommandDescription,
                Resources.Resources.gameroomCommandDescription,
                Resources.Resources.bathroomCommandDescription,

                Resources.Resources.ranksCommandDescription,
                Resources.Resources.referalCommandDescription,
                Resources.Resources.menuCommandDescription,
            };
        }

        public static List<BotCommand> GetCommands(bool showAllCommands = true)
        {
            List<BotCommand> result = new()
            {
                new BotCommand()
                {
                    Command = Commands.LanguageCommand,
                    Description = Resources.Resources.languageCommandDescription
                }
            };

            if (!showAllCommands)
                return result;

            List<BotCommand> resultExtra = new()
            {
                new BotCommand()
                {
                    Command = Commands.PetCommand,
                    Description = Resources.Resources.petCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.KitchenCommand,
                    Description = Resources.Resources.kitchenCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.WorkCommand,
                    Description = Resources.Resources.workCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.SleepCommand,
                    Description = Resources.Resources.sleepCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.GameroomCommand,
                    Description = Resources.Resources.gameroomCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.BathroomCommand,
                    Description = Resources.Resources.bathroomCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.RanksCommand,
                    Description = Resources.Resources.ranksCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.HospitalCommand,
                    Description = Resources.Resources.hospitalCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.RewardCommand,
                    Description = Resources.Resources.rewardCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.ReferalCommand,
                    Description = Resources.Resources.referalCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.MenuCommand,
                    Description = Resources.Resources.menuCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.HelpCommand,
                    Description = Resources.Resources.helpCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.ChangelogCommand,
                    Description = Resources.Resources.changelogCommandDescription
                }
            };

            resultExtra.AddRange(result);

            return resultExtra;
        }
        public static List<BotCommand> GetCommandsAdmin(bool showAllCommands = true)
        {
            List<BotCommand> result = new()
            {
                new BotCommand()
                {
                    Command = Commands.LanguageCommand,
                    Description = Resources.Resources.languageCommandDescription
                }
            };

            if (!showAllCommands)
                return result;

            List<BotCommand> resultExtra = new()
            {
                new BotCommand()
                {
                    Command = Commands.PetCommand,
                    Description = Resources.Resources.petCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.KitchenCommand,
                    Description = Resources.Resources.kitchenCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.WorkCommand,
                    Description = Resources.Resources.workCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.SleepCommand,
                    Description = Resources.Resources.sleepCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.GameroomCommand,
                    Description = Resources.Resources.gameroomCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.BathroomCommand,
                    Description = Resources.Resources.bathroomCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.RanksCommand,
                    Description = Resources.Resources.ranksCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.HospitalCommand,
                    Description = Resources.Resources.hospitalCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.RewardCommand,
                    Description = Resources.Resources.rewardCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.ReferalCommand,
                    Description = Resources.Resources.referalCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.MenuCommand,
                    Description = Resources.Resources.menuCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.HelpCommand,
                    Description = Resources.Resources.helpCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.ChangelogCommand,
                    Description = Resources.Resources.changelogCommandDescription
                }
            };
            List<BotCommand> resultAdmin = new List<BotCommand>()
            {
                new BotCommand()
                {
                    Command = Commands.CheckCommand,
                    Description = "Stats check"
                },
                new BotCommand()
                {
                    Command = Commands.GoldCommand,
                    Description = "Add gold [amount]"
                },
                new BotCommand()
                {
                    Command = Commands.KillCommand,
                    Description = "Sets HP to zero"
                },
                new BotCommand()
                {
                    Command = Commands.StartBotstatCheckCommand,
                    Description = "Check on Botstat"
                },
                new BotCommand()
                {
                    Command = Commands.StatusBotstatCheckCommand,
                    Description = "Status of Botstat check"
                },
                new BotCommand()
                {
                    Command = Commands.RestartCommand,
                    Description = "Deletes user from DB"
                }
            };
            resultExtra.AddRange(result);
            resultAdmin.AddRange(resultExtra);
            return resultAdmin;
        }

        public static List<BotCommand> GetMultiplayerCommands()
        {
            List<BotCommand> result = new()
            {
                new BotCommand()
                {
                    Command = CommandsMP.ShowPetCommand,
                    Description = Resources.Resources.ShowPetMPCommand
                },
                new BotCommand()
                {
                    Command = CommandsMP.StartDuelCommand,
                    Description = Resources.Resources.StartDuelMPCommand
                },
                new BotCommand()
                {
                    Command = CommandsMP.FeedMPCommand,
                    Description = Resources.Resources.FeedMPCommand
                },
                new BotCommand()
                {
                    Command = CommandsMP.ShowChatRanksMPCommand,
                    Description = Resources.Resources.ShowChatRanksMPCommand
                }
            };

            return result;
        }
        public static List<BotCommand> GetInApplegameCommands()
        {
            List<BotCommand> result = new()
            {
                new BotCommand()
                {
                    Command = Commands.QuitCommand,
                    Description = Resources.Resources.quitCommandDescription
                }
            };

            return result;
        }

        public static string GetFatigue(int fatigue)
        {
            if (fatigue is >= 0 and < 20)
                return Resources.Resources.FatigueFresh;

            if (fatigue is >= 20 and < 40)
                return Resources.Resources.FatigueRested;

            if (fatigue is >= 40 and < 60)
                return Resources.Resources.FatigueSlightlyTired;

            if (fatigue is >= 60 and < 80)
                return Resources.Resources.FatigueTired;

            if (fatigue >= 80)
                return Resources.Resources.FatigueSleepy;

            return Resources.Resources.FatigueSleepy;
        }
        public static string GetCurrentStatus(int status)
        {
            //var statusEnum = Enum.GetValues(typeof(CurrentStatus)).Cast<CurrentStatus>().FirstOrDefault(s => (int)s == status);
            if (Enum.IsDefined(typeof(CurrentStatus), status))
            {
                var statusEnum = (CurrentStatus)status;
                return statusEnum switch
                {
                    CurrentStatus.Active => Resources.Resources.CurrentStatusActive,
                    CurrentStatus.Sleeping => Resources.Resources.CurrentStatusSleeping,
                    CurrentStatus.Working => Resources.Resources.CurrentStatusWorking,
                    _ => null,
                };
            }
            else
                return null;
        }

        public static AdsProducers GetAdsProducerFromStart(string command)
        {
            if (command == null || !command.Contains("myref_1_"))
                return null;

            //    /start myref_1_ANONmyref_1_GramAdsRobot

            var dividedCommand = command.Replace(" ", "").Split("myref_1_");

            if (dividedCommand.Length > 1 && dividedCommand[0] == "/start")
            {
                if (dividedCommand.Length == 2)
                    return new AdsProducers()
                    {
                        ProducerName = dividedCommand[1],
                        CompanyName = "GramadsNet"
                    };

                return new AdsProducers()
                {
                    CompanyName = dividedCommand[1],
                    ProducerName = dividedCommand[2]
                };
            }
            return null;
        }
        public static long GetReferalProducerFromStart(string command)
        {
            if (command == null || !command.Contains(" kotik_"))
                return -1;

            var dividedCommand = command.Split(" kotik_");

            if (dividedCommand.Length > 1 && dividedCommand[0] == "/start")
            {
                var pureAds = dividedCommand[1];
                if (long.TryParse(pureAds, out var result))
                    return result;
            }
            return -1;
        }
        public static string GetLogUser(Models.Mongo.User user)
        {
            if (user == null)
                return "DELETED";

            if (user.Username != null)
                return $"|@{user.Username}, ID: {user.UserId}|";

            if (user.FirstName != null & user.LastName != null)
                return $"|{user.FirstName} {user.LastName}, ID: {user.UserId}|";

            if (user.FirstName != null)
                return $"|{user.FirstName}, ID: {user.UserId}|";

            if (user.UserId != 0 && user.Id == default)
                return $"|ID: {user.UserId} MP|";

            return $"|ID: {user.UserId}|";
        }

        public static string GetPersonalLink(long userId, string userName)
        {
            string personalLink = "<a href=\"tg://user?id=`userId`\">`username`</a>";
            personalLink = personalLink.Replace("`userId`", $"{userId}");
            personalLink = personalLink.Replace("`username`", $"{userName}");

            return personalLink;
        }
        public static string GetReferalLink(long userId, string botUsername)
        {
            return $"https://t.me/{botUsername}?start=kotik_" + $"{userId}";
        }
        public static bool IsEqual(this string telegramString, string defaultString)
        {

            string s1 = telegramString.ToLower().Trim();
            s1 = Regex.Replace(s1, "(?<!\r)\n", "\r\n");

            string s2 = defaultString.ToLower().Trim();
            s2 = Regex.Replace(s2, "(?<!\r)\n", "\r\n");

            if (string.Equals(s1, s2, StringComparison.InvariantCultureIgnoreCase))
                return true;
            else
                return false;
        }
        public static bool IsEqual(this BotCommand[] bcFirst, BotCommand[] bcSecond)
        {
            if (bcFirst == null && bcSecond == null)
                return true;

            if (bcFirst == null || bcSecond == null)
                return false;

            if (bcFirst.Length != bcSecond.Length)
                return false;

            for (int i = 0; i < bcFirst.Length; i++)
            {
                if (bcFirst[i].Description != bcSecond[i].Description
                    || bcFirst[i].Command != bcSecond[i].Command)
                    return false;
            }

            return true;
        }
        public static List<int> ParseString(List<string> toParse)
        {
            List<int> result = new List<int> ();

            if (toParse == null || toParse.Count == 0)
                return result;

            foreach (string s in toParse)
            {
                if (int.TryParse(s, out int parsedInt))
                    result.Add(parsedInt);
            }
            return result;
        }
        public static string UseCulture(this string toTranslate, CultureInfo culture)
        {
            if (culture == null || string.IsNullOrEmpty(toTranslate))
                return null;

            return Resources.Resources.ResourceManager.GetString(toTranslate, culture);
        }
        public static string UseCulture(this string toTranslate, string culture)
        {
            if (string.IsNullOrEmpty(culture) || string.IsNullOrEmpty(toTranslate))
                return null;

            CultureInfo cultureInfo;
            try
            {
                cultureInfo = CultureInfo.GetCultureInfo(culture);
            }
            catch (Exception e)
            {
                Log.Error(e, "ResourceManager");
                return null;
            }

            return toTranslate.UseCulture(cultureInfo);
        }
    }
}
