using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;

namespace TamagotchiBot.Services.Mongo
{
    public class AllUsersDataService(ITamagotchiDatabaseSettings settings) : MongoServiceBase<AllUsersData>(settings)
    {
        public AllUsersData Get(long userId) => _collection.Find(x => x.UserId == userId).FirstOrDefault();
        public List<AllUsersData> GetAll() => _collection.Find(x => true).ToList();
        public long CountAllAUD() => _collection.CountDocuments(a => true);

        public void Create(AllUsersData userData) 
        {
            userData.Created = DateTime.UtcNow;
            _collection.InsertOne(userData); 
        }

        public void Update(AllUsersData usersData)
        {
            var userDataDB = _collection.Find(ud => ud.UserId == usersData.UserId).FirstOrDefault();

            if (userDataDB == null)
                return;

            usersData.Updated = DateTime.UtcNow;
            _collection.ReplaceOne(u => u.UserId == usersData.UserId, usersData);
        }
    }
}
