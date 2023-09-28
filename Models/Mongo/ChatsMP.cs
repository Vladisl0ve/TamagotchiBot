using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace TamagotchiBot.Models.Mongo
{
    public class ChatsMP
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [BsonElement("ChatId")]
        public long ChatId { get; set; }

        [BsonElement("Name")]
        public string Name { get; set; }

    }
}
