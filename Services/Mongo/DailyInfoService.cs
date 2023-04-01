using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;

namespace TamagotchiBot.Services.Mongo
{
    public class DailyInfoService : MainConnectService
    {
        private readonly IMongoCollection<DailyInfo> _dailyInfo;

        public DailyInfoService(ITamagotchiDatabaseSettings settings) : base(settings)
        {
            _dailyInfo = base.GetCollection<DailyInfo>(settings.DailyInfoCollectionName);

            if (_dailyInfo.CountDocuments(u => true) == 0)
                CreateDefault();
        }

        public DailyInfo Get(DateTime dateIndex) => _dailyInfo.Find(x => x.DateInfo == dateIndex).FirstOrDefault();
        public List<DailyInfo> GetAll() => _dailyInfo.Find(x => true).ToList();
        public DailyInfo GetToday() => GetAll().Where(x => x.DateInfo.Date == DateTime.UtcNow.Date).FirstOrDefault();

        public void Create(DailyInfo dateIndex) => _dailyInfo.InsertOne(dateIndex);
        public DailyInfo CreateDefault()
        {
            var d = new DailyInfo()
            {
                CallbacksSent = 0,
                DateInfo = DateTime.UtcNow,
                MessagesSent = 0,
                UsersPlayed = 0,
            };
            _dailyInfo.InsertOne(d);

            return d;
        }
        public void Update(DailyInfo dateIndex)
        {
            var userDataDB = GetToday();

            if (userDataDB == null)
                return;

            _dailyInfo.ReplaceOne(u => u.DateInfo == userDataDB.DateInfo, dateIndex);
        }

        public void UpdateOrCreate(DailyInfo dateIndex)
        {
            if (GetToday() == null)
                Create(dateIndex);
            else
                Update(dateIndex);

        }
    }
}
