using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TamagotchiBot.Models.Mongo
{
    public class Chat
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("UserId")]
        public long UserId { get; set; }

        [BsonElement("Name")]
        public string Name { get; set; }

        [BsonElement("LastMessage")]
        public string LastMessage { get; set; }

        [BsonElement("ChatId")]
        public long ChatId { get; set; }
    }
}
