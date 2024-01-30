using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TamagotchiBot.Models
{
    public class DuelResultModel
    {
        [BsonElement("AttackerUserId")]
        public long AttackerUserId { get; set; }

        [BsonElement("DefenderUserId")]
        public long DefenderUserId { get; set; }

        [BsonElement("WinnerUserId")]
        public long WinnerUserId { get; set; }

        [BsonElement("Revision")]
        public DateTime Revision { get; set; }
    }
}
