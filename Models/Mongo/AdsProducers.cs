using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace TamagotchiBot.Models.Mongo
{
    public class AdsProducers
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [BsonElement("CompanyName")]
        public string CompanyName { get; set; }

        [BsonElement("ProducerName")]
        public string ProducerName { get; set; }

        [BsonElement("Counter")]
        public int Counter { get; set; }
    }
}
