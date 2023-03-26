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

        public DateTime GetLastGlobalUpdate() => _sinfo.Find(si => true).FirstOrDefault()?.LastGlobalUpdate ?? DateTime.MinValue;
        public bool GetDoSendChangelogs() => _sinfo.Find(si => true).FirstOrDefault()?.DoSendChangelogs ?? false;
        public void UpdateLastGlobalUpdate()
        {
            var lgu = Get();
            if (lgu == null)
            {
                CreateDefault();
                return;
            }
            lgu.LastGlobalUpdate = DateTime.UtcNow;
            _sinfo.ReplaceOne(i => i._id == lgu._id, lgu);
        }

        public void DisableChangelogsSending() //you can enable manually in database
        {
            var lgu = Get();
            if (lgu == null)
            {
                CreateDefault();
                return;
            }
            lgu.DoSendChangelogs = false;
            _sinfo.ReplaceOne(i => i._id == lgu._id, lgu);
        }

        public void Create(ServiceInfo info) => _sinfo.InsertOne(info);
        public ServiceInfo Get() => _sinfo.Find(s => true).FirstOrDefault();
        public void CreateDefault() => _sinfo.InsertOne(new ServiceInfo()
        {
            DoSendChangelogs = false,
            LastGlobalUpdate = DateTime.UtcNow
        });
        public void Update(ServiceInfo info)
        {
            var sinfoDB = Get();
            if (sinfoDB == null)
                Create(info);

            _sinfo.ReplaceOne(s => s._id == sinfoDB._id, info);
        }
    }
}
