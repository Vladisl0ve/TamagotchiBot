using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Collections.Generic;

namespace TamagotchiBot.Models.Mongo
{
    public class ChatsMP : MongoModelBase
    {
        [BsonElement("ChatId")]
        public long ChatId { get; set; }

        [BsonElement("Name")]
        public string Name { get; set; }

        [BsonElement("Culture")]
        public string Culture { get; set; }

        [BsonElement("DuelResults")]
        public List<DuelResultModel> DuelResults { get; set; }
    }
}
