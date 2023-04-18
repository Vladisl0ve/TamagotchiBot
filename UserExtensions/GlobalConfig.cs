using System;
using System.Collections.Generic;
using TamagotchiBot.Database;

namespace TamagotchiBot.UserExtensions
{
    public class GlobalConfig
    {
        public string TokenBot { get; set; }
        public IEnvsSettings EnvsSettings { get; set; }
        public ITamagotchiDatabaseSettings TamagotchiDatabaseSettings { get; set; }
    }
}
