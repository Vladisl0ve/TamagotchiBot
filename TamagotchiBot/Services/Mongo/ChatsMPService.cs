using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using TamagotchiBot.Database;
using TamagotchiBot.Models;
using TamagotchiBot.Models.Mongo;

namespace TamagotchiBot.Services.Mongo
{
    public class ChatsMPService(ITamagotchiDatabaseSettings settings) : MongoServiceBase<ChatsMP>(settings)
    {
        public List<ChatsMP> GetAll() => _collection.Find(c => true).ToList();

        public ChatsMP Get(long chatId) => _collection.Find(c => c.ChatId == chatId).FirstOrDefault();

        public ChatsMP Create(ChatsMP chat)
        {
            chat.Created = DateTime.UtcNow;
            if (Get(chat.ChatId) == null)
                _collection.InsertOne(chat);
            return chat;
        }

        public ChatsMP Update(long chatId, ChatsMP chat)
        {
            chat.Updated = DateTime.UtcNow;
            _collection.ReplaceOne(c => c.ChatId == chatId, chat);
            return chat;
        }

        public void Remove(long chatId) => _collection.DeleteOne(u => u.ChatId == chatId);

        public void AddDuelResult(long chatId, DuelResultModel duelResult)
        {
            var chatMPDB = Get(chatId);
            chatMPDB ??= Create(new ChatsMP()
            {
                ChatId = chatId,
                Name = "UNKNOWN",
                DuelResults = new List<DuelResultModel> ()
            });

            chatMPDB.DuelResults ??= new List<DuelResultModel>();
            chatMPDB.DuelResults.Add(duelResult);
            Update(chatMPDB.ChatId, chatMPDB);
        }
    }
}
