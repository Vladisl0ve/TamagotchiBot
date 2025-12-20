using MongoDB.Bson.Serialization.Attributes;
using System;

namespace TamagotchiBot.Models.Mongo
{
    public class ArchiveUserInfo : MongoModelBase
    {
        [BsonElement("UserId")]
        public long UserId { get; set; }

        [BsonElement("Exp")]
        public int Exp { get; set; }

        [BsonElement("SnapshotDate")]
        public DateTime SnapshotDate { get; set; }
    }
}
