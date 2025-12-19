using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;

namespace TamagotchiBot.Services.Mongo
{
    public class BannedUsersService : MongoServiceBase<BannedUsers>
    {
        public BannedUsersService(ITamagotchiDatabaseSettings settings) : base(settings)
        {
            if (GetAll().Count == 0)
                Create(new BannedUsers() { IsRenameBanned = true, UserId = 0 });
        }

        public List<BannedUsers> GetAll() => _collection.Find(u => true).ToList();

        public BannedUsers Get(long userId) => _collection.Find(u => u.UserId == userId).FirstOrDefault();

        public BannedUsers Create(BannedUsers user)
        {
            user.Created = DateTime.UtcNow;
            _collection.InsertOne(user);
            return user;
        }
    }
}
