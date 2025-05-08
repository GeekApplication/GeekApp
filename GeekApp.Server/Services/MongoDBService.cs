using GeekApp.Server.Models;
using GeekApp.Shared.Lists;
using MongoDB.Driver;

namespace GeekApp.Server.Services
{
    public class MongoDBService
    {
        private readonly IMongoDatabase _database;

        public MongoDBService(IConfiguration configuration)
        {
            var client = new MongoClient(configuration["MongoDB:ConnectionString"]);
            _database = client.GetDatabase(configuration["MongoDB:DatabaseName"]);
        }

        public IMongoCollection<ApiUser> Users => _database.GetCollection<ApiUser>("Users");
        public IMongoCollection<AddList> Lists => _database.GetCollection<AddList>("Lists");
        public IMongoCollection<ListItem> ListItems => _database.GetCollection<ListItem>("ListItems");
    }
}
