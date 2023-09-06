using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TamagotchiBot.Models.Mongo
{
    public class MetaUser
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [BsonElement("UserId")]
        public long UserId { get; set; }

        [BsonElement("IsPetNameAskedOnRename")]
        public bool IsPetNameAskedOnRename { get; set; } 
        [BsonElement("IsConfirmedPetRenaming")]
        public bool IsAskedToConfirmRenaming { get; set; }  

        [BsonElement("TmpPetName")]
        public string TmpPetName { get; set; }

        public MetaUser Clone(MetaUser toClone)
        {
            return new MetaUser()
            {
                UserId = toClone.UserId,
                IsPetNameAskedOnRename = toClone.IsPetNameAskedOnRename,
                IsAskedToConfirmRenaming = toClone.IsAskedToConfirmRenaming
            };
        }
    }
}
