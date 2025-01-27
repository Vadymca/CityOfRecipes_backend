using CityOfRecipes_backend.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace CityOfRecipes_backend.Services
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;
        public MongoDbContext(IOptions<MongoDBSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            _database = client.GetDatabase(settings.Value.DatabaseName);

        }
        public IMongoCollection<T> GetCollection<T>(string name) =>
        _database.GetCollection<T>(name);

        public IMongoCollection<User> Users => _database.GetCollection<User>("Users");

    }
}
