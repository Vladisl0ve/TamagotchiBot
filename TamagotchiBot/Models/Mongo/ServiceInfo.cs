using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace TamagotchiBot.Models.Mongo
{
    public class ServiceInfo : MongoModelBase
    {
        [BsonElement("LastGlobalUpdate")]
        public DateTime LastGlobalUpdate { get; set; }

        [BsonElement("NextNotify")]
        public DateTime NextNotify { get; set; }

        [BsonElement("NextDevNotify")]
        public DateTime NextDevNotify { get; set; }

        [BsonElement("DoSendChangelogs")]
        public bool DoSendChangelogs { get; set; }

        [BsonElement("DoMaintainWorks")]
        public bool DoMaintainWorks { get; set; }

        [BsonElement("BotstatCheckId")]
        public string BotstatCheckId { get; set; }
        [BsonElement("OpenAiKey")]
        public string OpenAiKey { get; set; } = string.Empty;

        [BsonElement("BannedWords")]
        public List<string> BannedWords { get; set; }

        [BsonElement("TmgToken")]
        public string TmgToken { get; set; } = string.Empty;

        [BsonElement("GeminiApiKey")]
        public string GeminiApiKey { get; set; } = string.Empty;

        [BsonElement("MaxGeminiHistory")]
        public int MaxGeminiHistory { get; set; } = 10;
    }
}
