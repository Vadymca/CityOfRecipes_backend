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

            // Ініціалізація індексів під час запуску
            InitializeIndexes();
        }
        public IMongoCollection<T> GetCollection<T>(string name) =>
        _database.GetCollection<T>(name);

        public IMongoCollection<User> Users => _database.GetCollection<User>("Users");

        public IMongoCollection<Recipe> Recipes => _database.GetCollection<Recipe>("Recipes");

        // Ініціалізація індексів для колекцій.
        public void InitializeIndexes()
        {
            // Створюємо текстовий індекс для колекції Recipe
            var recipeIndexKeys = Builders<Recipe>.IndexKeys
                .Text(r => r.RecipeName)
                .Text(r => r.InstructionsText)
                .Text(r => r.IngredientsText)
                .Text(r => r.IngredientsList)
                .Text(r => r.Ingredients)
                .Text(r => r.Tags)
                .Text(r => r.TagsText)
                .Text(r => r.Holidays).ToString();

            var recipeIndexModel = new CreateIndexModel<Recipe>(recipeIndexKeys);

            Recipes.Indexes.CreateOne(recipeIndexModel); // Виконуємо створення індексу
        }
    }
}
