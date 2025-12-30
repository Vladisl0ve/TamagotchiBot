using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using static TamagotchiBot.UserExtensions.Extensions;

namespace TamagotchiBot.UserExtensions
{
    public static class LanguageExtensions
    {
        /// <summary>
        /// Returns the localized string representation of a time unit, formatted according to the specified culture,
        /// value, and grammatical case.
        /// </summary>
        /// <remarks>Supports English, Russian, Ukrainian, and Belarusian cultures. For unsupported
        /// cultures, English localization is used. Pluralization and grammatical case are applied where appropriate for
        /// Slavic languages.</remarks>
        /// <param name="cultureInfo">The culture information used to determine the language and formatting of the time unit. If null, English is
        /// used by default.</param>
        /// <param name="unit">The time unit to be localized, such as day, hour, or minute.</param>
        /// <param name="value">The numeric value associated with the time unit, used to determine the correct pluralization form.</param>
        /// <param name="padezh">The grammatical case to use for the localized unit. This parameter is relevant for languages that require
        /// case inflection, such as Russian. Defaults to nominative case.</param>
        /// <returns>A localized string representing the time unit in the appropriate form for the specified culture and value.
        /// Returns an empty string if the unit is not recognized.</returns>
        public static string GetLocalizedUnit(
            CultureInfo cultureInfo,
            TimeUnit unit,
            int value,
            Padezh padezh = Padezh.Imenitelny)
        {
            var lang = cultureInfo?.TwoLetterISOLanguageName?.ToLowerInvariant() ?? "en";

            return lang switch
            {
                "ru" => GetSlavicForm(
                    padezh == Padezh.Roditelny
                        ? GetRussianGenitive(unit)
                        : GetRussianNominative(unit),
                    value),

                "uk" => GetSlavicForm(
                    padezh == Padezh.Roditelny
                        ? GetUkrainianGenitive(unit)
                        : GetUkrainianNominative(unit),
                    value),

                "be" => GetSlavicForm(
                    padezh == Padezh.Roditelny
                        ? GetBelarusianGenitive(unit)
                        : GetBelarusianNominative(unit),
                    value),

                _ => unit switch // EN (default)
                {
                    TimeUnit.Day => value == 1 ? "day" : "days",
                    TimeUnit.Hour => value == 1 ? "hour" : "hours",
                    TimeUnit.Minute => value == 1 ? "minute" : "minutes",
                    _ => ""
                }
            };
        }

        private static (string one, string few, string many) GetRussianNominative(TimeUnit unit) => unit switch
        {
            TimeUnit.Day => ("день", "дня", "дней"),
            TimeUnit.Hour => ("час", "часа", "часов"),
            TimeUnit.Minute => ("минута", "минуты", "минут"),
            _ => ("", "", "")
        };

        private static (string one, string few, string many) GetRussianGenitive(TimeUnit unit) => unit switch
        {
            TimeUnit.Day => ("день", "дня", "дней"),
            TimeUnit.Hour => ("час", "часа", "часов"),
            TimeUnit.Minute => ("минуту", "минуты", "минут"),
            _ => ("", "", "")
        };

        private static (string one, string few, string many) GetUkrainianNominative(TimeUnit unit) => unit switch
        {
            TimeUnit.Day => ("день", "дні", "днів"),
            TimeUnit.Hour => ("година", "години", "годин"),
            TimeUnit.Minute => ("хвилина", "хвилини", "хвилин"),
            _ => ("", "", "")
        };

        private static (string one, string few, string many) GetUkrainianGenitive(TimeUnit unit) => unit switch
        {
            TimeUnit.Day => ("день", "дні", "днів"),
            TimeUnit.Hour => ("годину", "години", "годин"),
            TimeUnit.Minute => ("хвилину", "хвилини", "хвилин"),
            _ => ("", "", "")
        };

        private static (string one, string few, string many) GetBelarusianNominative(TimeUnit unit) => unit switch
        {
            TimeUnit.Day => ("дзень", "дні", "дзён"),
            TimeUnit.Hour => ("гадзіна", "гадзіны", "гадзін"),
            TimeUnit.Minute => ("хвіліна", "хвіліны", "хвілін"),
            _ => ("", "", "")
        };

        private static (string one, string few, string many) GetBelarusianGenitive(TimeUnit unit) => unit switch
        {
            TimeUnit.Day => ("дзень", "дні", "дзён"),
            TimeUnit.Hour => ("гадзіну", "гадзіны", "гадзін"),
            TimeUnit.Minute => ("хвіліну", "хвіліны", "хвілін"),
            _ => ("", "", "")
        };


    }
}
