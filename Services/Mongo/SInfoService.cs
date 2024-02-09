using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;
using Telegram.Bot;

namespace TamagotchiBot.Services.Mongo
{
    public class SInfoService : MainConnectService
    {
        private readonly IMongoCollection<ServiceInfo> _sinfo;
        private readonly ITelegramBotClient _botClient;

        public SInfoService(ITamagotchiDatabaseSettings settings, ITelegramBotClient bot) : base(settings)
        {
            _sinfo = base.GetCollection<ServiceInfo>(settings.ServiceInfoCollectionName);
            _botClient = bot;
        }

        public DateTime GetLastGlobalUpdate() => _sinfo.Find(si => true).FirstOrDefault()?.LastGlobalUpdate ?? DateTime.MinValue;
        public DateTime GetLastAppChangeTime() => _sinfo.Find(si => true).FirstOrDefault()?.Updated ?? DateTime.MinValue;
        public bool GetDoSendChangelogs() => _sinfo.Find(si => true).FirstOrDefault()?.DoSendChangelogs ?? false;
        public bool GetDoMaintainWorks() => _sinfo.Find(si => true).FirstOrDefault()?.DoMaintainWorks ?? false;
        public List<string> GetBadWords() => _sinfo.Find(si => true).FirstOrDefault()?.BannedWords ?? new List<string>();
        public string GetLastBotstatId() => _sinfo.Find(si => true).FirstOrDefault()?.BotstatCheckId;
        public DateTime GetNextNotify()
        {
            bool isMongoAlive;
            try
            {
                isMongoAlive = _sinfo.Database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                isMongoAlive = false;
            }
            return isMongoAlive ? _sinfo.Find(si => true).FirstOrDefault()?.NextNotify ?? DateTime.MaxValue : DateTime.UtcNow.AddSeconds(10);
        }
        public DateTime GetNextDevNotify()
        {
            bool isMongoAlive;
            try
            {
                isMongoAlive = _sinfo.Database.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex);
                isMongoAlive = false;
            }
            return isMongoAlive ? _sinfo.Find(si => true).FirstOrDefault()?.NextDevNotify ?? DateTime.MaxValue : DateTime.UtcNow.AddSeconds(10);
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
            _sinfo.ReplaceOne(i => i.Id == lgu.Id, lgu);
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
            _sinfo.ReplaceOne(i => i.Id == unn.Id, unn);
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
            _sinfo.ReplaceOne(i => i.Id == unn.Id, unn);
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
            _sinfo.ReplaceOne(i => i.Id == unn.Id, unn);
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
            _sinfo.ReplaceOne(i => i.Id == lgu.Id, lgu);
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
            _sinfo.ReplaceOne(i => i.Id == lgu.Id, lgu);
        }

        public void Create(ServiceInfo info)
        {
            info.Created = DateTime.UtcNow;
            _sinfo.InsertOne(info);
        }

        public ServiceInfo Get() => _sinfo.Find(s => true).FirstOrDefault();
        public void CreateDefault()
        {
            _sinfo.InsertOne(new ServiceInfo()
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
            _sinfo.ReplaceOne(s => s.Id == sinfoDB.Id, info);
        }
        public async Task<Telegram.Bot.Types.User> GetBotUserInfo() => await _botClient.GetMeAsync();

    }
}
