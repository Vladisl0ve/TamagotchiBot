using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace TamagotchiBot.Models.Mongo
{
    public class User : MongoModelBase
    {
        [BsonElement("UserId")]
        public long UserId { get; set; }

        [BsonElement("ChatIds")]
        public List<long> ChatIds { get; set; }

        [BsonElement("Username")]
        public string Username { get; set; }

        [BsonElement("FirstName")]
        public string FirstName { get; set; }

        [BsonElement("LastName")]
        public string LastName { get; set; }

        [BsonElement("IsInAppleGame")]
        public bool IsInAppleGame { get; set; }

        [BsonElement("IsLanguageAskedOnCreate")]
        public bool IsLanguageAskedOnCreate { get; set; }

        [BsonElement("IsPetNameAskedOnCreate")]
        public bool IsPetNameAskedOnCreate { get; set; }      

        [BsonElement("Culture")]
        public string Culture { get; set; }

        [BsonElement("NextDailyRewardNotificationTime")]
        public DateTime NextDailyRewardNotificationTime { get; set; }

        [BsonElement("GotDailyRewardTime")]
        public DateTime GotDailyRewardTime { get; set; }

        [BsonElement("Gold")]
        public int Gold { get; set; } 
        
        [BsonElement("ReferaledBy")]
        public long ReferaledBy { get; set; }

        public static User Clone(User userToClone)
        {
            var clone = new User()
            {
                UserId = userToClone.UserId,
                LastName = userToClone.LastName,
                FirstName = userToClone.FirstName,
                ChatIds = userToClone.ChatIds,
                Culture = userToClone.Culture,
                Id = userToClone.Id,
                IsInAppleGame = userToClone.IsInAppleGame,
                IsLanguageAskedOnCreate = userToClone.IsLanguageAskedOnCreate,
                IsPetNameAskedOnCreate = userToClone.IsPetNameAskedOnCreate,
                NextDailyRewardNotificationTime = userToClone.NextDailyRewardNotificationTime,
                Username = userToClone.Username,
                Gold = userToClone.Gold,
            };

            return clone;
        }
    }
}
