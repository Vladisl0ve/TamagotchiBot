using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo.Games;

namespace TamagotchiBot.Services.Mongo
{
    public class TicTacToeGameDataService(ITamagotchiDatabaseSettings settings) : MongoServiceBase<TicTacToeGameData>(settings)
    {
        public List<TicTacToeGameData> GetAll() => _collection.Find(c => true).ToList();
        public TicTacToeGameData Get(long userId) => _collection.Find(c => c.UserId == userId).FirstOrDefault();
        public void Update(TicTacToeGameData toUpdate)
        {
            toUpdate.Updated = DateTime.UtcNow;
            _collection.ReplaceOne(c => c.UserId == toUpdate.UserId, toUpdate);
        }

        public void Create(TicTacToeGameData toUpdate)
        {
            toUpdate.Created = DateTime.UtcNow;
            _collection.InsertOne(toUpdate);
        }

        public void Delete(long userId) => _collection.DeleteOne(i => i.UserId == userId);

    }
}
