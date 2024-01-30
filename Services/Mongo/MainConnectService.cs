using MongoDB.Driver;
using Serilog;
using System;
using System.Threading;
using TamagotchiBot.Database;
using TamagotchiBot.Services.Interfaces;

namespace TamagotchiBot.Services.Mongo
{
    public class MainConnectService : IMainConnectService
    {
        readonly IMongoDatabase MongoDatabase;
        readonly ITamagotchiDatabaseSettings TamagotchiDatabaseSettings;
        public MainConnectService(ITamagotchiDatabaseSettings settings)
        {
            TamagotchiDatabaseSettings = settings;

            bool restart;
            do
            {
                try
                {
                    restart = false;
                    var databaseSettings = MongoClientSettings.FromConnectionString(TamagotchiDatabaseSettings.ConnectionString);
                    var client = new MongoClient(databaseSettings);
                    MongoDatabase = client.GetDatabase(TamagotchiDatabaseSettings.DatabaseName);
                }
                catch (Exception ex)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(10)); //cd 10 sec before reconnects
                    restart = true;
                    Log.Error(ex.Message);
                }
            }
            while (restart);
        }

        public virtual IMongoCollection<T> GetCollection<T>(string name = null) => MongoDatabase.GetCollection<T>(name ?? typeof(T).Name);
    }
}
