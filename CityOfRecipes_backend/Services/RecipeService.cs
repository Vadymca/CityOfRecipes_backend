using CityOfRecipes_backend.Models;
using CityOfRecipes_backend.Helpers;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Microsoft.OpenApi.Extensions;
using MongoDB.Bson;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CityOfRecipes_backend.DTOs;
using System.Linq;
using CityOfRecipes_backend.Validation;

namespace CityOfRecipes_backend.Services
{
    public class RecipeService
    {
        private readonly IMongoCollection<Recipe> _recipes;
        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<Category> _categories;
        private readonly TagService _tagService;
        private readonly IngredientService _ingredientService;
        

        public RecipeService(IOptions<MongoDBSettings> mongoSettings, 
            TagService tagService, IngredientService ingredientService, UserService userService)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _recipes = database.GetCollection<Recipe>("Recipes");
            _users = database.GetCollection<User>("Users");
            _categories = database.GetCollection<Category>("Categories");
            _tagService = tagService;
            _ingredientService = ingredientService;
            
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

        public async Task<(List<RecipeDto>, long)> SearchRecipesByTagAsync(string tag, int page = 1, int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                throw new ArgumentException("Тег не може бути порожнім.");
            }

            if (page < 1 || pageSize < 1)
            {
                throw new ArgumentException("Невірні параметри пагінації. Сторінка та розмір сторінки повинні бути більше 0.");
            }

            try
            {
                // Уніфікація формату тегу (без решітки, у нижньому регістрі)
                var normalizedTag = tag.Trim().ToLower();
                if (normalizedTag.StartsWith("#"))
                {
                    normalizedTag = normalizedTag.Substring(1); // Видаляємо решітку
                }

                // Фільтр: пошук рецептів, які містять заданий тег
                var filter = Builders<Recipe>.Filter.AnyEq(r => r.Tags, $"#{normalizedTag}");

                // Отримати загальну кількість рецептів з цим тегом
                var totalRecipes = await _recipes.CountDocumentsAsync(filter);

                // Отримати список рецептів з пагінацією і сортуванням від нових до старих
                var recipes = await _recipes.Find(filter)
                                            .SortByDescending(r => r.CreatedAt) // Сортуємо від найновіших
                                            .Skip((page - 1) * pageSize)
                                            .Limit(pageSize)
                                            .ToListAsync();

                // Перетворення `Recipe` у `RecipeDto`
                var recipeDtos = recipes.Select(r => new RecipeDto
                {
                    Id = r.Id.ToString(),
                    Slug = r.Slug,
                    RecipeName = r.RecipeName,
                    PhotoUrl = r.PhotoUrl,
                    AuthorId = r.AuthorId.ToString(),
                    CategoryId = r.CategoryId?.ToString() ?? string.Empty,
                    AverageRating = r.AverageRating
                }).ToList();

                return (recipeDtos, totalRecipes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка під час пошуку рецептів за тегом '{tag}': {ex.Message}");
            }
        }

        public async Task<(List<RecipeDto>, long)> SearchRecipesByStringAsync(string searchQuery, int page = 1, int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(searchQuery) || searchQuery.Length < 3)
            {
                return await GetPaginatedRecipesAsync(page, pageSize);
            }

            try
            {
                var words = searchQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // Фільтр для точного збігу назви
                var exactMatchFilter = Builders<Recipe>.Filter.Eq(r => r.RecipeName, searchQuery);

                // Фільтр для часткового збігу (як у вашому коді)
                var regexFilters = words.Select(word =>
                {
                    var regex = new BsonRegularExpression($".*{Regex.Escape(word)}.*", "i");
                    return Builders<Recipe>.Filter.Or(
                        Builders<Recipe>.Filter.Regex(r => r.RecipeName, regex),
                        Builders<Recipe>.Filter.Regex(r => r.IngredientsList, regex),
                        Builders<Recipe>.Filter.Regex(r => r.InstructionsText, regex),
                        Builders<Recipe>.Filter.Regex(r => r.TagsText, regex),
                        Builders<Recipe>.Filter.Regex(r => r.Tags, regex)
                    );
                }).ToList();

                var partialMatchFilter = Builders<Recipe>.Filter.Or(regexFilters);

                // Спочатку шукаємо точні збіги, потім часткові
                var exactMatches = await _recipes.Find(exactMatchFilter).ToListAsync();
                var partialMatches = await _recipes.Find(partialMatchFilter).ToListAsync();

                // Видаляємо дублікати
                var recipes = exactMatches.Concat(partialMatches)
                    .DistinctBy(r => r.Id)
                    .ToList();

                long totalCount = recipes.Count;

                // Пагінація
                var paginatedRecipes = recipes
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var recipeDtos = paginatedRecipes.Select(r => new RecipeDto
                {
                    Id = r.Id.ToString(),
                    Slug = r.Slug,
                    RecipeName = r.RecipeName,
                    PhotoUrl = r.PhotoUrl,
                    AuthorId = r.AuthorId.ToString(),
                    CategoryId = r.CategoryId.ToString(),
                    AverageRating = r.AverageRating
                }).ToList();

                return (recipeDtos, totalCount);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка під час пошуку: {ex.Message}");
            }
        }


