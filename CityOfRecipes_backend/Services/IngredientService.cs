using CityOfRecipes_backend.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace CityOfRecipes_backend.Services
{
    public class IngredientService
    {
        private readonly IMongoCollection<Ingredient> _ingredients;

        public IngredientService(IOptions<MongoDBSettings> mongoSettings)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _ingredients = database.GetCollection<Ingredient>("Ingredients");
        }

        // Парсинг сирого тексту інгредієнтів
        public List<string> ParseIngredients(string rawIngredients)
        {
            if (string.IsNullOrWhiteSpace(rawIngredients))
                return new List<string>();

            var ingredients = rawIngredients
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries) // Розділення по комі та крапці з комою
                .Select(ingredient => ingredient
                    .ToLower()                               // У нижній регістр
                    .Trim()                                  // Видалення зайвих пробілів
                    .Where(char.IsLetterOrDigit)            // Тільки букви/цифри
                    .Aggregate("", (current, c) => current + c)) // Перетворення в рядок
                .Where(ingredient => !string.IsNullOrEmpty(ingredient)) // Видалення пустих рядків
                .Distinct()                                 // Унікальність
                .ToList();

            return ingredients;
        }

        // Додавання нових інгредієнтів
        public async Task UpdateGlobalIngredientsAsync(List<string> ingredients)
        {
            foreach (var ingredientName in ingredients)
            {
                var exists = await _ingredients.Find(existing => existing.IngredientName == ingredientName).AnyAsync();
                if (!exists)
                {
                    var ingredient = new Ingredient
                    {
                        IngredientName = ingredientName
                    };
                    ingredient.Validate(); // Перевірка на валідність
                    await _ingredients.InsertOneAsync(ingredient);
                }
            }
        }

        // Отримання всіх інгредієнтів
        public async Task<List<Ingredient>> GetAllIngredientsAsync() =>
            await _ingredients.Find(_ => true).ToListAsync();
    }
}
