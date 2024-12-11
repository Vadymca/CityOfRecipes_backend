using CityOfRecipes_backend.Models;
using CityOfRecipes_backend.Helpers;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace CityOfRecipes_backend.Services
{
    public class RecipeService
    {
        private readonly IMongoCollection<Recipe> _recipes;
        private readonly TagService _tagService;
        private readonly IngredientService _ingredientService;

        public RecipeService(IOptions<MongoDBSettings> mongoSettings, TagService tagService, IngredientService ingredientService)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _recipes = database.GetCollection<Recipe>("Recipes");
            _tagService = tagService;
            _ingredientService = ingredientService;
            CreateIndexesAsync().Wait();
        }

        private async Task CreateIndexesAsync()
        {
            var indexKeys = Builders<Recipe>.IndexKeys.Ascending(recipe => recipe.Slug);
            var indexOptions = new CreateIndexOptions { Unique = true };
            var indexModel = new CreateIndexModel<Recipe>(indexKeys, indexOptions);
            await _recipes.Indexes.CreateOneAsync(indexModel);
        }

        public async Task<bool> SlugExistsAsync(string slug)
        {
            return await _recipes.Find(recipe => recipe.Slug == slug).AnyAsync();
        }

        public async Task<string> GenerateSlugAsync(string title)
        {
            var baseSlug = SlugHelper.Transliterate(title.ToLower());
            var uniqueSlug = baseSlug;
            int counter = 1;

            while (await SlugExistsAsync(uniqueSlug))
            {
                uniqueSlug = $"{baseSlug}{counter}";
                counter++;
            }

            return uniqueSlug;
        }

        public async Task<List<Recipe>> SearchRecipesByTagAsync(List<string> tags)
        {
            // Перевірка на null
            if (tags == null || tags.Count == 0)
            {
                throw new ArgumentException("Tags list is null or empty.");
            }

            // Видалення порожніх тегів
            var validTags = tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToList();

            if (validTags.Count == 0)
            {
                throw new ArgumentException("Tags list contains only null or empty values.");
            }

            // Форматування тегів
            var formattedTags = validTags.Select(tag => tag.StartsWith("#") ? tag : $"#{tag}").ToList();

            // Побудова фільтра
            var filter = Builders<Recipe>.Filter.AnyIn(r => r.Tags, formattedTags);

            // Виконання пошуку
            return await _recipes.Find(filter).ToListAsync();
        }




        public async Task<List<Recipe>> GetAllAsync() =>
            await _recipes.Find(recipe => true).ToListAsync();
        public async Task<(List<Recipe>, long)> GetPaginatedRecipesAsync(int skip = 0, int limit = 10)
        {
            var totalRecipes = await _recipes.CountDocumentsAsync(recipe => true);
            var recipes = await _recipes
                .Find(recipe => true)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync();

            return (recipes, totalRecipes);
        }
        public async Task<Recipe> GetBySlugAsync(string slug) =>
            await _recipes.Find(recipe => recipe.Slug == slug).FirstOrDefaultAsync();

        public async Task<Recipe?> GetByIdAsync(string id) =>
            await _recipes.Find(recipe => recipe.Id == id).FirstOrDefaultAsync();

        public async Task CreateAsync(Recipe newRecipe)
        {
            try
            {
                // Генерація слага
                newRecipe.Slug = await GenerateSlugAsync(newRecipe.RecipeName);

                // Обробка тегів
                var tags = _tagService.ParseTags(newRecipe.TagsText);
                newRecipe.Tags = tags;

                // Оновлення глобальних тегів
                await _tagService.UpdateGlobalTagsAsync(tags);

                // Обробка інгредієнтів
                var ingredients = _ingredientService.ParseIngredients(newRecipe.IngredientsText);
                newRecipe.Ingredients = ingredients;

                // Оновлення глобального списку інгредієнтів
                await _ingredientService.UpdateGlobalIngredientsAsync(ingredients);

                // Перевірка валідності рецепта
                newRecipe.Validate();

                // Додавання рецепта
                await _recipes.InsertOneAsync(newRecipe);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                throw new InvalidOperationException("Slug вже існує. Будь ласка, спробуйте ще раз з іншою назвою");
            }
        }

        public async Task UpdateAsync(string id, Recipe updatedRecipe)
        {
            var existingRecipe = await _recipes.Find(recipe => recipe.Id == id).FirstOrDefaultAsync();
            if (existingRecipe is null)
            {
                throw new KeyNotFoundException($"Рецепт з ID {id} не знайдено.");
            }

            // Зміна Slug тільки якщо змінено RecipeName
            if (!existingRecipe.RecipeName.Equals(updatedRecipe.RecipeName, StringComparison.OrdinalIgnoreCase))
            {
                // Генерація слага
                updatedRecipe.Slug = await GenerateSlugAsync(updatedRecipe.RecipeName);
            }
            else
            {
                updatedRecipe.Slug = existingRecipe.Slug;
            }

            updatedRecipe.Id = existingRecipe.Id;

            // Обробка тегів
            var tags = _tagService.ParseTags(updatedRecipe.TagsText);
            updatedRecipe.Tags = tags;

            // Оновлення глобальних тегів
            await _tagService.UpdateGlobalTagsAsync(tags);

            // Обробка інгредієнтів
            var ingredients = _ingredientService.ParseIngredients(updatedRecipe.IngredientsText);
            updatedRecipe.Ingredients = ingredients;

            // Оновлення глобального списку інгредієнтів
            await _ingredientService.UpdateGlobalIngredientsAsync(ingredients);

            // Перевірка валідності оновленого рецепта
            updatedRecipe.Validate();

            // Оновлення рецепта
            await _recipes.ReplaceOneAsync(recipe => recipe.Id == id, updatedRecipe);
        }

        public async Task RemoveAsync(string id)
        {
            var deleteResult = await _recipes.DeleteOneAsync(recipe => recipe.Id == id);
            if (deleteResult.DeletedCount == 0)
            {
                throw new KeyNotFoundException($"Recipe with ID {id} not found.");
            }
        }
    }
}
