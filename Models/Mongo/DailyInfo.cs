using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TamagotchiBot.Models.Mongo
{
    public class DailyInfo
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [BsonElement("DateInfo")]
        public DateTime DateInfo { get; set; }

        [BsonElement("UsersPlayed")]
        public int UsersPlayed { get; set; }

        [BsonElement("MessagesSent")]
        public long MessagesSent { get; set; }

        [BsonElement("CallbacksSent")]
        public long CallbacksSent { get; set; }

        [BsonElement("TodayMessages")]
        public int TodayMessages { get; set; }

        [BsonElement("TodayCallbacks")]
        public int TodayCallbacks { get; set; }
    }
}