        public async Task<List<Recipe>> GetAllAsync() =>
            await _recipes.Find(recipe => true).ToListAsync();
        public async Task<(List<RecipeDto>, long)> GetPaginatedRecipesAsync(int page = 1, int pageSize = 10)
        {
            if (page <= 0 || pageSize <= 0)
                throw new ArgumentException("Параметри пагінації мають бути більше нуля.");

            try
            {
                // Загальна кількість рецептів
                var totalRecipes = await _recipes.CountDocumentsAsync(recipe => true);
                // Отримуємо список рецептів із сортуванням та пагінацією
                var recipes = await _recipes
                    .Find(_ => true)
                    .SortByDescending(r => r.CreatedAt) // Від найновіших до найстаріших
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();

                // Отримуємо всі унікальні CategoryId
                var categoryIds = recipes
                    .Where(r => r.CategoryId != null)
                    .Select(r => r.CategoryId)
                    .Distinct()
                    .ToList();

                // Отримуємо категорії за їхніми ID
                var categories = await _categories
                    .Find(c => categoryIds.Contains(c.Id))
                    .ToListAsync();

                // Перетворюємо `Recipe` у `RecipeDto`
                var recipeDtos = recipes.Select(r =>
                {
                    
                    return new RecipeDto
                    {
                        Id = r.Id.ToString(),
                        Slug = r.Slug,
                        RecipeName = r.RecipeName,
                        PhotoUrl = r.PhotoUrl,
                        AuthorId = r.AuthorId.ToString(),
                        CategoryId = r.CategoryId.ToString(),
                        AverageRating = r.AverageRating
                    };
                }).ToList();

                return (recipeDtos, totalRecipes);
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

        public async Task<(List<RecipeDto>, long)> GetRecipesByAuthorIdAsync(string authorId, int page = 1, int pageSize = 10)
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

                // Отримати загальну кількість рецептів цього автора
                var totalRecipes = await _recipes.CountDocumentsAsync(filter);

                // Отримати список рецептів з пагінацією і сортуванням від нових до старих
                var recipes = await _recipes.Find(filter)
                                            .SortByDescending(r => r.CreatedAt) // Сортуємо від найновіших
                                            .Skip((page - 1) * pageSize)
                                            .Limit(pageSize)
                                            .ToListAsync();

                // Перетворюємо `Recipe` у `RecipeDto`
                var recipeDtos = recipes.Select(r => new RecipeDto
                {
                    Id = r.Id.ToString(),
                    Slug = r.Slug,
                    RecipeName = r.RecipeName,
                    PhotoUrl = r.PhotoUrl,
                    AuthorId = r.AuthorId.ToString(),
                    CategoryId = r.CategoryId?.ToString() ?? string.Empty,
                    AverageRating = r.AverageRating
                }).ToList();

                return (recipeDtos, totalRecipes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка під час отримання рецептів для автора з ID '{authorId}': {ex.Message}");
            }
        }

        public async Task<(List<RecipeDto>, long)> GetRecipesByCategoryIdAsync(string categoryId, int page = 1, int pageSize = 10)
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

                // Отримати загальну кількість рецептів у категорії
                var totalRecipes = await _recipes.CountDocumentsAsync(filter);

                // Отримати список рецептів з пагінацією і сортуванням від нових до старих
                var recipes = await _recipes.Find(filter)
                                            .SortByDescending(r => r.CreatedAt) // Сортуємо від найновіших
                                            .Skip((page - 1) * pageSize)
                                            .Limit(pageSize)
                                            .ToListAsync();

                // Перетворюємо `Recipe` у `RecipeDto`
                var recipeDtos = recipes.Select(r => new RecipeDto
                {
                    Id = r.Id.ToString(),
                    Slug = r.Slug,
                    RecipeName = r.RecipeName,
                    PhotoUrl = r.PhotoUrl,
                    AuthorId = r.AuthorId.ToString(),
                    CategoryId = r.CategoryId?.ToString() ?? string.Empty,
                    AverageRating = r.AverageRating
                }).ToList();

                return (recipeDtos, totalRecipes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка під час отримання рецептів для категорії з ID '{categoryId}': {ex.Message}");
            }
        }

