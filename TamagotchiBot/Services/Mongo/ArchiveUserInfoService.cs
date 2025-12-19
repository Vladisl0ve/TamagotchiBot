using MongoDB.Driver;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;
using TamagotchiBot.UserExtensions;

namespace TamagotchiBot.Services.Mongo
{
    public class ArchiveUserInfoService(ITamagotchiDatabaseSettings settings) : MongoServiceBase<ArchiveUserInfo>(settings)
    {
        public List<ArchiveUserInfo> GetAll() => _collection.Find(u => true).ToList();
        public ArchiveUserInfo Get(long userId) => _collection.Find(u => u.UserId == userId).FirstOrDefault();

        public ArchiveUserInfo Update(long userId, ArchiveUserInfo userIn)
        {
            userIn.Updated = DateTime.UtcNow;
            _collection.ReplaceOne(u => u.UserId == userId, userIn);
            return userIn;
        }
        public ArchiveUserInfo Create(ArchiveUserInfo user)
        {
            user.Created = DateTime.UtcNow;
            _collection.InsertOne(user);
            return user;
        }

        public void Remove(long userId) => _collection.DeleteOne(u => u.UserId == userId);
    }
}
