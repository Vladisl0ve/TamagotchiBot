using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using TamagotchiBot.Models;

namespace TamagotchiBot.Services
{
    public class UserService
    {
        private IMongoCollection<User> _users;

        public UserService(ITamagotchiDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _users = database.GetCollection<User>(settings.UsersCollectionName);
        }

        public List<User> Get() => _users.Find(u => true).ToList();

        public User Get(long userId) => _users.Find(u => u.UserId == userId).FirstOrDefault();

        public User Create(User user)
        {
            _users.InsertOne(user);
            return user;
        }

        public void Update(long userId, User userIn) => _users.ReplaceOne(u => u.UserId == userIn.UserId, userIn);

        public void Remove(long userId) => _users.DeleteOne(u => u.UserId == userId);

    }
}
