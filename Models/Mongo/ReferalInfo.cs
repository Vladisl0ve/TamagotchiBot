using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TamagotchiBot.Models.Mongo
{
    public class ReferalInfo
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [BsonElement("CreatorUserId")]
        public long CreatorUserId { get; set; }

        [BsonElement("RefUsers")]
        public List<ReferalUserModel> RefUsers { get; set; }
    }
}
