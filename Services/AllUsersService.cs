using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;

namespace TamagotchiBot.Services
{
    public class AllUsersService
    {
        private readonly IMongoCollection<AllUsersData> _allUsersData;

        public AllUsersService(ITamagotchiDatabaseSettings settings)
        {
            var databaseSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);
            var client = new MongoClient(databaseSettings);
            var database = client.GetDatabase(settings.DatabaseName);

            _allUsersData = database.GetCollection<AllUsersData>(settings.AllUsersDataCollectionName);
        }

        public void Create(AllUsersData userData) => _allUsersData.InsertOne(userData);

        public bool Update(AllUsersData usersData)
        {
            var userDataDB = _allUsersData.Find(ud => ud.UserId == usersData.UserId).FirstOrDefault();

            if (userDataDB == null)
                return false;

            var updateDefintion = new BsonDocument();
            var bDoc = usersData.ToBsonDocument();
            SetUpdatableElements(usersData.ToBsonDocument(), updateDefintion, new HashSet<string>() { nameof(usersData.UserId), nameof(usersData.Id), nameof(usersData.Created) });
            var result = _allUsersData.UpdateOne(p => p.UserId == usersData.UserId, new BsonDocument("$set", updateDefintion));
            return result.IsAcknowledged;
        }

        private void SetUpdatableElements(BsonDocument bdoc, BsonDocument updateDefintion, HashSet<string> excluded = null, string parentName = "")
        {
            excluded = excluded ?? new HashSet<string>();
            parentName = !string.IsNullOrWhiteSpace(parentName) ? $"{parentName}." : "";
            foreach (var item in bdoc)
            {
                if (item.Value.IsObjectId || // skip _id                     
                    item.Value.IsBsonNull || // skip properties with null values
                    excluded.Contains(item.Name)) // skip other properties that should not be updated
                {
                    continue;
                }
                if (!item.Value.IsBsonDocument) // to avoid override nested objects)
                {
                    updateDefintion = updateDefintion.Add($"{parentName}{item.Name}", item.Value);
                    continue;
                }
                // recursively set nested elements to avoid overriding the full object
                SetUpdatableElements(item.Value.ToBsonDocument(), updateDefintion, parentName: item.Name);
            }
        }
    }
}
