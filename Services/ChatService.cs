using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;

namespace TamagotchiBot.Services
{
    public class ChatService
    {
        readonly IMongoCollection<Chat> _chats;

        public ChatService(ITamagotchiDatabaseSettings settings)
        {
            var databaseSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);
            var client = new MongoClient(databaseSettings);
            var database = client.GetDatabase(settings.DatabaseName);

            _chats = database.GetCollection<Chat>(settings.ChatsCollectionName);
        }

        public List<Chat> GetAll() => _chats.Find(c => true).ToList();

        public Chat Get(long chatId) => _chats.Find(c => c.ChatId == chatId).FirstOrDefault();

        public Chat Create(Chat chat)
        {
            _chats.InsertOne(chat);
            return chat;
        }

        public Chat Update(long chatId, Chat chat)
        {
            _chats.ReplaceOne(c => c.ChatId == chatId, chat);
            return chat;
        }

        public void Remove(long userId) => _chats.DeleteOne(u => u.UserId == userId);
    }
}
