using System.Collections.Generic;
using System.Globalization;
using TamagotchiBot.UserExtensions;
using static TamagotchiBot.Resources.Resources;

namespace TamagotchiBot.Controllers
{
    public abstract class ControllerBase
    {
        public List<string> GetAllTranslatedAndLowered(string text)
        {
            List<string> translated = new List<string>();
            foreach (var culture in Extensions.GetAllAvailableLanguagesDisplayName())
                translated.Add(ResourceManager.GetString(text, new CultureInfo(culture))?.ToLower());

            return translated;
        }
    }
}
