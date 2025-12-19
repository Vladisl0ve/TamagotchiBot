using MongoDB.Bson.Serialization.Attributes;

namespace TamagotchiBot.Models
{
    public class ReferalUserModel
    {
        [BsonElement("RefUserId")]
        public long RefUserId { get; set; }

        [BsonElement("IsTaskDone")]
        public bool IsTaskDone { get; set; }
    }
}
