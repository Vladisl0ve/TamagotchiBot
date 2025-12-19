using MongoDB.Driver;
using System;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;

namespace TamagotchiBot.Services.Mongo
{
    public class DiamondService(ITamagotchiDatabaseSettings settings) : MongoServiceBase<User>(settings)
    {
        public void UpdateDiamonds(long userId, int newDiamonds)
        {
            var userDb = _collection.Find(u => u.UserId == userId).FirstOrDefault();
            if (userDb != null)
            {
                userDb.Diamonds = newDiamonds;
                Update(userId, userDb);
            }
        }

        public bool AddDiamonds(long userId, int diamondsToAdd)
        {
            var userDb = _collection.Find(u => u.UserId == userId).FirstOrDefault();
            if (userDb == null)
                return false;

            userDb.Diamonds += diamondsToAdd;
            userDb.Updated = DateTime.UtcNow;
            _collection.ReplaceOne(u => u.UserId == userId, userDb);
            return true;
        }

        public User Update(long userId, User userIn)
        {
            userIn.Updated = DateTime.UtcNow;
            _collection.ReplaceOne(u => u.UserId == userId, userIn);
            return userIn;
        }
    }
}
