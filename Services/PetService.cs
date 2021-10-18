﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using TamagotchiBot.Database;
using TamagotchiBot.Models;

namespace TamagotchiBot.Services
{
    public class PetService
    {
        private IMongoCollection<Pet> _pets;
        public PetService(ITamagotchiDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _pets = database.GetCollection<Pet>(settings.PetsCollectionName);
        }

        public List<Pet> Get() => _pets.Find(p => true).ToList();

        public Pet Get(long userId) => _pets.Find(p => p.UserId == userId).FirstOrDefault();

        public Pet Create(Pet pet)
        {
            _pets.InsertOne(pet);
            return pet;
        }

        public void Update(long userId, Pet petIn) => _pets.ReplaceOne(p => p.UserId == userId, petIn);

        public void Remove(long userId) => _pets.DeleteOne(p => p.UserId == userId);

    }
}
