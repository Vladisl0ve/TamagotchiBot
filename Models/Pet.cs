using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TamagotchiBot.Models
{
    public class Pet
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("UserId")]
        public long UserId { get; set; }

        [BsonElement("Name")]
        public string Name { get; set; }

        [BsonElement("Type")]
        public string Type { get; set; }

        [BsonElement("BirthDateTime")]
        public DateTime BirthDateTime { get; set; }

        [BsonElement("LastUpdateTime")]
        public DateTime LastUpdateTime { get; set; }

        [BsonElement("HP")]
        public int HP { get; set; }

        [BsonElement("Starving")]
        public int Starving { get; set; }

        [BsonElement("Joy")]
        public int Joy { get; set; }

        [BsonElement("EXP")]
        public int EXP { get; set; }

        [BsonElement("Level")]
        public int Level { get; set; }

    }
}
