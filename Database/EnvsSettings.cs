using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TamagotchiBot.Database
{
    public class EnvsSettings : IEnvsSettings
    {
        public List<string> AlwaysNotifyUsers { get; set; }
        public List<string> ChatsToDevNotify { get; set; }

    }

    public interface IEnvsSettings
    {
        List<string> AlwaysNotifyUsers { get; set; }
        List<string> ChatsToDevNotify { get; set; }

    }
}
