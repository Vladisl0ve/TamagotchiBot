using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;

namespace MaintanceAlertBot.Models.Mongo
{
    public class MongoModelBase
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [BsonElement("Created")]
        public DateTime Created { get; set; }

        [BsonElement("Updated")]
        public DateTime Updated { get; set; }
    }
}
