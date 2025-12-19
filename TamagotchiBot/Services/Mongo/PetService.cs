using MongoDB.Driver;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;
using static TamagotchiBot.UserExtensions.Constants;

namespace TamagotchiBot.Services.Mongo
{
    public class PetService(ITamagotchiDatabaseSettings settings) : MongoServiceBase<Pet>(settings)
    {
        public List<Pet> GetAll() => _collection.Find(p => true).ToList();

        public Pet Get(long userId) => _collection.Find(p => p.UserId == userId).FirstOrDefault();
        public long Count() => _collection.CountDocuments(p => true);

        public Pet Create(Pet pet)
        {
            pet.Created = DateTime.UtcNow;
            _collection.InsertOne(pet);
            return pet;
        }

        public void Update(long userId, Pet petIn)
        {
            petIn.Updated = DateTime.UtcNow;
            _collection.ReplaceOne(p => p.UserId == userId, petIn);
        }

        public void UpdateName(long userId, string newName)
        {
            var pet = _collection.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.Name = newName;
                Update(userId, pet);
            }
        }
        public void UpdateCurrentStatus(long userId, int newStatus)
        {
            var pet = _collection.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.CurrentStatus = newStatus;
                Update(userId, pet);
            }
        }
        public void UpdateCurrentJob(long userId, int newJob)
        {
            var pet = _collection.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.CurrentJob = newJob;
                Update(userId, pet);
            }
        }
        public void UpdateLastMPFedTime(long userId, DateTime newLastMPFed)
        {
            var pet = _collection.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.LastMPFedTime = newLastMPFed;
                Update(userId, pet);
            }
        }
        public void UpdateType(long userId, PetType newType)
        {
            var pet = _collection.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.Type = (int)newType;
                Update(userId, pet);
            }
        }
        public void UpdateMPSatiety(long userId, int newMPSatiety)
        {
            var pet = _collection.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.MPSatiety = newMPSatiety;
                Update(userId, pet);
            }
        }
        public void UpdateEXP(long userId, int newEXP)
        {
            var pet = _collection.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.EXP = newEXP;
                Update(userId, pet);
            }
        }
        public void UpdateSatiety(long userId, double newSatiety, bool forcePush = false)
        {
            if (newSatiety > 100 && !forcePush)
                newSatiety = 100;
            else if (newSatiety < 0 && !forcePush)
                newSatiety = 0;

            var pet = _collection.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.Satiety = newSatiety;
                Update(userId, pet);
            }
        }

        public void UpdateStartWorkingTime(long userId, DateTime newStartTime)
        {
            var pet = _collection.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.StartWorkingTime = newStartTime;
                Update(userId, pet);
            }
        }

        public void UpdateGotRandomEventTime(long userId, DateTime newStartTime)
        {
            var pet = _collection.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.GotRandomEventTime = newStartTime;
                Update(userId, pet);
            }
        }

        public void UpdateFatigue(long userId, int newFatigue)
        {
            var pet = _collection.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.Fatigue = newFatigue;
                Update(userId, pet);
            }
        }

        public void UpdateHygiene(long userId, int newHygiene, bool forcePush = false)
        {
            if (newHygiene > 100 && !forcePush)
                newHygiene = 100;
            else if (newHygiene < 0 && !forcePush)
                newHygiene = 0;

            var pet = _collection.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.Hygiene = newHygiene;
                Update(userId, pet);
            }
        }
        public void UpdateHP(long userId, int newHP, bool forcePush = false)
        {
            if (newHP > 100 && !forcePush)
                newHP = 100;
            else if (newHP < 0 && !forcePush)
                newHP = 0;

            var pet = _collection.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.HP = newHP;
                Update(userId, pet);
            }
        }

        public void UpdateJoy(long userId, int newJoy, bool forcePush = false)
        {
            if (newJoy > 100 && !forcePush)
                newJoy = 100;
            else if (newJoy < 0 && !forcePush)
                newJoy = 0;

            var pet = _collection.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.Joy = newJoy;
                Update(userId, pet);
            }
        }

        public bool UpdateNextRandomEventNotificationTime(long userId, DateTime nextNotify)
        {
            var petDb = _collection.Find(u => u.UserId == userId).FirstOrDefault();
            if (petDb == null)
                return false;

            petDb.NextRandomEventNotificationTime = nextNotify;
            petDb.Updated = DateTime.UtcNow;
            _collection.ReplaceOne(u => u.UserId == userId, petDb);
            return true;
        }
        public void Remove(long userId) => _collection.DeleteOne(p => p.UserId == userId);

        internal long CountLastWeekPlayed() => _collection.CountDocuments(p => p.LastUpdateTime > DateTime.UtcNow.AddDays(-7));

        public void ResetAllExpAndLevel()
        {
            var allPets = GetAll();

            foreach (var pet in allPets)
            {
                pet.EXP = 0;
                pet.LevelAllGame += pet.Level;
                pet.Level = 1;
                pet.Updated = DateTime.UtcNow;
                _collection.ReplaceOne(p => p.UserId == pet.UserId, pet);
            }
        }

        public void UpdateIsAutoFeedEnabled(long userId, bool isEnabled)
        {
            var pet = _collection.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.IsAutoFeedEnabled = isEnabled;
                Update(userId, pet);
            }
        }

        public List<Pet> GetAutoFeedingPets() => _collection.Find(p => p.IsAutoFeedEnabled).ToList();

        public void UpdateEducationStage(long userId, int newStage)
        {
            var pet = _collection.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.EducationStage = newStage;
                Update(userId, pet);
            }
        }

        public void UpdateStartStudyingTime(long userId, DateTime newStartTime)
        {
            var pet = _collection.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.StartStudyingTime = newStartTime;
                Update(userId, pet);
            }
        }

        public void UpdateEducationLevel(long userId, int newLevel)
        {
            var pet = _collection.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.EducationLevel = newLevel;
                Update(userId, pet);
            }
        }
        public List<Pet> GetTop10PetsByLevelAllGame()
        {
            var pipeline = new EmptyPipelineDefinition<Pet>()
                .AppendStage<Pet, Pet, Pet>(AddTotalLevelStage)
                .Sort(Builders<Pet>.Sort
                .Descending("totalLevel")
                .Descending(p => p.LastUpdateTime))
                .Limit(10)
                .AppendStage<Pet, Pet, Pet>(
                new BsonDocument("$unset", "totalLevel"));

            return _collection.Aggregate(pipeline).ToList();
        }

        public long CountPetsWithHigherLevel(int myTotalLevel, DateTime myLastUpdate)
        {
            var pipeline = new EmptyPipelineDefinition<Pet>()
                .AppendStage<Pet, Pet, Pet>(AddTotalLevelStage)
                .AppendStage<Pet, Pet, Pet>(
                BuildHigherRankMatchStage(myTotalLevel, myLastUpdate))
                .Group(new BsonDocument
                {
                    { "_id", BsonNull.Value },
                    { "count", new BsonDocument("$sum", 1) }
                });
            
            var result = _collection.Aggregate(pipeline).FirstOrDefault();
            return result?["count"].AsInt32 ?? 0;
        }

        private static BsonDocument BuildHigherRankMatchStage(int myTotalLevel, DateTime myLastUpdate)
        {
            return new BsonDocument("$match",
                new BsonDocument("$or", new BsonArray
                {
                    new BsonDocument("totalLevel",
                    new BsonDocument("$gt", myTotalLevel)),
                    new BsonDocument("$and", new BsonArray
                    {
                        new BsonDocument("totalLevel",
                        new BsonDocument("$eq", myTotalLevel)),
                        new BsonDocument("LastUpdateTime",
                        new BsonDocument("$gt", myLastUpdate))
                    })
                })
                );
        }

        private static BsonDocument AddTotalLevelStage =>
            new BsonDocument("$addFields",
                new BsonDocument("totalLevel",
                    new BsonDocument("$add", new BsonArray
                    {
                        new BsonDocument("$ifNull", new BsonArray { "$LevelAllGame", 0 }),
                        new BsonDocument("$ifNull", new BsonArray { "$Level", 0 })
                    })
                )
            );
    }
}
