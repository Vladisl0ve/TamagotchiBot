using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;

namespace TamagotchiBot.Services.Mongo
{
    public class AdsProducersService(ITamagotchiDatabaseSettings settings) : MongoServiceBase<AdsProducers>(settings)
    {
        public List<AdsProducers> GetAll() => _collection.Find(u => true).ToList();

        public AdsProducers Get(string company, string producer) => _collection.Find(u => u.ProducerName == producer
                                                                                            && u.CompanyName == company).FirstOrDefault();

        public AdsProducers Get(AdsProducers ads) => _collection.Find(u => u.ProducerName == ads.ProducerName
                                                                                            && u.CompanyName == ads.CompanyName).FirstOrDefault();

        private void Create(AdsProducers ads)
        {
            ads.Counter = 1;
            ads.Created = DateTime.UtcNow;
            _collection.InsertOne(ads);
        }

        public AdsProducers Update(AdsProducers adsProducersIn)
        {
            adsProducersIn.Updated = DateTime.UtcNow;
            _collection.ReplaceOne(u => u.CompanyName == adsProducersIn.CompanyName && u.ProducerName == adsProducersIn.ProducerName, adsProducersIn);
            return adsProducersIn;
        }

        public AdsProducers Create(string company, string producer)
        {
            AdsProducers adsProducer = new(){ CompanyName = company, ProducerName = producer };
            adsProducer.Created = DateTime.UtcNow;
            _collection.InsertOne(adsProducer);
            return adsProducer;
        }

        public bool AddOrInsert(AdsProducers adsProducer)
        {
            var adsDB = Get(adsProducer);
            if (adsDB == null)
            {
                Create(adsProducer);
                return true;
            }

            adsDB.Counter++;
            Update(adsDB);
            return true;
        }
    }
}
