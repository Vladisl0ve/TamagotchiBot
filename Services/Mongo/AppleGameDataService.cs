using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo.Games;

namespace TamagotchiBot.Services.Mongo
{
    public class AppleGameDataService(ITamagotchiDatabaseSettings settings) : MongoServiceBase<AppleGameData>(settings)
    {
        public List<AppleGameData> GetAll() => _collection.Find(c => true).ToList();
        public AppleGameData Get(long userId) => _collection.Find(c => c.UserId == userId).FirstOrDefault();
        public void Update(AppleGameData toUpdate)
        {
            toUpdate.Updated = DateTime.UtcNow;
            _collection.ReplaceOne(c => c.UserId == toUpdate.UserId, toUpdate);
        }

        public void Create(AppleGameData toUpdate)
        {
            toUpdate.Created = DateTime.UtcNow;
            _collection.InsertOne(toUpdate);
        }

        public void Delete(long userId) => _collection.DeleteOne(i => i.UserId == userId);

    }
}
