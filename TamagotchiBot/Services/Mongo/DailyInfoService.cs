using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;

namespace TamagotchiBot.Services.Mongo
{
    public class DailyInfoService : MongoServiceBase<DailyInfo>
    {
        public DailyInfoService(ITamagotchiDatabaseSettings settings) : base(settings)
        {
            if (_collection.CountDocuments(u => true) == 0)
                CreateDefault();
        }

        public DailyInfo Get(DateTime dateIndex) => _collection.Find(x => x.DateInfo == dateIndex).FirstOrDefault();
        public List<DailyInfo> GetAll() => _collection.Find(x => true).ToList();
        public DailyInfo GetToday() => GetAll().Find(x => x.DateInfo.Date == DateTime.UtcNow.Date);
        public DailyInfo GetPreviousDay() => GetAll().LastOrDefault(d => d.DateInfo.Date < DateTime.UtcNow.Date);

        public void Create(DailyInfo dateIndex)
        {
            dateIndex.Created = DateTime.UtcNow;
            _collection.InsertOne(dateIndex);
        }

        public DailyInfo CreateDefault()
        {
            var d = new DailyInfo()
            {
                CallbacksSent = 0,
                DateInfo = DateTime.UtcNow,
                MessagesSent = 0,
                UsersPlayed = 0,
                TodayCallbacks = 0,
                TodayMessages = 0,
                Created = DateTime.UtcNow
        };
            _collection.InsertOne(d);

            return d;
        }
        public void Update(DailyInfo dateIndex)
        {
            var userDataDB = GetToday();

            if (userDataDB == null)
                return;

            dateIndex.Updated = DateTime.UtcNow;
            _collection.ReplaceOne(u => u.DateInfo == userDataDB.DateInfo, dateIndex);
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
