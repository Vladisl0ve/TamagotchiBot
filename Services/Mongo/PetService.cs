using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;

namespace TamagotchiBot.Services.Mongo
{
    public class PetService : MainConnectService
    {
        private readonly IMongoCollection<Pet> _pets;
        public PetService(ITamagotchiDatabaseSettings settings) : base(settings)
        {
            _pets = base.GetCollection<Pet>(settings.PetsCollectionName);
        }

        public List<Pet> GetAll() => _pets.Find(p => true).ToList();

        public Pet Get(long userId) => _pets.Find(p => p.UserId == userId).FirstOrDefault();

        public Pet Create(Pet pet)
        {
            _pets.InsertOne(pet);
            return pet;
        }

        public void Update(long userId, Pet petIn) => _pets.ReplaceOne(p => p.UserId == userId, petIn);
        public void UpdateName(long userId, string newName)
        {
            var pet = _pets.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.Name = newName;
                Update(userId, pet);
            }
        }
        public void UpdateCurrentStatus(long userId, int newStatus)
        {
            var pet = _pets.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.CurrentStatus = newStatus;
                Update(userId, pet);
            }
        }

        public void UpdateStarving(long userId, double newStarving)
        {
            var pet = _pets.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.Satiety = newStarving;
                Update(userId, pet);
            }
        }

        public void UpdateStartWorkingTime(long userId, DateTime newStartTime)
        {
            var pet = _pets.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.StartWorkingTime = newStartTime;
                Update(userId, pet);
            }
        }

        public void UpdateDailyRewardTime(long userId, DateTime newStartTime)
        {
            var pet = _pets.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.GotDailyRewardTime = newStartTime;
                Update(userId, pet);
            }
        }

        public void UpdateGold(long userId, int newGold)
        {
            var pet = _pets.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.Gold = newGold;
                Update(userId, pet);
            }
        }
        public void UpdateFatigue(long userId, int newFatigue)
        {
            var pet = _pets.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.Fatigue = newFatigue;
                Update(userId, pet);
            }
        }

        public void UpdateHP(long userId, int newHP)
        {
            var pet = _pets.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.HP = newHP;
                Update(userId, pet);
            }
        }

        public void UpdateJoy(long userId, int newJoy)
        {
            var pet = _pets.Find(p => p.UserId == userId).FirstOrDefault();
            if (pet != null)
            {
                pet.Joy = newJoy;
                Update(userId, pet);
            }
        }

        public void Remove(long userId) => _pets.DeleteOne(p => p.UserId == userId);

    }
}
