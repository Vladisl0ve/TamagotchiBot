using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace TamagotchiBot.Models.Mongo
{
    public class MetaUser : MongoModelBase
    {
        [BsonElement("UserId")]
        public long UserId { get; set; }

        [BsonElement("IsPetNameAskedOnRename")]
        public bool IsPetNameAskedOnRename { get; set; }

        [BsonElement("IsConfirmedPetRenaming")]
        public bool IsAskedToConfirmRenaming { get; set; }

        [BsonElement("TmpPetName")]
        public string TmpPetName { get; set; }

        [BsonElement("MsgDuelId")]
        public int MsgDuelId { get; set; }

        [BsonElement("MsgCreatorDuelId")]
        public int MsgCreatorDuelId { get; set; }

        [BsonElement("ChatDuelId")]
        public long ChatDuelId { get; set; }

        [BsonElement("DebugMessageThreadId")]
        public int DebugMessageThreadId { get; set; }

        [BsonElement("DuelStartTime")]
        public DateTime DuelStartTime { get; set; }

        [BsonElement("NextPossibleDuelTime")]
        public DateTime NextPossibleDuelTime { get; set; }

        [BsonElement("LastMPFeedingTime")]
        public DateTime LastMPFeedingTime { get; set; }

        [BsonElement("IsFeedingMPStarted")]
        public bool IsFeedingMPStarted { get; set; }

        [BsonElement("LastChatGptQA")]
        public List<string> LastChatGptQA { get; set; }

        [BsonElement("LastGeminiQA")]
        public List<string> LastGeminiQA { get; set; }

        public MetaUser Clone(MetaUser toClone)
        {
            return new MetaUser()
            {
                UserId = toClone.UserId,
                IsPetNameAskedOnRename = toClone.IsPetNameAskedOnRename,
                IsAskedToConfirmRenaming = toClone.IsAskedToConfirmRenaming,
                TmpPetName = toClone.TmpPetName,
            };
        }
    }
}
