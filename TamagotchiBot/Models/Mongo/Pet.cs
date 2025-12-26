using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using static TamagotchiBot.UserExtensions.Constants;

namespace TamagotchiBot.Models.Mongo
{
    public class Pet : MongoModelBase
    {
        [BsonElement("UserId")]
        public long UserId { get; set; }

        [BsonElement("Name")]
        public string Name { get; set; }

        [BsonElement("Type")]
        public int Type { get; set; } = (int)PetType.Cat;

        [BsonElement("BirthDateTime")]
        public DateTime BirthDateTime { get; set; } = DateTime.UtcNow;

        [BsonElement("StartWorkingTime")]
        public DateTime StartWorkingTime { get; set; }

        [BsonElement("ToWakeUpTime")]
        public DateTime ToWakeUpTime { get; set; }

        [BsonElement("GotRandomEventTime")]
        public DateTime GotRandomEventTime { get; set; }

        [BsonElement("LastUpdateTime")]
        public DateTime LastUpdateTime { get; set; } = DateTime.UtcNow;

        [BsonElement("StartSleepingTime")]
        public DateTime StartSleepingTime { get; set; }

        [BsonElement("NextRandomEventNotificationTime")]
        public DateTime NextRandomEventNotificationTime { get; set; } = DateTime.UtcNow.AddMinutes(25);

        [BsonElement("CurrentStatus")]
        public int CurrentStatus { get; set; }

        [BsonElement("CurrentJob")]
        public int CurrentJob { get; set; }

        [BsonElement("HP")]
        public int HP { get; set; } = 100;

        [BsonElement("Hygiene")]
        public int Hygiene { get; set; }

        [BsonElement("Satiety")]
        public double Satiety { get; set; } = 80;

        [BsonElement("MPSatiety")]
        public int MPSatiety { get; set; }

        [BsonElement("LastMPFedTime")]
        public DateTime LastMPFedTime { get; set; }

        [BsonElement("Joy")]
        public int Joy { get; set; } = 30;

        [BsonElement("Fatigue")]
        public int Fatigue { get; set; } = 0;

        [Obsolete]
        [BsonElement("IsNew")]
        public bool IsNew { get; set; }

        [BsonElement("IsGone")]
        public bool IsGone { get; set; }

        [BsonElement("EXP")]
        public int EXP { get; set; } = 0;

        [BsonElement("Level")]
        public int Level { get; set; } = 1;

        [BsonElement("LevelAllGame")]
        public int LevelAllGame { get; set; }

        [BsonElement("IsAutoFeedEnabled")]
        public bool IsAutoFeedEnabled { get; set; }

        [BsonElement("EducationLevel")]
        public int EducationLevel { get; set; } = 1;

        [BsonElement("EducationStage")]
        public int EducationStage { get; set; } = 0;

        [BsonElement("StartStudyingTime")]
        public DateTime StartStudyingTime { get; set; }

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
                IsAutoFeedEnabled = petToClone.IsAutoFeedEnabled,
                EducationLevel = petToClone.EducationLevel,
                EducationStage = petToClone.EducationStage,
                StartStudyingTime = petToClone.StartStudyingTime
            };

            return clone;
        }
    }
}
