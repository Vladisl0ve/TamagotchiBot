using MongoDB.Driver;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;
using TamagotchiBot.Models.Mongo.Games;
using TamagotchiBot.Services.Interfaces;

namespace TamagotchiBot.Services.Mongo
{
    public abstract class MongoServiceBase<T> : IMainConnectService
    {
        protected readonly IMongoCollection<T> _collection;
        readonly IMongoDatabase MongoDatabase;
        public MongoServiceBase(ITamagotchiDatabaseSettings settings)
        {
            bool restart;
            do
            {
                try
                {
                    restart = false;

                    _collection = new MongoClient(MongoClientSettings.FromConnectionString(GetConnectStringFromEnv()))
                        .GetDatabase(settings.DatabaseName)
                        .GetCollection<T>
                        (
                            EditedClassNames.TryGetValue(typeof(T).Name, out string value) ? value : typeof(T).Name
                        );
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

        private readonly Dictionary<string, string> EditedClassNames = new()
        {
                {nameof(AppleGameData), "AppleGame"},
                {nameof(Chat), "Chats"},
                {nameof(Pet), "Pets"},
                {nameof(User), "Users"}
        };

        private string GetConnectStringFromEnv()
        {
            string username = Environment.GetEnvironmentVariable("MongoUsername", Environment.OSVersion.Platform == PlatformID.Win32NT ? EnvironmentVariableTarget.User : EnvironmentVariableTarget.Process);
            string pass = Environment.GetEnvironmentVariable("MongoPass", Environment.OSVersion.Platform == PlatformID.Win32NT ? EnvironmentVariableTarget.User : EnvironmentVariableTarget.Process);
            string ip = Environment.GetEnvironmentVariable("MongoIP", Environment.OSVersion.Platform == PlatformID.Win32NT ? EnvironmentVariableTarget.User : EnvironmentVariableTarget.Process);
            string port = Environment.GetEnvironmentVariable("MongoPort", Environment.OSVersion.Platform == PlatformID.Win32NT ? EnvironmentVariableTarget.User : EnvironmentVariableTarget.Process);

            if (ip == null || port == null)
                return null;

            if (username == null || pass == null)
                return $"mongodb://{ip}:{port}";

            if (username != null && pass != null)
                return $"mongodb://{username}:{pass}@{ip}:{port}";

            return null;
        }
    }
}
