using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using TamagotchiBot.Database;
using TamagotchiBot.Models.Mongo;

namespace TamagotchiBot.Services.Mongo
{
    public class AllUsersDataService : MainConnectService
    {
        private readonly IMongoCollection<AllUsersData> _allUsersData;

        public AllUsersDataService(ITamagotchiDatabaseSettings settings) : base(settings)
        {
            _allUsersData = base.GetCollection<AllUsersData>(settings.AllUsersDataCollectionName);
        }

        public AllUsersData Get(long userId) => _allUsersData.Find(x => x.UserId == userId).FirstOrDefault();
        public List<AllUsersData> GetAll() => _allUsersData.Find(x => true).ToList();

        public void Create(AllUsersData userData) => _allUsersData.InsertOne(userData);

        public void Update(AllUsersData usersData)
        {
            var userDataDB = _allUsersData.Find(ud => ud.UserId == usersData.UserId).FirstOrDefault();

            if (userDataDB == null)
                return;

            _allUsersData.ReplaceOne(u => u.UserId == usersData.UserId, usersData);
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
