using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace TamagotchiBot.Models.Mongo
{
    public class Pet : MongoModelBase
    {
        [BsonElement("UserId")]
        public long UserId { get; set; }

        [BsonElement("Name")]
        public string Name { get; set; }

        [BsonElement("Type")]
        public int Type { get; set; }

        [BsonElement("BirthDateTime")]
        public DateTime BirthDateTime { get; set; }

        [BsonElement("StartWorkingTime")]
        public DateTime StartWorkingTime { get; set; }

        [BsonElement("ToWakeUpTime")]
        public DateTime ToWakeUpTime { get; set; }

        [BsonElement("GotRandomEventTime")]
        public DateTime GotRandomEventTime { get; set; }

        [BsonElement("LastUpdateTime")]
        public DateTime LastUpdateTime { get; set; }

        [BsonElement("StartSleepingTime")]
        public DateTime StartSleepingTime { get; set; }

        [BsonElement("NextRandomEventNotificationTime")]
        public DateTime NextRandomEventNotificationTime { get; set; }

        [BsonElement("CurrentStatus")]
        public int CurrentStatus { get; set; }

        [BsonElement("CurrentJob")]
        public int CurrentJob { get; set; }

        [BsonElement("HP")]
        public int HP { get; set; }

        [BsonElement("Hygiene")]
        public int Hygiene { get; set; }

        [BsonElement("Satiety")]
        public double Satiety { get; set; }

        [BsonElement("MPSatiety")]
        public int MPSatiety { get; set; }

        [BsonElement("LastMPFedTime")]
        public DateTime LastMPFedTime { get; set; }

        [BsonElement("Joy")]
        public int Joy { get; set; }

        [BsonElement("Fatigue")]
        public int Fatigue { get; set; }

        [Obsolete]
        [BsonElement("IsNew")]
        public bool IsNew { get; set; }

        [BsonElement("IsGone")]
        public bool IsGone { get; set; }

        [BsonElement("EXP")]
        public int EXP { get; set; }

        [BsonElement("Level")]
        public int Level { get; set; }

        [BsonElement("LevelAllGame")]
        public int LevelAllGame { get; set; }

        [BsonElement("IsAutoFeedEnabled")]
        public bool IsAutoFeedEnabled { get; set; }

        public static Pet Clone(Pet petToClone)
        {
            Pet clone = new()
            {
                BirthDateTime = petToClone.BirthDateTime,
                CurrentStatus = petToClone.CurrentStatus,
                EXP = petToClone.EXP,
                Fatigue = petToClone.Fatigue,
                Level = petToClone.Level,
                GotRandomEventTime = petToClone.GotRandomEventTime,
                HP = petToClone.HP,
                Hygiene = petToClone.Hygiene,
                Id = petToClone.Id,
                Joy = petToClone.Joy,
                LastUpdateTime = petToClone.LastUpdateTime,
                Name = petToClone.Name,
                NextRandomEventNotificationTime = petToClone.NextRandomEventNotificationTime,
                Satiety = petToClone.Satiety,
                StartSleepingTime = petToClone.StartSleepingTime,
                StartWorkingTime = petToClone.StartWorkingTime,
                Type = petToClone.Type,
                UserId = petToClone.UserId,
                ToWakeUpTime = petToClone.ToWakeUpTime,
                IsGone = petToClone.IsGone,
                MPSatiety = petToClone.MPSatiety,
                CurrentJob = petToClone.CurrentJob,
                LevelAllGame = petToClone.LevelAllGame,
                IsAutoFeedEnabled = petToClone.IsAutoFeedEnabled
            };

            return clone;
        }
    }
}
