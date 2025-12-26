using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;

namespace TamagotchiBot.Models.Mongo
{
    public class MongoModelBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [BsonElement("Created")]
        public DateTime Created { get; set; } = DateTime.UtcNow;

        [BsonElement("Updated")]
        public DateTime Updated { get; set; } = DateTime.UtcNow;
    }
}
