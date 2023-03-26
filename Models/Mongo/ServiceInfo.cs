using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

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

        [BsonElement("DoSendChangelogs")]
        public bool DoSendChangelogs { get; set; }
    }
}
