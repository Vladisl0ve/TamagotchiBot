using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TamagotchiBot.Services
{
    public class AllUsersData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [BsonElement("UserId")]
        public long UserId { get; set; }

        [BsonElement("Username")]
        public string Username { get; set; }

        [BsonElement("ChatId")]
        public long ChatId { get; set; }

        [BsonElement("LastMessage")]
        public string LastMessage { get; set; }

        [BsonElement("Culture")]
        public string Culture { get; set; }

        [BsonElement("MessageCounter")]
        public long MessageCounter { get; set; }

        [BsonElement("CallbacksCounter")]
        public long CallbacksCounter { get; set; }

        [BsonElement("BreadEatenCounter")]
        public long BreadEatenCounter { get; set; }

        [BsonElement("AppleEatenCounter")]
        public long AppleEatenCounter { get; set; }

        [BsonElement("ChocolateEatenCounter")]
        public long ChocolateEatenCounter { get; set; }

        [BsonElement("LollypopEatenCounter")]
        public long LollypopEatenCounter { get; set; }

        [BsonElement("SleepenTimesCounter")]
        public long SleepenTimesCounter { get; set; }

        [BsonElement("DicePlayedCounter")]
        public long DicePlayedCounter { get; set; }

        [BsonElement("CardsPlayedCounter")]
        public long CardsPlayedCounter { get; set; }

        [BsonElement("PillEatenCounter")]
        public long PillEatenCounter { get; set; }

        [BsonElement("Created")]
        public DateTime Created { get; set; }
        
        [BsonElement("Updated")]
        public DateTime Updated { get; set; }


    }
}
