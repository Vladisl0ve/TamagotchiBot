using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace TamagotchiBot.Models.Mongo
{
    public class Pet
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [BsonElement("UserId")]
        public long UserId { get; set; }

        [BsonElement("Name")]
        public string Name { get; set; }

        [BsonElement("Type")]
        public string Type { get; set; }

        [BsonElement("BirthDateTime")]
        public DateTime BirthDateTime { get; set; }

        [BsonElement("StartWorkingTime")]
        public DateTime StartWorkingTime { get; set; }

        [BsonElement("ToWakeUpTime")]
        public DateTime ToWakeUpTime { get; set; }

        [BsonElement("GotRandomEventTime")]
        public DateTime GotRandomEventTime { get; set; }

        [BsonElement("GotDailyRewardTime")]
        public DateTime GotDailyRewardTime { get; set; }

        [BsonElement("LastUpdateTime")]
        public DateTime LastUpdateTime { get; set; }

        [BsonElement("StartSleepingTime")]
        public DateTime StartSleepingTime { get; set; }

        [BsonElement("NextRandomEventNotificationTime")]
        public DateTime NextRandomEventNotificationTime { get; set; }

        [BsonElement("CurrentStatus")]
        public int CurrentStatus { get; set; }

        [BsonElement("Gold")]
        public int Gold { get; set; }

        [BsonElement("HP")]
        public int HP { get; set; }

        [BsonElement("Hygiene")]
        public int Hygiene { get; set; }

        [BsonElement("Satiety")]
        public double Satiety { get; set; }

        [BsonElement("Joy")]
        public int Joy { get; set; }

        [BsonElement("Fatigue")]
        public int Fatigue { get; set; }

        [BsonElement("IsNew")]
        public bool IsWelcomed { get; set; }

        [BsonElement("EXP")]
        public int EXP { get; set; }

        [BsonElement("Level")]
        public int Level { get; set; }

        public Pet Clone(Pet petToClone)
        {
            Pet clone = new Pet()
            {
                BirthDateTime                   = petToClone.BirthDateTime,
                CurrentStatus                   = petToClone.CurrentStatus,
                EXP                             = petToClone.EXP,
                Fatigue                         = petToClone.Fatigue,
                Gold                            = petToClone.Gold,
                GotDailyRewardTime              = petToClone.GotDailyRewardTime,
                Level                           = petToClone.Level,
                GotRandomEventTime              = petToClone.GotRandomEventTime,
                HP                              = petToClone.HP,
                Hygiene                         = petToClone.Hygiene,
                Id                              = petToClone.Id,
                IsWelcomed                      = petToClone.IsWelcomed,
                Joy                             = petToClone.Joy,
                LastUpdateTime                  = petToClone.LastUpdateTime,
                Name                            = petToClone.Name,
                NextRandomEventNotificationTime = petToClone.NextRandomEventNotificationTime,
                Satiety                         = petToClone.Satiety,
                StartSleepingTime               = petToClone.StartSleepingTime,
                StartWorkingTime                = petToClone.StartWorkingTime,
                Type                            = petToClone.Type,
                UserId                          = petToClone.UserId
            };

            return clone;
        }
    }
}
