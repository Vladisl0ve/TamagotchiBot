﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace TamagotchiBot.Models.Mongo
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [BsonElement("UserId")]
        public long UserId { get; set; }

        [BsonElement("ChatId")]
        public long ChatId { get; set; }

        [BsonElement("ChatIds")]
        public List<long> ChatIds { get; set; }

        [BsonElement("Username")]
        public string Username { get; set; }

        [BsonElement("FirstName")]
        public string FirstName { get; set; }

        [BsonElement("LastName")]
        public string LastName { get; set; }

        [BsonElement("IsInAppleGame")]
        public bool IsInAppleGame { get; set; }

        [BsonElement("IsLanguageAskedOnCreate")]
        public bool IsLanguageAskedOnCreate { get; set; }

        [BsonElement("IsPetNameAskedOnCreate")]
        public bool IsPetNameAskedOnCreate { get; set; }

        [BsonElement("Culture")]
        public string Culture { get; set; }

        [BsonElement("NextDailyRewardNotificationTime")]
        public DateTime NextDailyRewardNotificationTime { get; set; }

        [Obsolete]
        [BsonElement("NextRandomEventNotificationTime")]
        public DateTime NextRandomEventNotificationTime { get; set; }
    }
}
