using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace TamagotchiBot.Models.Mongo
{
    public class BannedUsers : MongoModelBase
    {
        [BsonElement("UserId")]
        public long UserId { get; set; }

        [BsonElement("IsRenameBanned")]
        public bool IsRenameBanned { get; set; }
    }
}
