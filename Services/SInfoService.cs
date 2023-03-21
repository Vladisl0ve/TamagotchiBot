using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;

namespace TamagotchiBot.Services
{
    public class SInfoService
    {
        private readonly IMongoCollection<ServiceInfo> _sinfo;

        public SInfoService(ITamagotchiDatabaseSettings settings)
        {
            var databaseSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);
            var client = new MongoClient(databaseSettings);
            var database = client.GetDatabase(settings.DatabaseName);

            _sinfo = database.GetCollection<ServiceInfo>(settings.ServiceInfoCollectionName);
        }

        public DateTime GetLastGlobalUpdate() => _sinfo.Find(si => true).First().LastGlobalUpdate;
        public void UpdateLastGlobalUpdate()
        {
            var toInsert = new ServiceInfo() { _id = ObjectId.GenerateNewId(), LastGlobalUpdate = DateTime.Now};
            var lgu = _sinfo.Find(si => true).FirstOrDefault();
            if (lgu == null)
            {
                _sinfo.InsertOne(toInsert);
                return;
            }

            toInsert._id = lgu._id;
            _sinfo.ReplaceOne(i => true, toInsert);
        }
    }
}
