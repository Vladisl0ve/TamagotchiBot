using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace TamagotchiBot.Models.Mongo
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [BsonElement("UserId")]
        public long UserId { get; set; }

        [BsonElement("ChatId")]
        public long ChatId { get; set; }

        [BsonElement("Username")]
        public string Username { get; set; }
        [BsonElement("FirstName")]
        public string FirstName { get; set; }
        [BsonElement("LastName")]
        public string LastName { get; set; }
        [BsonElement("IsInAppleGame")]
        public bool IsInAppleGame { get; set; }
        [BsonElement("Culture")]
        public string Culture { get; set; }
        [BsonElement("NextDailyRewardNotificationTime")]
        public DateTime NextDailyRewardNotificationTime { get; set; }
    }
}
