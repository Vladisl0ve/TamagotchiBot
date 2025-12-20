using TamagotchiBot.Database;

namespace TamagotchiBot.UserExtensions
{
    public class GlobalConfig
    {
        public IEnvsSettings EnvsSettings { get; set; }
        public ITamagotchiDatabaseSettings TamagotchiDatabaseSettings { get; set; }
    }
}
