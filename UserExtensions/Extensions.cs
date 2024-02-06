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

        public static List<string> GetFarmButtons(CultureInfo culture)
        {
            return new List<string>()
            {
                nameof(Resources.Resources.farmButtonChangeType).UseCulture(culture),
                nameof(Resources.Resources.goAwayButton).UseCulture(culture),
            };
        }
        public static List<string> GetChangeTypeButtons(CultureInfo culture)
        {
            return new List<string>()
            {
                nameof(Resources.Resources.CatTypeText).UseCulture(culture),
                nameof(Resources.Resources.DogTypeText).UseCulture(culture),
                nameof(Resources.Resources.MouseTypeText).UseCulture(culture),
                nameof(Resources.Resources.FoxTypeText).UseCulture(culture),
                nameof(Resources.Resources.PandaTypeText).UseCulture(culture),
                nameof(Resources.Resources.goAwayButton).UseCulture(culture),
            };
        }
        public static List<string> GetMenuButtons(CultureInfo culture)
        {
            return new List<string>()
            {
                nameof(Resources.Resources.workCommandDescription).UseCulture(culture),
                nameof(Resources.Resources.petCommandDescription).UseCulture(culture),
                nameof(Resources.Resources.kitchenCommandDescription).UseCulture(culture),

                nameof(Resources.Resources.sleepCommandDescription).UseCulture(culture),
                nameof(Resources.Resources.gameroomCommandDescription).UseCulture(culture),
                nameof(Resources.Resources.bathroomCommandDescription).UseCulture(culture),

                nameof(Resources.Resources.ranksCommandDescription).UseCulture(culture),
                nameof(Resources.Resources.farmCommandDescription).UseCulture(culture),
                nameof(Resources.Resources.menuCommandDescription).UseCulture(culture),
            };
        }

        public static List<BotCommand> GetCommands(string culture, bool showAllCommands = true)
        {
            List<BotCommand> result = new()
            {
                new BotCommand()
                {
                    Command = Commands.LanguageCommand,
                    Description = nameof(Resources.Resources.languageCommandDescription).UseCulture(culture)
                }
            };

            if (!showAllCommands)
                return result;

            List<BotCommand> resultExtra = new()
            {
                new BotCommand()
                {
                    Command = Commands.PetCommand,
                    Description = nameof(Resources.Resources.petCommandDescription).UseCulture(culture)
                },

                new BotCommand()
                {
                    Command = Commands.KitchenCommand,
                    Description = nameof(Resources.Resources.kitchenCommandDescription).UseCulture(culture)
                },

                new BotCommand()
                {
                    Command = Commands.WorkCommand,
                    Description = nameof(Resources.Resources.workCommandDescription).UseCulture(culture)
                },

                new BotCommand()
                {
                    Command = Commands.SleepCommand,
                    Description = nameof(Resources.Resources.sleepCommandDescription).UseCulture(culture)
                },

                new BotCommand()
                {
                    Command = Commands.GameroomCommand,
                    Description = nameof(Resources.Resources.gameroomCommandDescription).UseCulture(culture)
                },

                new BotCommand()
                {
                    Command = Commands.BathroomCommand,
                    Description = nameof(Resources.Resources.bathroomCommandDescription).UseCulture(culture)
                },

                new BotCommand()
                {
                    Command = Commands.RanksCommand,
                    Description = nameof(Resources.Resources.ranksCommandDescription).UseCulture(culture)
                },

                new BotCommand()
                {
                    Command = Commands.HospitalCommand,
                    Description = nameof(Resources.Resources.hospitalCommandDescription).UseCulture(culture)
                },

                new BotCommand()
                {
                    Command = Commands.RewardCommand,
                    Description = nameof(Resources.Resources.rewardCommandDescription).UseCulture(culture)
                },

                new BotCommand()
                {
                    Command = Commands.ReferalCommand,
                    Description = nameof(Resources.Resources.referalCommandDescription).UseCulture(culture)
                },

                new BotCommand()
                {
                    Command = Commands.MenuCommand,
                    Description = nameof(Resources.Resources.menuCommandDescription).UseCulture(culture)
                },

                new BotCommand()
                {
                    Command = Commands.HelpCommand,
                    Description = nameof(Resources.Resources.helpCommandDescription).UseCulture(culture)
                },

                new BotCommand()
                {
                    Command = Commands.ChangelogCommand,
                    Description = nameof(Resources.Resources.changelogCommandDescription).UseCulture(culture)
                }
            };

            resultExtra.AddRange(result);

            return resultExtra;
        }
        public static List<BotCommand> GetCommandsAdmin(string culture, bool showAllCommands = true)
        {
            List<BotCommand> result = new()
            {
                new BotCommand()
                {
                    Command = Commands.LanguageCommand,
                    Description = nameof(Resources.Resources.languageCommandDescription).UseCulture(culture)
                }
            };

            if (!showAllCommands)
                return result;

            List<BotCommand> resultExtra = new()
            {
                new BotCommand()
                {
                    Command = Commands.PetCommand,
                    Description = nameof(Resources.Resources.petCommandDescription).UseCulture(culture)
                },

                new BotCommand()
                {
                    Command = Commands.KitchenCommand,
                    Description = nameof(Resources.Resources.kitchenCommandDescription).UseCulture(culture)
                },

                new BotCommand()
                {
                    Command = Commands.WorkCommand,
                    Description = nameof(Resources.Resources.workCommandDescription).UseCulture(culture)
                },

                new BotCommand()
                {
                    Command = Commands.SleepCommand,
                    Description = nameof(Resources.Resources.sleepCommandDescription).UseCulture(culture)
                },

                new BotCommand()
                {
                    Command = Commands.GameroomCommand,
                    Description = nameof(Resources.Resources.gameroomCommandDescription).UseCulture(culture)
                },

                new BotCommand()
                {
                    Command = Commands.BathroomCommand,
                    Description = nameof(Resources.Resources.bathroomCommandDescription).UseCulture(culture)
                },

                new BotCommand()
                {
                    Command = Commands.RanksCommand,
                    Description = nameof(Resources.Resources.ranksCommandDescription).UseCulture(culture)
                },

                new BotCommand()
                {
                    Command = Commands.HospitalCommand,
                    Description = nameof(Resources.Resources.hospitalCommandDescription).UseCulture(culture)
                },

                new BotCommand()
                {
                    Command = Commands.RewardCommand,
                    Description = nameof(Resources.Resources.rewardCommandDescription).UseCulture(culture)
                },

                new BotCommand()
                {
                    Command = Commands.ReferalCommand,
                    Description = nameof(Resources.Resources.referalCommandDescription).UseCulture(culture)
                },

                new BotCommand()
                {
                    Command = Commands.MenuCommand,
                    Description = nameof(Resources.Resources.menuCommandDescription).UseCulture(culture)
                },

                new BotCommand()
                {
                    Command = Commands.HelpCommand,
                    Description = nameof(Resources.Resources.helpCommandDescription).UseCulture(culture)
                },

                new BotCommand()
                {
                    Command = Commands.ChangelogCommand,
                    Description = nameof(Resources.Resources.changelogCommandDescription).UseCulture(culture)
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
            resultExtra.AddRange(resultAdmin);
            return resultExtra;
        }

        public static List<BotCommand> GetMultiplayerCommands(string culture)
        {
            List<BotCommand> result = new()
            {
                new BotCommand()
                {
                    Command = CommandsMP.ShowPetCommand,
                    Description = nameof(Resources.Resources.ShowPetMPCommand).UseCulture(culture)
                },
                new BotCommand()
                {
                    Command = CommandsMP.StartDuelCommand,
                    Description = nameof(Resources.Resources.StartDuelMPCommand).UseCulture(culture)
                },
                new BotCommand()
                {
                    Command = CommandsMP.FeedMPCommand,
                    Description = nameof(Resources.Resources.FeedMPCommand).UseCulture(culture)
                },
                new BotCommand()
                {
                    Command = CommandsMP.ShowChatRanksMPCommand,
                    Description = nameof(Resources.Resources.ShowChatRanksMPCommand).UseCulture(culture)
                }
            };

            return result;
        }
        public static List<BotCommand> GetInApplegameCommands(string culture)
        {
            CultureInfo tmpCult;
            try
            {
                tmpCult = new CultureInfo(culture);
            }
            catch
            {
                return new List<BotCommand>();
            }

            return GetInApplegameCommands(tmpCult);
        }
        public static List<BotCommand> GetInApplegameCommands(CultureInfo culture)
        {
            List<BotCommand> result = new()
            {
                new BotCommand()
                {
                    Command = Commands.QuitCommand,
                    Description = nameof(Resources.Resources.quitCommandDescription).UseCulture(culture)
                }
            };

            return result;
        }

        public static string GetFatigue(int fatigue, CultureInfo culture)
        {
            if (fatigue is >= 0 and < 20)
                return nameof(Resources.Resources.FatigueFresh).UseCulture(culture);

            if (fatigue is >= 20 and < 40)
                return nameof(Resources.Resources.FatigueRested).UseCulture(culture);

            if (fatigue is >= 40 and < 60)
                return nameof(Resources.Resources.FatigueSlightlyTired).UseCulture(culture);

            if (fatigue is >= 60 and < 80)
                return nameof(Resources.Resources.FatigueTired).UseCulture(culture);

            if (fatigue >= 80)
                return nameof(Resources.Resources.FatigueSleepy).UseCulture(culture);

            return nameof(Resources.Resources.FatigueSleepy).UseCulture(culture);
        }
        public static string GetCurrentStatus(int status, CultureInfo culture)
        {
            if (Enum.IsDefined(typeof(CurrentStatus), status))
            {
                var statusEnum = (CurrentStatus)status;
                return statusEnum switch
                {
                    CurrentStatus.Active => nameof(Resources.Resources.CurrentStatusActive).UseCulture(culture),
                    CurrentStatus.Sleeping => nameof(Resources.Resources.CurrentStatusSleeping).UseCulture(culture),
                    CurrentStatus.Working => nameof(Resources.Resources.CurrentStatusWorking).UseCulture(culture),
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

            if (user.FirstName != null && user.LastName != null)
                return $"|{user.FirstName} {user.LastName}, ID: {user.UserId}|";

            if (user.FirstName != null)
                return $"|{user.FirstName}, ID: {user.UserId}|";

            if (user.UserId != 0 && user.Id == default)
                return $"|ID: {user.UserId} MP|";

            return $"|ID: {user.UserId}|";
        }
        public static PetType GetEnumPetType(int type)
        {
            if (!Enum.IsDefined(typeof(PetType), type))
                return PetType.UNKNOWN;
            return (PetType)type;
        }
        public static string GetTypeEmoji(int type) => GetTypeEmoji(GetEnumPetType(type));
        public static string GetTypeEmoji(PetType petType)
        {
            return petType switch
            {
                PetType.Cat => "🐱",
                PetType.Dog => "🐶",
                PetType.Mouse => "🐭",
                PetType.Fox => "🦊",
                PetType.Panda => "🐼",
                _ => "🐽"
            };
        }
        public static string GetLongTypeEmoji(PetType petType, CultureInfo culture)
        {
            return petType switch
            {
                PetType.Cat => nameof(Resources.Resources.CatTypeText).UseCulture(culture),
                PetType.Dog => nameof(Resources.Resources.DogTypeText).UseCulture(culture),
                PetType.Mouse => nameof(Resources.Resources.MouseTypeText).UseCulture(culture),
                PetType.Fox => nameof(Resources.Resources.FoxTypeText).UseCulture(culture),
                PetType.Panda => nameof(Resources.Resources.PandaTypeText).UseCulture(culture),
                _ => "⭕️"
            };
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
