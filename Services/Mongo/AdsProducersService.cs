using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;

namespace TamagotchiBot.Services.Mongo
{
    public class AdsProducersService : MainConnectService
    {
        private readonly IMongoCollection<AdsProducers> _adsProducers;

        public AdsProducersService(ITamagotchiDatabaseSettings settings) : base(settings)
        {
            _adsProducers = base.GetCollection<AdsProducers>();
        }
        public List<AdsProducers> GetAll() => _adsProducers.Find(u => true).ToList();

        public AdsProducers Get(string company, string producer) => _adsProducers.Find(u => u.ProducerName == producer
                                                                                            && u.CompanyName == company).FirstOrDefault();

        public AdsProducers Get(AdsProducers ads) => _adsProducers.Find(u => u.ProducerName == ads.ProducerName
                                                                                            && u.CompanyName == ads.CompanyName).FirstOrDefault();

        private void Create(AdsProducers ads)
        {
            ads.Counter = 1;
            ads.Created = DateTime.UtcNow;
            _adsProducers.InsertOne(ads);
        }

        public AdsProducers Update(AdsProducers adsProducersIn)
        {
            adsProducersIn.Updated = DateTime.UtcNow;
            _adsProducers.ReplaceOne(u => u.CompanyName == adsProducersIn.CompanyName && u.ProducerName == adsProducersIn.ProducerName, adsProducersIn);
            return adsProducersIn;
        }

        public AdsProducers Create(string company, string producer)
        {
            AdsProducers adsProducer = new(){ CompanyName = company, ProducerName = producer };
            adsProducer.Created = DateTime.UtcNow;
            _adsProducers.InsertOne(adsProducer);
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
