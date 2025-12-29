using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;

namespace TamagotchiBot.Services.Mongo
{
    public class BonusCodeService : MongoServiceBase<BonusCode>
    {
        public BonusCodeService(ITamagotchiDatabaseSettings settings) : base(settings)
        {
        }

        public BonusCode Get(string code) => _collection.Find(b => b.CodeValue == code).FirstOrDefault();
        public List<BonusCode> GetAll() => _collection.Find(p => true).ToList();

        public void Create(BonusCode bonusCode)
        {
            bonusCode.Created = DateTime.UtcNow;
            _collection.InsertOne(bonusCode);
        }

        public void Update(BonusCode bonusCode)
        {
            bonusCode.Updated = DateTime.UtcNow;
            _collection.ReplaceOne(b => b.CodeValue == bonusCode.CodeValue, bonusCode);
        }

        public void Delete(string code)
        {
            _collection.DeleteOne(b => b.CodeValue == code);
        }
    }
}
