using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using static TamagotchiBot.UserExtensions.Constants;

namespace TamagotchiBot.UserExtensions
{
    public static class Extensions
    {
        public static ReplyKeyboardMarkup ReplyKeyboardOptimizer(List<string> names, int columnCounter = 2)
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
                        keyboard[i][j] = new KeyboardButton();
                    counter++;
                }
            }

            for (int i = 0; i < names.Count; i++)
                keyboard[i / x][i % x] = names[i];

            return new ReplyKeyboardMarkup(keyboard) { ResizeKeyboard = true };
        }

        public static InlineKeyboardMarkup InlineKeyboardOptimizer(List<Tuple<string, string>> names, int columnCounter = 2)
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
                        keyboard[i][j] = new InlineKeyboardButton();
                    counter++;
                }
            }

            for (int i = 0; i < names.Count; i++)
            {
                keyboard[i / x][i % x].Text = names[i].Item1;
                keyboard[i / x][i % x].CallbackData = names[i].Item2;
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
            List<string> result = new List<string>();

            foreach (Language l in Enum.GetValues(typeof(Language)))
                result.Add($"{l} {l.GetDisplayShortName()}");

            return result;
        }

        public static List<string> Languages()
        {
            List<string> result = new List<string>();

            foreach (var l in Enum.GetNames(typeof(Language)))
                result.Add(l);
            return result;
        }

        public static string Culture(this string flag)
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
                            return Constants.Language.Русский;*/

            return Constants.Language.English;
        }

        public static string GetFatigue(int fatigue)
        {
            if (fatigue >= 0 && fatigue < 20)
                return Resources.Resources.FatigueFresh;

            if (fatigue >= 20 && fatigue < 40)
                return Resources.Resources.FatigueRested;

            if (fatigue >= 40 && fatigue < 60)
                return Resources.Resources.FatigueSlightlyTired;

            if (fatigue >= 60 && fatigue < 80)
                return Resources.Resources.FatigueTired;

            if (fatigue >= 80)
                return Resources.Resources.FatigueSleepy;

            return Resources.Resources.FatigueSleepy;
        }

    }
}
