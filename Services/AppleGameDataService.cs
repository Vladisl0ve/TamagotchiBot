using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo.Games;

namespace TamagotchiBot.Services
{
    public class AppleGameDataService
    {
        readonly IMongoCollection<AppleGameData> _appleGameData;

        public AppleGameDataService(ITamagotchiDatabaseSettings settings)
        {
            var databaseSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);
            var client = new MongoClient(databaseSettings);
            var database = client.GetDatabase(settings.DatabaseName);

            _appleGameData = database.GetCollection<AppleGameData>(settings.AppleGameDataCollectionName);
        }

        public List<AppleGameData> GetAll() => _appleGameData.Find(c => true).ToList();
        public AppleGameData Get(long userId) => _appleGameData.Find(c => c.UserId == userId).FirstOrDefault();
        public void Update(AppleGameData toUpdate) => _appleGameData.ReplaceOne(c => c.UserId == toUpdate.UserId, toUpdate);
        public void Create(AppleGameData toUpdate) => _appleGameData.InsertOne(toUpdate);
        public void Delete(long userId) => _appleGameData.DeleteOne(i => i.UserId == userId);

    }
}
