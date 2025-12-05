using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;

namespace TamagotchiBot.Services.Mongo
{
    public class SInfoService(ITamagotchiDatabaseSettings settings) : MongoServiceBase<ServiceInfo>(settings)
    {

        public DateTime GetLastGlobalUpdate() => _collection.Find(si => true).FirstOrDefault()?.LastGlobalUpdate ?? DateTime.MinValue;
        public DateTime GetLastAppChangeTime() => _collection.Find(si => true).FirstOrDefault()?.Updated ?? DateTime.MinValue;
        public bool GetDoSendChangelogs() => _collection.Find(si => true).FirstOrDefault()?.DoSendChangelogs ?? false;
        public bool GetDoMaintainWorks() => _collection.Find(si => true).FirstOrDefault()?.DoMaintainWorks ?? false;
        public string GetOpenAiKey() => _collection.Find(si => true).FirstOrDefault()?.OpenAiKey;
        public string GetGeminiKey() => _collection.Find(si => true).FirstOrDefault()?.GeminiApiKey;
        public string GetBotToken() => _collection.Find(si => true).FirstOrDefault()?.TmgToken;
        public List<string> GetBadWords() => _collection.Find(si => true).FirstOrDefault()?.BannedWords ?? new List<string>();
        public string GetLastBotstatId() => _collection.Find(si => true).FirstOrDefault()?.BotstatCheckId;
        public DateTime GetNextNotify()
        {
            bool isMongoAlive;
            try
            {
                isMongoAlive = _collection.Database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                isMongoAlive = false;
            }
            return isMongoAlive ? _collection.Find(si => true).FirstOrDefault()?.NextNotify ?? DateTime.MaxValue : DateTime.UtcNow.AddSeconds(10);
        }
        public DateTime GetNextDevNotify()
        {
            bool isMongoAlive;
            try
            {
                isMongoAlive = _collection.Database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                isMongoAlive = false;
            }
            return isMongoAlive ? _collection.Find(si => true).FirstOrDefault()?.NextDevNotify ?? DateTime.MaxValue : DateTime.UtcNow.AddSeconds(10);
        }
        public void UpdateLastGlobalUpdate()
        {
            var lgu = Get();
            if (lgu == null)
            {
                CreateDefault();
                return;
            }
            lgu.LastGlobalUpdate = DateTime.UtcNow;
            lgu.Updated = DateTime.UtcNow;
            _collection.ReplaceOne(i => i.Id == lgu.Id, lgu);
        }

        public void UpdateBotstatId(string newId)
        {
            var unn = Get();
            if (unn == null)
            {
                CreateDefault();
                return;
            }
            unn.BotstatCheckId = newId;
            unn.Updated = DateTime.UtcNow;
            _collection.ReplaceOne(i => i.Id == unn.Id, unn);
        }
        public void UpdateNextNotify(DateTime newNotifyDate)
        {
            var unn = Get();
            if (unn == null)
            {
                CreateDefault();
                return;
            }
            unn.NextNotify = newNotifyDate;
            unn.Updated = DateTime.UtcNow;
            _collection.ReplaceOne(i => i.Id == unn.Id, unn);
        }

        public void UpdateNextDevNotify(DateTime newNotifyDate)
        {
            var unn = Get();
            if (unn == null)
            {
                CreateDefault();
                return;
            }
            unn.NextDevNotify = newNotifyDate;
            unn.Updated = DateTime.UtcNow;
            _collection.ReplaceOne(i => i.Id == unn.Id, unn);
        }

        public void DisableMaintainWorks() //you can enable manually in database
        {
            var lgu = Get();
            if (lgu == null)
            {
                CreateDefault();
                return;
            }
            lgu.DoMaintainWorks = false;
            lgu.Updated = DateTime.UtcNow;
            _collection.ReplaceOne(i => i.Id == lgu.Id, lgu);
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
            lgu.Updated = DateTime.UtcNow;
            _collection.ReplaceOne(i => i.Id == lgu.Id, lgu);
        }

        public void Create(ServiceInfo info)
        {
            info.Created = DateTime.UtcNow;
            _collection.InsertOne(info);
        }

        public ServiceInfo Get() => _collection.Find(s => true).FirstOrDefault();
        public void CreateDefault()
        {
            _collection.InsertOne(new ServiceInfo()
            {
                DoSendChangelogs = false,
                LastGlobalUpdate = DateTime.UtcNow,
                NextNotify = DateTime.UtcNow + TimeSpan.FromMinutes(1),
                NextDevNotify = DateTime.UtcNow,
                Created = DateTime.UtcNow,
                BannedWords = new List<string>() { "TEST_STRING" }
            });
            Log.Warning("Created default SInfo");
        }
        public void Update(ServiceInfo info)
        {
            var sinfoDB = Get();
            if (sinfoDB == null)
                Create(info);

            info.Updated = DateTime.UtcNow;
            _collection.ReplaceOne(s => s.Id == sinfoDB.Id, info);
        }
    }
}