        public async Task<(List<RecipeDto>, long)> GetFavoriteRecipesByUserIdAsync(string userId, int page, int pageSize)
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
                    return (new List<RecipeDto>(), 0); // Якщо немає улюблених рецептів
                }

                // Формуємо список ObjectId улюблених рецептів
                var favoriteRecipeIds = user.FavoriteRecipes.Select(id => id.ToString()).ToList();

                // Формуємо фільтр для отримання улюблених рецептів
                var filter = Builders<Recipe>.Filter.In(r => r.Id, favoriteRecipeIds);

                // Загальна кількість улюблених рецептів
                var totalCount = await _recipes.CountDocumentsAsync(filter);

                // Отримуємо улюблені рецепти з пагінацією та сортуванням (від найновіших)
                var paginatedRecipes = await _recipes.Find(filter)
                                                     .SortByDescending(r => r.CreatedAt)
                                                     .Skip((page - 1) * pageSize)
                                                     .Limit(pageSize)
                                                     .ToListAsync();

                // Перетворюємо `Recipe` у `RecipeDto`
                var recipeDtos = paginatedRecipes.Select(r => new RecipeDto
                {
                    Id = r.Id.ToString(),
                    Slug = r.Slug,
                    RecipeName = r.RecipeName,
                    PhotoUrl = r.PhotoUrl,
                    AuthorId = r.AuthorId.ToString(),
                    CategoryId = r.CategoryId?.ToString() ?? string.Empty,
                    AverageRating = r.AverageRating
                }).ToList();

                return (recipeDtos, totalCount);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка під час отримання улюблених рецептів: {ex.Message}");
            }
        }

        public async Task<(List<RecipeDto>, long)> GetRecipesByHolidayAsync(string holiday, int page, int pageSize)
        {
            if (page <= 0 || pageSize <= 0)
            {
                throw new ArgumentException("Параметри пагінації мають бути більше нуля.");
            }

            try
            {
                // Визначаємо фільтр для пошуку за святом
                var filter = holiday.ToLower() switch
                {
                    "christmas" => Builders<Recipe>.Filter.Eq(r => r.IsChristmas, true),
                    "newyear" => Builders<Recipe>.Filter.Eq(r => r.IsNewYear, true),
                    "children" => Builders<Recipe>.Filter.Eq(r => r.IsChildren, true),
                    "easter" => Builders<Recipe>.Filter.Eq(r => r.IsEaster, true),
                    _ => Builders<Recipe>.Filter.Eq(r => r.IsChristmas, false) // Якщо свято не знайдено, повертаємо порожній результат
                };

                // Загальна кількість рецептів для свята
                var totalCount = await _recipes.CountDocumentsAsync(filter);

                // Отримуємо рецепти з пагінацією та сортуванням по даті (найновіші зверху)
                var recipes = await _recipes.Find(filter)
                                            .SortByDescending(r => r.CreatedAt)
                                            .Skip((page - 1) * pageSize)
                                            .Limit(pageSize)
                                            .ToListAsync();

                // Конвертуємо `Recipe` у `RecipeDto`
                var recipeDtos = recipes.Select(r => new RecipeDto
                {
                    Id = r.Id.ToString(),
                    Slug = r.Slug,
                    RecipeName = r.RecipeName,
                    PhotoUrl = r.PhotoUrl,
                    AuthorId = r.AuthorId.ToString(),
                    CategoryId = r.CategoryId?.ToString() ?? string.Empty,
                    AverageRating = r.AverageRating
                }).ToList();

                return (recipeDtos, totalCount);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка під час отримання рецептів за святом '{holiday}': {ex.Message}");
            }
        }

        public async Task<bool> ToggleFavoriteRecipeAsync(string userId, string recipeId, bool isAdded = true)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("Ідентифікатор користувача не може бути пустим.", nameof(userId));

            if (string.IsNullOrEmpty(recipeId))
                throw new ArgumentException("Ідентифікатор рецепта не може бути пустим.", nameof(recipeId));

            // Перетворюємо recipeId у ObjectId
            if (!ObjectId.TryParse(recipeId, out ObjectId recipeObjectId))
                throw new ArgumentException("Невірний формат ідентифікатора рецепта.", nameof(recipeId));

            // Завантажуємо користувача
            var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null)
                throw new KeyNotFoundException("Користувача з вказаним ID не знайдено.");

            // Ініціалізуємо список улюблених рецептів, якщо він ще не створений
            if (user.FavoriteRecipes == null)
            {
                user.FavoriteRecipes = new List<ObjectId> { recipeObjectId };
            }
            else
            {
                // Додаємо або видаляємо рецепт
                if (user.FavoriteRecipes.Contains(recipeObjectId))
                {
                    user.FavoriteRecipes.Remove(recipeObjectId); // Видаляємо з улюблених
                    isAdded = false;
                }
                else
                {
                    user.FavoriteRecipes.Add(recipeObjectId); // Додаємо в улюблені
                    isAdded = true;
                }
            }

            // Оновлюємо користувача в базі
            var updateDefinition = Builders<User>.Update.Set(u => u.FavoriteRecipes, user.FavoriteRecipes);
            await _users.UpdateOneAsync(u => u.Id == userId, updateDefinition);

            return isAdded;
        }

        public async Task<Recipe> CreateAsync(CreateRecipeDto dto, string authorId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.RecipeName))
                    throw new ArgumentException("Назва рецепта не може бути порожньою.");

                // Генерація унікального slug
                var slug = await GenerateSlugAsync(dto.RecipeName);

                // Обробка тегів
                var tags = _tagService.ParseTags(dto.TagsText);
                await _tagService.UpdateGlobalTagsAsync(tags);

                // Створюємо новий об'єкт рецепта
                var newRecipe = new Recipe
                {
                    AuthorId = authorId,
                    CategoryId = dto.CategoryId,
                    RecipeName = dto.RecipeName,
                    PreparationTimeMinutes = dto.PreparationTimeMinutes,
                    IngredientsList = dto.IngredientsList,
                    InstructionsText = dto.InstructionsText,
                    PhotoUrl = dto.PhotoUrl,
                    VideoUrl = dto.VideoUrl,
                    TagsText = dto.TagsText,
                    Tags = tags,
                    Slug = slug,
                    CreatedAt = DateTime.UtcNow,
                    IsChristmas = dto.IsChristmas,
                    IsNewYear = dto.IsNewYear,
                    IsChildren = dto.IsChildren,
                    IsEaster = dto.IsEaster
                };

                // Перевірка валідності
                newRecipe.Validate();

                // Перевірка URL-адрес
                if (!UrlValidator.IsValidUrl(newRecipe.PhotoUrl))
                    throw new ArgumentException("Некоректне посилання на фото.");

                if (!UrlValidator.IsValidUrl(newRecipe.VideoUrl))
                    throw new ArgumentException("Некоректне посилання на відео.");

                // Додавання рецепта в базу
                await _recipes.InsertOneAsync(newRecipe);

                return newRecipe; // Повертаємо створеного рецепта
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException($"Помилка при створенні рецепта: {ex.Message}");
            }
        }

        public async Task<Recipe> UpdateAsync(string recipeId, Recipe updatedData, string userId)
        {
            try
            {
                // Знайти рецепт у базі даних
                var existingRecipe = await _recipes.Find(r => r.Id == recipeId).FirstOrDefaultAsync();
                if (existingRecipe == null)
                {
                    throw new KeyNotFoundException($"Рецепт з ID {recipeId} не знайдено.");
                }

                // Перевірка на авторство
                if (existingRecipe.AuthorId != userId)
                {
                    throw new UnauthorizedAccessException("Ви не можете редагувати цей рецепт, оскільки ви не є його автором.");
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
                if (!string.IsNullOrWhiteSpace(updatedData.IngredientsList))
                {
                    existingRecipe.IngredientsList = updatedData.IngredientsList;
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
                // 🔹 **Додаємо валідацію URL**
                if (!string.IsNullOrWhiteSpace(updatedData.PhotoUrl))
                {
                    if (!UrlValidator.IsValidUrl(updatedData.PhotoUrl))
                        throw new ArgumentException("Некоректне посилання на фото.");
                    existingRecipe.PhotoUrl = updatedData.PhotoUrl;
                }

                if (!string.IsNullOrWhiteSpace(updatedData.VideoUrl))
                {
                    if (!UrlValidator.IsValidUrl(updatedData.VideoUrl))
                        throw new ArgumentException("Некоректне посилання на відео.");
                    existingRecipe.VideoUrl = updatedData.VideoUrl;
                }

                // Оновлення свят через булеві поля
                existingRecipe.IsChristmas = updatedData.IsChristmas;
                existingRecipe.IsNewYear = updatedData.IsNewYear;
                existingRecipe.IsChildren = updatedData.IsChildren;
                existingRecipe.IsEaster = updatedData.IsEaster;

                // Перевірка валідності рецепта
                existingRecipe.Validate();

                // Оновлення рецепта в базі
                var filter = Builders<Recipe>.Filter.Eq(r => r.Id, recipeId);
                await _recipes.ReplaceOneAsync(filter, existingRecipe);
                return existingRecipe;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка під час оновлення рецепта: {ex.Message}");
            }
        }

        public async Task DeleteAsync(string recipeId, string userId)
        {
            try
            {
                // Знайти рецепт у базі даних
                var existingRecipe = await _recipes.Find(r => r.Id == recipeId).FirstOrDefaultAsync();
                if (existingRecipe == null)
                {
                    throw new KeyNotFoundException($"Рецепт з ID {recipeId} не знайдено.");
                }

                // Перевірка на авторство
                if (existingRecipe.AuthorId != userId)
                {
                    throw new UnauthorizedAccessException("Ви не можете видалити цей рецепт, оскільки ви не є його автором.");
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
