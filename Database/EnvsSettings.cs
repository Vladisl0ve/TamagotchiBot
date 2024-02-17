using System;
using System.Collections.Generic;

namespace TamagotchiBot.Database
{
    public class EnvsSettings : IEnvsSettings
    {
        public List<string> AlwaysNotifyUsers   { get; set; }
        public List<string> ChatsToDevNotify    { get; set; }
        public List<string> BannedRenamingUsers { get; set; }
        public TimeSpan NotifyEvery             { get; set; }
        public TimeSpan DevNotifyEvery          { get; set; }
        public TimeSpan DevExtraNotifyEvery     { get; set; }
        public TimeSpan TriggersEvery           { get; set; }
        public TimeSpan AwakeWhenAFKFor         { get; set; }
        public string TokenBot                  { get; set; }
        public string BotstatApiKey             { get; set; }
        public string OpenAiApiKey              { get; set; }
        public long   ChatToForwardId            { get; set; }
    }

    public interface IEnvsSettings
    {
        List<string> AlwaysNotifyUsers   { get; set; }
        List<string> ChatsToDevNotify    { get; set; }
        List<string> BannedRenamingUsers { get; set; }
        TimeSpan NotifyEvery             { get; set; }
        TimeSpan DevNotifyEvery          { get; set; }
        TimeSpan DevExtraNotifyEvery     { get; set; }
        TimeSpan TriggersEvery           { get; set; }
        TimeSpan AwakeWhenAFKFor         { get; set; }
        string   TokenBot                { get; set; }
        string   BotstatApiKey           { get; set; }
        string   OpenAiApiKey            { get; set; }
        long     ChatToForwardId          { get; set; }
    }
}
