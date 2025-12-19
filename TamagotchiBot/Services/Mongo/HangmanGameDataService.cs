using MongoDB.Driver;
using System;
using System.Linq;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo.Games;

namespace TamagotchiBot.Services.Mongo
{
    public class HangmanGameDataService : MongoServiceBase<HangmanGameData>
    {
        public HangmanGameDataService(ITamagotchiDatabaseSettings settings) : base(settings)
        {
        }


        public HangmanGameData Get(long userId)
        {
            return _collection.Find(g => g.UserId == userId).FirstOrDefault();
        }

        public HangmanGameData Create(HangmanGameData gameData)
        {
            gameData.Created = DateTime.UtcNow;
            _collection.InsertOne(gameData);
            return gameData;
        }

        public HangmanGameData Update(HangmanGameData gameData)
        {
            gameData.Updated = DateTime.UtcNow;
            _collection.ReplaceOne(g => g.UserId == gameData.UserId, gameData);
            return gameData;
        }

        public bool Delete(long userId)
        {
            var result = _collection.DeleteOne(g => g.UserId == userId);
            return result.DeletedCount > 0;
        }
    }
}
