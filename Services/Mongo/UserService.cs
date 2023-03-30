﻿using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
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

        public User Create(User user)
        {
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
                //Culture = user.LanguageCode
            });
        }

        public User Update(long userId, User userIn)
        {
            _users.ReplaceOne(u => u.UserId == userId, userIn);
            return userIn;
        }

        public bool UpdateAppleGameStatus(long userId, bool isInAppleGame)
        {
            var userDb = _users.Find(u => u.UserId == userId).FirstOrDefault();
            userDb.IsInAppleGame = isInAppleGame;
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
                Culture = oldUser.Culture
            };
            _users.ReplaceOne(u => u.UserId == userId, newUser);
            return newUser;
        }

        public void Remove(long userId) => _users.DeleteOne(u => u.UserId == userId);
    }
}