using CityOfRecipes_backend.Models;
using CityOfRecipes_backend.Helpers;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Microsoft.OpenApi.Extensions;

namespace CityOfRecipes_backend.Services
{
    public class RecipeService
    {
        private readonly IMongoCollection<Recipe> _recipes;
        private readonly IMongoCollection<User> _users;
        private readonly TagService _tagService;
        private readonly IngredientService _ingredientService;
        

        public RecipeService(IOptions<MongoDBSettings> mongoSettings, 
            TagService tagService, IngredientService ingredientService, UserService userService)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _recipes = database.GetCollection<Recipe>("Recipes");
            _users = database.GetCollection<User>("Users");
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

        public async Task<List<Recipe>> SearchRecipesByTagAsync(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                throw new ArgumentException("Тег не може бути порожнім.");
            }

            try
            {
                // Уніфікуємо формат тегу (без решітки, у нижньому регістрі)
                var normalizedTag = tag.Trim().ToLower();
                if (normalizedTag.StartsWith("#"))
                {
                    normalizedTag = normalizedTag.Substring(1); // Видаляємо решітку
                }

                // Фільтр: пошук рецептів, які містять заданий тег
                var filter = Builders<Recipe>.Filter.AnyEq(r => r.Tags, $"#{normalizedTag}");

                // Отримання відповідних рецептів
                var recipes = await _recipes.Find(filter).ToListAsync();

                if (!recipes.Any())
                {
                    throw new KeyNotFoundException($"Жодного рецепта з тегом '{tag}' не знайдено.");
                }

                return recipes;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка під час пошуку рецептів за тегом: {ex.Message}");
            }
        }

        public async Task<List<Recipe>> SearchRecipesByStringAsync(string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                throw new ArgumentException("Рядок пошуку не може бути порожнім.");
            }

