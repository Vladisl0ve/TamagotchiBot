using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace TamagotchiBot.Models.Mongo.Games
{
    public class TicTacToeGameData : MongoModelBase
    {
        [BsonElement("UserId")]
        public long UserId { get; set; }

        [BsonElement("Board")]
        public string Board { get; set; } // "XXXXXXXXX" where X is 0 (empty), 1 (X - user), 2 (O - bot)

        [BsonElement("IsGameOver")]
        public bool IsGameOver { get; set; }

        [BsonElement("CurrentTurn")]
        public int CurrentTurn { get; set; } // 1 for User (X), 2 for Bot (O)

        [BsonElement("TotalWins")]
        public int TotalWins { get; set; }

        [BsonElement("TotalLoses")]
        public int TotalLoses { get; set; }

        [BsonElement("TotalDraws")]
        public int TotalDraws { get; set; }
    }
}
