using CityOfRecipes_backend.Models;
using CityOfRecipes_backend.Helpers;
using MongoDB.Driver;
using CityOfRecipes_backend.Validation;

namespace CityOfRecipes_backend.Services
{

    public class ContestService
    {
        private readonly IMongoCollection<Contest> _contests;
        private readonly IMongoCollection<Recipe> _recipes;

        public ContestService(MongoDbContext context)
        {
            _contests = context.GetCollection<Contest>("Contests");
            _recipes = context.GetCollection<Recipe>("Recipes");
        }

        // Отримати список поточних конкурсів
        public async Task<List<Contest>> GetActiveContestsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var contests = await _contests
                    .Find(c => c.StartDate <= now && c.EndDate >= now)
                    .ToListAsync();
                if (contests.Count == 0)
                    throw new KeyNotFoundException("Наразі немає активних конкурсів.");

                return contests;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка отримання активних конкурсів: {ex.Message}");
            }
        }

        // Отримати список завершених конкурсів
        public async Task<List<Contest>> GetFinishedContestsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var contests = await _contests.Find(c => c.EndDate < now).ToListAsync();
                if (contests.Count == 0)
                    throw new KeyNotFoundException("Завершених конкурсів не знайдено.");

                return contests;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка отримання завершених конкурсів: {ex.Message}");
            }
        }

        // Отримати конкретний конкурс за ID
        public async Task<Contest?> GetContestByIdAsync(string contestId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(contestId))
                    throw new ArgumentException("ID конкурсу не може бути порожнім.");

                var contest = await _contests.Find(c => c.Id == contestId).FirstOrDefaultAsync();
                if (contest == null)
                    throw new KeyNotFoundException($"Конкурс з ID {contestId} не знайдено.");

                return contest;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка отримання конкурсу: {ex.Message}");
            }
        }

        // Отримати список конкурсів, у яких бере участь рецепт
        public async Task<List<Contest>> GetContestsByRecipeIdAsync(string recipeId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(recipeId))
                    throw new ArgumentException("ID рецепта не може бути порожнім.");

                var contests = await _contests.Find(c => c.ContestRecipes.Contains(recipeId)).ToListAsync();
                if (contests.Count == 0)
                    throw new KeyNotFoundException("Рецепт не бере участі в жодному конкурсі.");

                return contests;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка отримання конкурсів для рецепта: {ex.Message}");
            }
        }

        // Отримати список конкурсів, у яких рецепт може взяти участь
        public async Task<List<Contest>> GetAvailableContestsForRecipeAsync(string recipeId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(recipeId))
                    throw new ArgumentException("ID рецепта не може бути порожнім.");

                var recipe = await _recipes.Find(r => r.Id == recipeId).FirstOrDefaultAsync();
                if (recipe == null)
                    throw new KeyNotFoundException("Рецепт не знайдено.");

                var now = DateTime.UtcNow;
                var contests = await _contests.Find(c =>
                    c.StartDate <= now && c.EndDate >= now &&
                    (string.IsNullOrEmpty(c.CategoryId) || c.CategoryId == recipe.CategoryId) &&
                    (string.IsNullOrEmpty(c.RequiredIngredients) || recipe.IngredientsList.Contains(c.RequiredIngredients))
                ).ToListAsync();

                if (contests.Count == 0)
                    throw new KeyNotFoundException("Немає конкурсів, у які цей рецепт може взяти участь.");

                return contests;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка отримання доступних конкурсів для рецепта: {ex.Message}");
            }
        }

        // Отримати список рецептів у конкурсі
        public async Task<List<Recipe>> GetRecipesByContestIdAsync(string contestId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(contestId))
                    throw new ArgumentException("ID конкурсу не може бути порожнім.");

                var contest = await _contests.Find(c => c.Id == contestId).FirstOrDefaultAsync();
                if (contest == null)
                    throw new KeyNotFoundException("Конкурс не знайдено.");

                var recipes = await _recipes
                    .Find(r => contest.ContestRecipes.Contains(r.Id))
                    .SortByDescending(r => r.AverageRating)
                    .ToListAsync();
                if (recipes.Count == 0)
                    throw new KeyNotFoundException("У цьому конкурсі немає рецептів.");

                return recipes;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка отримання рецептів для конкурсу: {ex.Message}");
            }
        }

        // Додати рецепт до конкурсу
        public async Task AddRecipeToContestAsync(string contestId, string recipeId, string userId)
        {
            try
            {
                if(string.IsNullOrWhiteSpace(contestId) ||
                    string.IsNullOrWhiteSpace(recipeId) || 
                    string.IsNullOrWhiteSpace(userId))
                    throw new ArgumentException("ID конкурсу, ID рецепта та ID користувача не можуть бути порожніми.");

                var contest = await _contests.Find(c => c.Id == contestId).FirstOrDefaultAsync();
                if (contest == null)
                    throw new KeyNotFoundException("Конкурс не знайдено.");

                var recipe = await _recipes.Find(r => r.Id == recipeId).FirstOrDefaultAsync();
                if (recipe == null)
                    throw new KeyNotFoundException("Рецепт не знайдено.");

                // Перевіряємо, чи є користувач власником рецепта
                if (recipe.AuthorId != userId)
                    throw new UnauthorizedAccessException("Ви можете додавати до конкурсу лише свої рецепти.");

                if (contest.ContestRecipes.Contains(recipeId))
                    throw new InvalidOperationException("Рецепт вже бере участь у цьому конкурсі.");

                if (recipe.TotalRatings < contest.InitialLikes)
                    throw new InvalidOperationException($"Рецепт повинен мати щонайменше {contest.InitialLikes} лайків для участі.");

                contest.ContestRecipes.Add(recipeId);
                var update = Builders<Contest>.Update.Set(c => c.ContestRecipes, contest.ContestRecipes);
                await _contests.UpdateOneAsync(c => c.Id == contestId, update);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка додавання рецепта до конкурсу: {ex.Message}");
            }
        }

        // Створити конкурс
        public async Task<Contest> CreateContestAsync(Contest newContest)
        {
            try
            {
                if (newContest == null)
                    throw new ArgumentNullException(nameof(newContest), "Дані конкурсу не можуть бути порожніми.");

                // 🔹 Перевіряємо коректність дат
                if (newContest.StartDate >= newContest.EndDate)
                    throw new ArgumentException("Дата початку має бути раніше, ніж дата завершення.");

                // 🔹 Генеруємо унікальний Slug (якщо не передано)
                if (string.IsNullOrWhiteSpace(newContest.Slug))
                {
                    newContest.Slug = await GenerateSlugAsynс(newContest.ContestName);
                }

                // 🔹 Викликаємо валідацію моделі
                newContest.Validate();

                // Перевірка URL-адрес
                if (!UrlValidator.IsValidUrl(newContest.PhotoUrl))
                    throw new ArgumentException("Некоректне посилання на фото.");

                await _contests.InsertOneAsync(newContest);
                return newContest;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка створення конкурсу: {ex.Message}");
            }
        }

        private async Task<string> GenerateSlugAsynс(string title)
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

        public async Task<bool> SlugExistsAsync(string slug)
        {
            return await _recipes.Find(recipe => recipe.Slug == slug).AnyAsync();
        }

    }
}
