using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Collections.Generic;

namespace TamagotchiBot.Models.Mongo
{
    public class ReferalInfo : MongoModelBase
    {
        [BsonElement("CreatorUserId")]
        public long CreatorUserId { get; set; }

        [BsonElement("RefUsers")]
        public List<ReferalUserModel> RefUsers { get; set; }
    }
}
