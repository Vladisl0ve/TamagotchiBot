using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;
using static TamagotchiBot.Extensions.Constants;

namespace TamagotchiBot.Extensions
{
    public static class Extensions
    {
        public static ReplyKeyboardMarkup KeyboardOptimizer(List<string> names)
        {
            int x = 2;
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

    }
}
