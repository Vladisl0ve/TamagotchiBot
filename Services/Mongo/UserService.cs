using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;

namespace TamagotchiBot.Services.Mongo
{
    public class UserService : MainConnectService
    {
        private readonly IMongoCollection<User> _users;

        public UserService(ITamagotchiDatabaseSettings settings) : base(settings)
        {
            _users = base.GetCollection<User>(settings.UsersCollectionName);
        }

        public List<User> GetAll() => _users.Find(u => true).ToList();

        public User Get(long userId) => _users.Find(u => u.UserId == userId).FirstOrDefault();
        public User GetByUsername(string username) => _users.Find(u => u.Username == username).FirstOrDefault();

        public User Create(User user)
        {
            user.Created = DateTime.UtcNow;
            _users.InsertOne(user);
            return user;
        }

        public User Create(Telegram.Bot.Types.User user)
        {
            return Create(new User()
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserId = user.Id,
                Username = user.Username,
                Culture = user.LanguageCode,
                Gold = 50,
                ChatIds = new List<long>() { user.Id },
                Created = DateTime.UtcNow                
            });
        }

        public bool UpdateReferaledBy(long userId, long referaledByUserId)
        {
            var userDb = _users.Find(u => u.UserId == userId).FirstOrDefault();
            if (userDb == null)
                return false;

            userDb.Updated = DateTime.UtcNow;
            userDb.ReferaledBy = referaledByUserId;
            _users.ReplaceOne(u => u.UserId == userId, userDb);
            return true;
        }

        public User Update(long userId, User userIn)
        {
            userIn.Updated = DateTime.UtcNow;
            _users.ReplaceOne(u => u.UserId == userId, userIn);
            return userIn;
        }

        public async Task UpdateAppleGameStatus(long userId, bool isInAppleGame)
        {
            var userDb = _users.Find(u => u.UserId == userId).FirstOrDefault();
            if (userDb == null)
                return;

            userDb.IsInAppleGame = isInAppleGame;
            userDb.Updated = DateTime.UtcNow;
            await _users.ReplaceOneAsync(u => u.UserId == userId, userDb);
        }

        public void UpdateGold(long userId, int newGold)
        {
            var userDb = _users.Find(u => u.UserId == userId).FirstOrDefault();
            if (userDb != null)
            {
                userDb.Gold = newGold;
                Update(userId, userDb);
            }
        }

        public void UpdateDailyRewardTime(long userId, DateTime newStartTime)
        {
            var userDb = _users.Find(u => u.UserId == userId).FirstOrDefault();
            if (userDb != null)
            {
                userDb.GotDailyRewardTime = newStartTime;
                Update(userId, userDb);
            }
        }

        public bool UpdateNextDailyRewardNotificationTime(long userId, DateTime nextNotify)
        {
            var userDb = _users.Find(u => u.UserId == userId).FirstOrDefault();
            if (userDb == null)
                return false;

            userDb.NextDailyRewardNotificationTime = nextNotify;
            userDb.Updated = DateTime.UtcNow;
            _users.ReplaceOne(u => u.UserId == userId, userDb);
            return true;
        }

        public User UpdateLanguage(long userId, string newLanguage)
        {
            var user = _users.Find(p => p.UserId == userId).FirstOrDefault();
            if (user != null)
            {
                user.Culture = newLanguage;
                Update(userId, user);
            }

            return user;
        }

        public bool UpdateIsPetNameAskedOnCreate(long userId, bool isAsked)
        {
            var user = _users.Find(p => p.UserId == userId).FirstOrDefault();
            if (user != null)
            {
                user.IsPetNameAskedOnCreate = isAsked;
                Update(userId, user);
                return true;
            }

            return false;
        }
        public bool UpdateIsLanguageAskedOnCreate(long userId, bool isAsked)
        {
            var user = _users.Find(p => p.UserId == userId).FirstOrDefault();
            if (user != null)
            {
                user.IsLanguageAskedOnCreate = isAsked;
                Update(userId, user);
                return true;
            }

            return false;
        }

        public User Update(long userId, Telegram.Bot.Types.User userIn)
        {
            var oldUser = _users.Find(u => u.UserId == userId).First();
            User newUser = new()
            {
                Id = oldUser.Id,
                FirstName = userIn.FirstName,
                LastName = userIn.LastName,
                UserId = userIn.Id,
                Username = userIn.Username,
                Culture = oldUser.Culture,
                Updated = DateTime.UtcNow
            };
            _users.ReplaceOne(u => u.UserId == userId, newUser);
            return newUser;
        }

        public void Remove(long userId) => _users.DeleteOne(u => u.UserId == userId);
    }
}
