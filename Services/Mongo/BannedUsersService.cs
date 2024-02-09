using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;

namespace TamagotchiBot.Services.Mongo
{
    public class BannedUsersService : MainConnectService
    {
        readonly IMongoCollection<BannedUsers> _bannedUsers;
        public BannedUsersService(ITamagotchiDatabaseSettings settings) : base(settings)
        {
            _bannedUsers = base.GetCollection<BannedUsers>(settings.BannedUsersCollectionName);
            if (!GetAll().Any())
                Create(new BannedUsers() { IsRenameBanned = true, UserId = 0 });
        }

        public List<BannedUsers> GetAll() => _bannedUsers.Find(u => true).ToList();

        public BannedUsers Get(long userId) => _bannedUsers.Find(u => u.UserId == userId).FirstOrDefault();

        public BannedUsers Create(BannedUsers user)
        {
            user.Created = DateTime.UtcNow;
            _bannedUsers.InsertOne(user);
            return user;
        }
    }
}