            try
            {
                // Фільтр для текстового пошуку
                var filter = Builders<Recipe>.Filter.Text(searchQuery);

                // Сортування за релевантністю
                var sort = Builders<Recipe>.Sort.MetaTextScore("textScore");

                // Пошук документів
                var recipes = await _recipes.Find(filter)
                                            .Sort(sort) // Сортування за релевантністю
                                            .ToListAsync();

                return recipes;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка під час пошуку: {ex.Message}");
            }
        }

        public async Task<List<Recipe>> GetAllAsync() =>
            await _recipes.Find(recipe => true).ToListAsync();
        public async Task<(List<Recipe>, long)> GetPaginatedRecipesAsync(int skip = 0, int limit = 10)
        {
            try
            {
            var totalRecipes = await _recipes.CountDocumentsAsync(recipe => true);
            var recipes = await _recipes
                .Find(recipe => true)
                .Skip(skip)
                .Limit(limit)
                .ToListAsync();

            return (recipes, totalRecipes);
            }
            catch (MongoException ex)
            {
                throw new InvalidOperationException("Помилка під час отримання даних рецептів з бази даних.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Сталася несподівана помилка під час виконання запиту.", ex);
            }
        }
        public async Task<Recipe> GetBySlugAsync(string slug)
        {
            try
            {
                // Перевірка на null або порожній slug
                if (string.IsNullOrWhiteSpace(slug))
                {
                    throw new ArgumentException("Slug не може бути порожнім або null.");
                }

                // Пошук рецепта за slug
                var recipe = await _recipes.Find(recipe => recipe.Slug == slug).FirstOrDefaultAsync();

                // Якщо рецепт не знайдено
                if (recipe == null)
                {
                    throw new KeyNotFoundException($"Рецепт зі slug '{slug}' не знайдено.");
                }

                return recipe;
            }
            catch (ArgumentException ex)
            {
                // Помилка через некоректний параметр
                throw new InvalidOperationException($"Помилка вхідних даних: {ex.Message}", ex);
            }
            catch (MongoException ex)
            {
                // Помилка при роботі з MongoDB
                throw new InvalidOperationException("Помилка доступу до бази даних.", ex);
            }
            catch (Exception ex)
            {
                // Інші несподівані помилки
                throw new Exception("Сталася несподівана помилка під час виконання запиту.", ex);
            }
        }

        public async Task<Recipe?> GetByIdAsync(string recipeId)
        {
            try
            {
                var recipe = await _recipes.Find(r => r.Id == recipeId).FirstOrDefaultAsync();

                if (recipe == null)
                {
                    throw new KeyNotFoundException($"Рецепт з ID {recipeId} не знайдено.");
                }

                return recipe;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка під час отримання рецепта: {ex.Message}");
            }
        }

        public async Task<List<Recipe>> GetRecipesByAuthorIdAsync(string authorId, int page = 1, int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(authorId))
            {
                throw new ArgumentException("Ідентифікатор автора не може бути порожнім.");
            }

            if (page < 1 || pageSize < 1)
            {
                throw new ArgumentException("Невірні параметри пагінації. Сторінка та розмір сторінки повинні бути більше 0.");
            }

            try
            {
                // Фільтр: знайти всі рецепти, у яких AuthorId відповідає переданому значенню
                var filter = Builders<Recipe>.Filter.Eq(r => r.AuthorId, authorId);

                // Пропустити рецепти попередніх сторінок і взяти лише поточну сторінку
                var recipes = await _recipes.Find(filter)
                                            .Skip((page - 1) * pageSize)
                                            .Limit(pageSize)
                                            .ToListAsync();

                return recipes;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка під час отримання рецептів для автора з ID '{authorId}': {ex.Message}");
            }
        }

        public async Task<List<Recipe>> GetRecipesByCategoryIdAsync(string categoryId, int page = 1, int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(categoryId))
            {
                throw new ArgumentException("Ідентифікатор категорії не може бути порожнім.");
            }

            if (page < 1 || pageSize < 1)
            {
                throw new ArgumentException("Невірні параметри пагінації. Сторінка та розмір сторінки повинні бути більше 0.");
            }

            try
            {
                // Фільтр: знайти всі рецепти, у яких CategoryId відповідає переданому значенню
                var filter = Builders<Recipe>.Filter.Eq(r => r.CategoryId, categoryId);

                // Пропустити рецепти попередніх сторінок і взяти лише поточну сторінку
                var recipes = await _recipes.Find(filter)
                                            .Skip((page - 1) * pageSize)
                                            .Limit(pageSize)
                                            .ToListAsync();

                return recipes;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка під час отримання рецептів для категорії з ID '{categoryId}': {ex.Message}");
            }
        }

        public async Task<(List<Recipe>, long)> GetFavoriteRecipesByUserIdAsync(string userId, int page, int pageSize)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("Ідентифікатор користувача не може бути порожнім.");
            }

            if (page <= 0 || pageSize <= 0)
            {
                throw new ArgumentException("Параметри пагінації мають бути більше нуля.");
            }

            try
            {
                // Отримуємо користувача
                var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();

                if (user == null)
                {
                    throw new KeyNotFoundException("Користувача не знайдено.");
                }

                if (user.FavoriteRecipes == null || !user.FavoriteRecipes.Any())
                {
                    return (new List<Recipe>(), 0); // Якщо немає улюблених рецептів
                }

                // Загальна кількість улюблених рецептів
                var totalCount = user.FavoriteRecipes.Count;

                // Виконуємо пагінацію на стороні серверу
                var paginatedRecipes = user.FavoriteRecipes
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return (paginatedRecipes, totalCount);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка під час отримання улюблених рецептів: {ex.Message}");
            }
        }

        public async Task<(List<Recipe>, long)> GetRecipesByHolidayAsync(Holiday holiday, int page, int pageSize)
        {
            if (page <= 0 || pageSize <= 0)
            {
                throw new ArgumentException("Параметри пагінації мають бути більше нуля.");
            }

            try
            {
                // Фільтр для бітового порівняння
                var filter = Builders<Recipe>.Filter.Where(r => (r.Holidays & holiday) == holiday);

                // Загальна кількість рецептів для свята
                var totalCount = await _recipes.CountDocumentsAsync(filter);

                // Отримуємо рецепти з пагінацією
                var recipes = await _recipes.Find(filter)
                                            .Skip((page - 1) * pageSize)
                                            .Limit(pageSize)
                                            .ToListAsync();

                return (recipes, totalCount);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка під час отримання рецептів за святом: {ex.Message}");
            }
        }

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

        public async Task UpdateAsync(string recipeId, Recipe updatedData)
        {
            try
            {
                // Знайти рецепт у базі даних
                var existingRecipe = await _recipes.Find(r => r.Id == recipeId).FirstOrDefaultAsync();
                if (existingRecipe == null)
                {
                    throw new KeyNotFoundException($"Рецепт з ID {recipeId} не знайдено.");
                }

                // Перевірка на участь у конкурсі
                if (existingRecipe.IsParticipatedInContest)
                {
                    throw new InvalidOperationException("Цей рецепт брав участь у конкурсі та не може бути оновлений.");
                }

                // Генерація слага, якщо назва змінена
                if (!string.IsNullOrWhiteSpace(updatedData.RecipeName) && updatedData.RecipeName != existingRecipe.RecipeName)
                {
                    existingRecipe.Slug = await GenerateSlugAsync(updatedData.RecipeName);
                    existingRecipe.RecipeName = updatedData.RecipeName;
                }

                // Оновлення тегів
                if (!string.IsNullOrWhiteSpace(updatedData.TagsText))
                {
                    var tags = _tagService.ParseTags(updatedData.TagsText);
                    existingRecipe.Tags = tags;
                    await _tagService.UpdateGlobalTagsAsync(tags);
                }

                // Оновлення інгредієнтів
                if (!string.IsNullOrWhiteSpace(updatedData.IngredientsText))
                {
                    var ingredients = _ingredientService.ParseIngredients(updatedData.IngredientsText);
                    existingRecipe.Ingredients = ingredients;
                    await _ingredientService.UpdateGlobalIngredientsAsync(ingredients);
                }

                // Оновлення інструкцій
                if (!string.IsNullOrWhiteSpace(updatedData.InstructionsText))
                {
                    existingRecipe.InstructionsText = updatedData.InstructionsText;
                }

                // Оновлення решти полів, якщо вони передані
                if (updatedData.PreparationTimeMinutes > 0)
                {
                    existingRecipe.PreparationTimeMinutes = updatedData.PreparationTimeMinutes;
                }
                if (!string.IsNullOrWhiteSpace(updatedData.PhotoUrl))
                {
                    existingRecipe.PhotoUrl = updatedData.PhotoUrl;
                }
                if (!string.IsNullOrWhiteSpace(updatedData.VideoUrl))
                {
                    existingRecipe.VideoUrl = updatedData.VideoUrl;
                }
                if (updatedData.Holidays != Models.Holiday.None)
                {
                    existingRecipe.Holidays = updatedData.Holidays;
                }

                // Перевірка валідності рецепта
                existingRecipe.Validate();

                // Оновлення рецепта в базі
                var filter = Builders<Recipe>.Filter.Eq(r => r.Id, recipeId);
                await _recipes.ReplaceOneAsync(filter, existingRecipe);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка під час оновлення рецепта: {ex.Message}");
            }
        }

        public async Task DeleteAsync(string recipeId)
        {
            try
            {
                // Знайти рецепт у базі даних
                var existingRecipe = await _recipes.Find(r => r.Id == recipeId).FirstOrDefaultAsync();
                if (existingRecipe == null)
                {
                    throw new KeyNotFoundException($"Рецепт з ID {recipeId} не знайдено.");
                }

                // Перевірка на участь у конкурсі
                if (existingRecipe.IsParticipatedInContest)
                {
                    throw new InvalidOperationException("Цей рецепт брав участь у конкурсі та не може бути видалений.");
                }

                // Видалення рецепта
                var filter = Builders<Recipe>.Filter.Eq(r => r.Id, recipeId);
                var result = await _recipes.DeleteOneAsync(filter);

                if (result.DeletedCount == 0)
                {
                    throw new InvalidOperationException($"Не вдалося видалити рецепт з ID {recipeId}.");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка під час видалення рецепта: {ex.Message}");
            }
        }


    }
}
