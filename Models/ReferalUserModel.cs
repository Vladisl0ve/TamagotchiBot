using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
