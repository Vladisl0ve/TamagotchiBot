using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace TamagotchiBot.Models.Mongo
{
    public class ServiceInfo
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId _id { get; set; }

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

        [BsonElement("BannedWords")]
        public List<string> BannedWords { get; set; }
    }
}
