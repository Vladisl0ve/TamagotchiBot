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

        public List<User> Get()
        {
            var k = _users.Find(u => true).ToList();
            return k;
        }
    }
}
