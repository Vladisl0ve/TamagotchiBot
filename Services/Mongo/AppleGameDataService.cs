using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo.Games;

namespace TamagotchiBot.Services.Mongo
{
    public class AppleGameDataService : MainConnectService
    {
        readonly IMongoCollection<AppleGameData> _appleGameData;

        public AppleGameDataService(ITamagotchiDatabaseSettings settings) : base(settings)
        {
            _appleGameData = base.GetCollection<AppleGameData>(settings.AppleGameDataCollectionName);
        }

        public List<AppleGameData> GetAll() => _appleGameData.Find(c => true).ToList();
        public AppleGameData Get(long userId) => _appleGameData.Find(c => c.UserId == userId).FirstOrDefault();
        public void Update(AppleGameData toUpdate)
        {
            toUpdate.Updated = DateTime.UtcNow;
            _appleGameData.ReplaceOne(c => c.UserId == toUpdate.UserId, toUpdate);
        }

        public void Create(AppleGameData toUpdate)
        {
            toUpdate.Created = DateTime.UtcNow;
            _appleGameData.InsertOne(toUpdate);
        }

        public void Delete(long userId) => _appleGameData.DeleteOne(i => i.UserId == userId);

    }
}
