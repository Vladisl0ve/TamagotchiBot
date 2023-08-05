using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

        public static InlineKeyboardMarkup InlineKeyboardOptimizer(List<CommandModel> names, int columnCounter = 2)
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

            foreach (Language l in Enum.GetValues(typeof(Language)))
                result.Add($"{l} {l.GetDisplayShortName()}");

            return result;
        }

        public static List<string> Languages()
        {
            List<string> result = new();

            foreach (var l in Enum.GetNames(typeof(Language)))
                result.Add(l);
            return result;
        }

        public static string GetCulture(this string flag)
        {
            foreach (Language l in Enum.GetValues(typeof(Language)))
                if (l.GetDisplayShortName() == flag)
                    return l.GetDisplayName();

            return null;
        }

        public static Language Language(this string flag)
        {
            foreach (Language l in Enum.GetValues(typeof(Language)))
                if (l.GetDisplayName() == flag)
                    return l;

            /*            if (flag == "be" || flag == "by")
                            return Constants.Language.Беларуская;

                        if (flag == "ru")
                            return Constants.Language.Русский;


                        if (flag == "pl")
                            return Constants.Language.Русский;
            */

            return Constants.Language.English;
        }

        public static List<BotCommand> GetCommands(bool showAllCommands = true)
        {
            List<BotCommand> result = new()
            {
                new BotCommand()
                {
                    Command = Commands.LanguageCommand,
                    Description = Resources.Resources.languageCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.HelpCommand,
                    Description = Resources.Resources.helpCommandDescription
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
                    Command = Commands.BathroomCommand,
                    Description = Resources.Resources.bathroomCommandDescription
                },

                new BotCommand()
                {
                    Command = Commands.MenuCommand,
                    Description = Resources.Resources.menuCommandDescription
                }
            };

            resultExtra.AddRange(result);

            return resultExtra;
        }

        public static List<BotCommand> GetIngameCommands()
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
                    CurrentStatus.WorkingOnPC => Resources.Resources.CurrentStatusWorkingOnPC,
                    _ => null,
                };
            }
            else
                return null;
        }

        public static AdsProducers GetAdsProducerFromStart(string command)
        {
            if (command == null)
                return null;

            var dividedCommand = command.Split(new char[] {'_', ' ' });

            if (dividedCommand.Length > 1 && dividedCommand[0] == "/start")
            {
                var pureAds = dividedCommand.Skip(1);
                return new AdsProducers()
                {
                    ProducerName = pureAds.Last(),
                    CompanyName = string.Join(" ", pureAds.SkipLast(1))
                };
            }
            return null;
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

    }
}
