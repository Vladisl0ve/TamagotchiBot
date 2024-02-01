using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;

namespace TamagotchiBot.Models.Mongo
{
    public class DailyInfo : MongoModelBase
    {
        [BsonElement("DateInfo")]
        public DateTime DateInfo { get; set; }

        [BsonElement("UsersPlayed")]
        public int UsersPlayed { get; set; }

        [BsonElement("MessagesSent")]
        public long MessagesSent { get; set; }

        [BsonElement("CallbacksSent")]
        public long CallbacksSent { get; set; }

        [BsonElement("TodayMessages")]
        public int TodayMessages { get; set; }

        [BsonElement("TodayCallbacks")]
        public int TodayCallbacks { get; set; } 
        
        [BsonElement("PetCounter")]
        public long PetCounter { get; set; }   
        
        [BsonElement("UserCounter")]
        public long UserCounter { get; set; }
    }
}
