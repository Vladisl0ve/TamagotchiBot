using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace TamagotchiBot.Models.Mongo
{
    public class BannedUsers
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [BsonElement("UserId")]
        public long UserId { get; set; }

        [BsonElement("IsRenameBanned")]
        public bool IsRenameBanned { get; set; }
    }
}
