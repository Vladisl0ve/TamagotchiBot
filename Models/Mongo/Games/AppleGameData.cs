using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TamagotchiBot.Models.Mongo.Games
{
    public class AppleGameData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [BsonElement("UserId")]
        public long UserId { get; set; }

        [BsonElement("IsGameOvered")]
        public bool IsGameOvered { get; set; }

        [BsonElement("CurrentAppleCounter")]
        public int CurrentAppleCounter { get; set; }

        [BsonElement("TotalWins")]
        public int TotalWins { get; set; }

        [BsonElement("TotalLoses")]
        public int TotalLoses { get; set; }

        [BsonElement("TotalDraws")]
        public int TotalDraws { get; set; }
    }
}
