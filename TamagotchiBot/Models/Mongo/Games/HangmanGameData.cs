using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Collections.Generic;

namespace TamagotchiBot.Models.Mongo.Games
{
    public class HangmanGameData : MongoModelBase
    {
        [BsonElement("UserId")]
        public long UserId { get; set; }

        [BsonElement("Word")]
        public string Word { get; set; }

        [BsonElement("GuessedLetters")]
        public List<char> GuessedLetters { get; set; }

        [BsonElement("IsGameOver")]
        public bool IsGameOver { get; set; }

        [BsonElement("TotalWins")]
        public int TotalWins { get; set; }

        [BsonElement("TotalLoses")]
        public int TotalLoses { get; set; }
    }
}
