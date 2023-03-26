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

        [BsonElement("IsChangelogsSent")]
        public bool IsChangelogsSent { get; set; }
    }
}
