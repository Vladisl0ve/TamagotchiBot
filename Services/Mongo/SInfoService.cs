using MongoDB.Driver;
using Serilog;
using System;
using System.Linq;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;
using Telegram.Bot.Types;

namespace TamagotchiBot.Services.Mongo
{
    public class SInfoService : MainConnectService
    {
        private readonly IMongoCollection<ServiceInfo> _sinfo;

        public SInfoService(ITamagotchiDatabaseSettings settings) : base(settings)
        {
            _sinfo = base.GetCollection<ServiceInfo>(settings.ServiceInfoCollectionName);
        }

        public DateTime GetLastGlobalUpdate() => _sinfo.Find(si => true).FirstOrDefault()?.LastGlobalUpdate ?? DateTime.MinValue;
        public bool GetDoSendChangelogs() => _sinfo.Find(si => true).FirstOrDefault()?.DoSendChangelogs ?? false;
        public DateTime GetNextNotify() => _sinfo.Find(si => true).FirstOrDefault()?.NextNotify ?? DateTime.UtcNow;
        public DateTime GetNextDevNotify() => _sinfo.Find(si => true).FirstOrDefault()?.NextDevNotify ?? DateTime.UtcNow;
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

        public void UpdateNextNotify(DateTime newNotifyDate)
        {
            var unn = Get();
            if (unn == null)
            {
                CreateDefault();
                return;
            }
            unn.NextNotify = newNotifyDate;
            _sinfo.ReplaceOne(i => i._id == unn._id, unn);
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
            _sinfo.ReplaceOne(i => i._id == unn._id, unn);
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
        public void CreateDefault()
        {
            _sinfo.InsertOne(new ServiceInfo()
            {
                DoSendChangelogs = false,
                LastGlobalUpdate = DateTime.UtcNow,
                NextNotify = DateTime.UtcNow + TimeSpan.FromMinutes(1),
                NextDevNotify = DateTime.UtcNow,
            });
            Log.Warning("Created default SInfo");
        }
        public void Update(ServiceInfo info)
        {
            var sinfoDB = Get();
            if (sinfoDB == null)
                Create(info);

            _sinfo.ReplaceOne(s => s._id == sinfoDB._id, info);
        }
    }
}
