using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;
using TamagotchiBot.Services.Interfaces;

namespace TamagotchiBot.Services.Mongo
{
    public class ChatsMPService : MainConnectService
    {
        readonly IMongoCollection<ChatsMP> _chats;

        public ChatsMPService(ITamagotchiDatabaseSettings settings) : base(settings)
        {
            _chats = base.GetCollection<ChatsMP>(settings.ChatsMPCollectionName);
        }

        public List<ChatsMP> GetAll() => _chats.Find(c => true).ToList();

        public ChatsMP Get(long chatId) => _chats.Find(c => c.ChatId == chatId).FirstOrDefault();

        public ChatsMP Create(ChatsMP chat)
        {
            chat.Created = DateTime.UtcNow;
            if (Get(chat.ChatId) == null)
                _chats.InsertOne(chat);
            return chat;
        }

        public ChatsMP Update(long chatId, ChatsMP chat)
        {
            chat.Updated = DateTime.UtcNow;
            _chats.ReplaceOne(c => c.ChatId == chatId, chat);
            return chat;
        }

        public void Remove(long chatId) => _chats.DeleteOne(u => u.ChatId == chatId);
    }
}
