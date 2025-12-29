using MongoDB.Bson.Serialization.Attributes;
using System;
using static TamagotchiBot.UserExtensions.Constants;

namespace TamagotchiBot.Models.Mongo
{
    public class BonusCode : MongoModelBase
    {
        [BsonElement("CodeName")]
        public string CodeName { get; set; }

        [BsonElement("CodeValue")]
        public string CodeValue { get; set; }

        [BsonElement("ExpirationDateTime")]
        public DateTime ExpirationDateTime { get; set; }

        [BsonElement("Type")]
        public BonusType Type { get; set; }
    }
}
