namespace TamagotchiBot.Database
{
    public class TamagotchiDatabaseSettings : ITamagotchiDatabaseSettings
    {
        public string ChatsCollectionName { get; set; }
        public string PetsCollectionName { get; set; }
        public string UsersCollectionName { get; set; }
        public string ServiceInfoCollectionName { get; set; }
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }


    public interface ITamagotchiDatabaseSettings
    {
        string ChatsCollectionName { get; set; }
        string PetsCollectionName { get; set; }
        string UsersCollectionName { get; set; }
        string ServiceInfoCollectionName { get; set; }
        string ConnectionString { get; set; }
        string DatabaseName { get; set; }
    }
}
