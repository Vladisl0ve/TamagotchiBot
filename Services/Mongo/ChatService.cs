using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;

namespace TamagotchiBot.Services.Mongo
{
    public class ChatService(ITamagotchiDatabaseSettings settings) : MongoServiceBase<Chat>(settings)
    {
        public List<Chat> GetAll() => _collection.Find(c => true).ToList();

        public Chat Get(long chatId) => _collection.Find(c => c.ChatId == chatId).FirstOrDefault();

        public Chat Create(Chat chat)
        {
            chat.Created = DateTime.UtcNow;
            _collection.InsertOne(chat);
            return chat;
        }

        public Chat Update(long chatId, Chat chat)
        {
            chat.Updated = DateTime.UtcNow;
            _collection.ReplaceOne(c => c.ChatId == chatId, chat);
            return chat;
        }

        public void Remove(long userId) => _collection.DeleteOne(u => u.UserId == userId);
    }
}
