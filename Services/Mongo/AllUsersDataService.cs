using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;

namespace TamagotchiBot.Services.Mongo
{
    public class AllUsersDataService : MainConnectService
    {
        private readonly IMongoCollection<AllUsersData> _allUsersData;

        public AllUsersDataService(ITamagotchiDatabaseSettings settings) : base(settings)
        {
            _allUsersData = base.GetCollection<AllUsersData>(settings.AllUsersDataCollectionName);
        }

        public AllUsersData Get(long userId) => _allUsersData.Find(x => x.UserId == userId).FirstOrDefault();
        public List<AllUsersData> GetAll() => _allUsersData.Find(x => true).ToList();

        public void Create(AllUsersData userData) 
        {
            userData.Created = DateTime.UtcNow;
            _allUsersData.InsertOne(userData); 
        }

        public void Update(AllUsersData usersData)
        {
            var userDataDB = _allUsersData.Find(ud => ud.UserId == usersData.UserId).FirstOrDefault();

            if (userDataDB == null)
                return;

            usersData.Updated = DateTime.UtcNow;
            _allUsersData.ReplaceOne(u => u.UserId == usersData.UserId, usersData);
        }
    }
}
