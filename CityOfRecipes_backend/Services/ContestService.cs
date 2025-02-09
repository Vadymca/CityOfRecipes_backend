using CityOfRecipes_backend.Models;
using CityOfRecipes_backend.Helpers;
using MongoDB.Driver;
using CityOfRecipes_backend.Validation;
using CityOfRecipes_backend.DTOs;

namespace CityOfRecipes_backend.Services
{

    public class ContestService
    {
        private readonly IMongoCollection<Contest> _contests;
        private readonly IMongoCollection<Recipe> _recipes;
        private readonly IMongoCollection<Rating> _ratings;
        private readonly IMongoCollection<User> _users;
        private readonly IEmailService _emailService;

        public ContestService(MongoDbContext context, IEmailService emailService)
        {
            _contests = context.GetCollection<Contest>("Contests");
            _recipes = context.GetCollection<Recipe>("Recipes");
            _ratings = context.GetCollection<Rating>("Ratings");
            _users = context.GetCollection<User>("Users");
            _emailService = emailService;
        }

        // Отримати список поточних конкурсів
        public async Task<List<ContestDto>> GetActiveContestsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var contests = await _contests
                    .Find(c => c.StartDate <= now && c.EndDate >= now)
                    .ToListAsync();

                // Якщо відповідних конкурсів немає, повертаємо порожній список
                if (contests == null || contests.Count == 0)
                    return new List<ContestDto>();

                // Маппінг з моделі Contest до ContestDto
                var contestDtos = contests.Select(c => new ContestDto
                {
                    Id = c.Id,
                    ContestName = c.ContestName,
                    PhotoUrl = c.PhotoUrl,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    RequiredIngredients = c.RequiredIngredients,
                    ContestDetails = c.ContestDetails,
                    CategoryId = c.CategoryId,
                    Slug = c.Slug,
                    ContestRecipes = c.ContestRecipes.Select(r => new ContestRecipeDto
                    {
                        Id = r.Id,
                        Slug = r.Slug,
                        RecipeName = r.RecipeName,
                        PhotoUrl = r.PhotoUrl,
                        AuthorId = r.AuthorId,
                        CategoryId = r.CategoryId,
                        AverageRating = r.AverageRating,
                        ContestRating = r.ContestRating
                    }).ToList()
                }).ToList();

                return contestDtos;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка отримання активних конкурсів: {ex.Message}");
            }
        }

        // Отримати список завершених конкурсів
        public async Task<List<ContestDto>> GetFinishedContestsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var contests = await _contests.Find(c => c.EndDate < now).ToListAsync();

                // Якщо відповідних конкурсів немає, повертаємо порожній список
                if (contests == null || contests.Count == 0)
                    return new List<ContestDto>();

                // Маппінг з моделі Contest до ContestDto
                var contestDtos = contests.Select(c => new ContestDto
                {
                    Id = c.Id,
                    ContestName = c.ContestName,
                    PhotoUrl = c.PhotoUrl,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    RequiredIngredients = c.RequiredIngredients,
                    ContestDetails = c.ContestDetails,
                    CategoryId = c.CategoryId,
                    Slug = c.Slug,
                    ContestRecipes = c.ContestRecipes.Select(r => new ContestRecipeDto
                    {
                        Id = r.Id,
                        Slug = r.Slug,
                        RecipeName = r.RecipeName,
                        PhotoUrl = r.PhotoUrl,
                        AuthorId = r.AuthorId,
                        CategoryId = r.CategoryId,
                        AverageRating = r.AverageRating,
                        ContestRating = r.ContestRating
                    }).ToList(),
                    WinningRecipes = c.WinningRecipes.Select(r => new ContestRecipeDto
                    {
                        Id = r.Id,
                        Slug = r.Slug,
                        RecipeName = r.RecipeName,
                        PhotoUrl = r.PhotoUrl,
                        AuthorId = r.AuthorId,
                        CategoryId = r.CategoryId,
                        AverageRating = r.AverageRating,
                        ContestRating = r.ContestRating
                    }).ToList()
                }).ToList();

                return contestDtos;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка отримання завершених конкурсів: {ex.Message}");
            }
        }

        // Отримати конкретний конкурс за ID
        public async Task<ContestDto?> GetContestByIdAsync(string contestId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(contestId))
                    throw new ArgumentException("ID конкурсу не може бути порожнім.");

                var contest = await _contests.Find(c => c.Id == contestId).FirstOrDefaultAsync();
                if (contest == null)
                    throw new KeyNotFoundException($"Конкурс з ID {contestId} не знайдено.");

                // Ручний мапінг з Contest до ContestDto
                var contestDto = new ContestDto
                {
                    Id = contest.Id,
                    ContestName = contest.ContestName,
                    PhotoUrl = contest.PhotoUrl,
                    StartDate = contest.StartDate,
                    EndDate = contest.EndDate,
                    RequiredIngredients = contest.RequiredIngredients,
                    ContestDetails = contest.ContestDetails,
                    CategoryId = contest.CategoryId,
                    Slug = contest.Slug,
                    ContestRecipes = contest.ContestRecipes.Select(r => new ContestRecipeDto
                    {
                        Id = r.Id,
                        Slug = r.Slug,
                        RecipeName = r.RecipeName,
                        PhotoUrl = r.PhotoUrl,
                        AuthorId = r.AuthorId,
                        CategoryId = r.CategoryId,
                        AverageRating = r.AverageRating,
                        ContestRating = r.ContestRating
                    }).ToList(),
                    WinningRecipes = contest.WinningRecipes.Select(r => new ContestRecipeDto
                    {
                        Id = r.Id,
                        Slug = r.Slug,
                        RecipeName = r.RecipeName,
                        PhotoUrl = r.PhotoUrl,
                        AuthorId = r.AuthorId,
                        CategoryId = r.CategoryId,
                        AverageRating = r.AverageRating,
                        ContestRating = r.ContestRating
                    }).ToList()
                };

                return contestDto;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка отримання конкурсу: {ex.Message}");
            }
        }

        // Отримати конкретний конкурс за Слагом
        public async Task<ContestDto?> GetContestBySlugAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Slug не може бути порожнім.", nameof(slug));

            try
            {
                var contest = await _contests.Find(c => c.Slug == slug).FirstOrDefaultAsync();
                // Ручний мапінг з Contest до ContestDto
                var contestDto = new ContestDto
                {
                    Id = contest.Id,
                    ContestName = contest.ContestName,
                    PhotoUrl = contest.PhotoUrl,
                    StartDate = contest.StartDate,
                    EndDate = contest.EndDate,
                    RequiredIngredients = contest.RequiredIngredients,
                    ContestDetails = contest.ContestDetails,
                    CategoryId = contest.CategoryId,
                    Slug = contest.Slug,
                    ContestRecipes = contest.ContestRecipes.Select(r => new ContestRecipeDto
                    {
                        Id = r.Id,
                        Slug = r.Slug,
                        RecipeName = r.RecipeName,
                        PhotoUrl = r.PhotoUrl,
                        AuthorId = r.AuthorId,
                        CategoryId = r.CategoryId,
                        AverageRating = r.AverageRating,
                        ContestRating = r.ContestRating
                    }).ToList(),
                    WinningRecipes = contest.WinningRecipes.Select(r => new ContestRecipeDto
                    {
                        Id = r.Id,
                        Slug = r.Slug,
                        RecipeName = r.RecipeName,
                        PhotoUrl = r.PhotoUrl,
                        AuthorId = r.AuthorId,
                        CategoryId = r.CategoryId,
                        AverageRating = r.AverageRating,
                        ContestRating = r.ContestRating
                    }).ToList()
                };

                return contestDto;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка при отриманні конкурсу за slug: {ex.Message}", ex);
            }
        }

        //Отримати список конкурсів, у яких бере участь рецепт
        public async Task<List<ContestDto>> GetContestsByRecipeIdAsync(string recipeId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(recipeId))
                    throw new ArgumentException("ID рецепта не може бути порожнім.");

                var contests = await _contests
                    .Find(c => c.ContestRecipes.Any(r => r.Id == recipeId))
                    .ToListAsync();

                // Якщо відповідних конкурсів немає, повертаємо порожній список
                if (contests == null || contests.Count == 0)
                    return new List<ContestDto>();

                // Маппінг з моделі Contest до ContestDto
                var contestDtos = contests.Select(c => new ContestDto
                {
                    Id = c.Id,
                    ContestName = c.ContestName,
                    PhotoUrl = c.PhotoUrl,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    RequiredIngredients = c.RequiredIngredients,
                    ContestDetails = c.ContestDetails,
                    CategoryId = c.CategoryId,
                    Slug = c.Slug,
                    ContestRecipes = c.ContestRecipes.Select(r => new ContestRecipeDto
                    {
                        Id = r.Id,
                        Slug = r.Slug,
                        RecipeName = r.RecipeName,
                        PhotoUrl = r.PhotoUrl,
                        AuthorId = r.AuthorId,
                        CategoryId = r.CategoryId,
                        AverageRating = r.AverageRating,
                        ContestRating = r.ContestRating
                    }).ToList()
                }).ToList();

                return contestDtos;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка отримання конкурсів для рецепта: {ex.Message}");
            }
        }

        // Отримати список конкурсів, у яких рецепт може взяти участь
        public async Task<List<ContestDto>> GetAvailableContestsForRecipeAsync(string recipeId)
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
                    (string.IsNullOrEmpty(c.CategoryId) || c.CategoryId == recipe.CategoryId)
                ).ToListAsync();

                if (contests == null || contests.Count == 0)
                    return new List<ContestDto>();

                var availableContests = new List<ContestDto>();

                foreach (var contest in contests)
                {
                    // Перевірка обов’язкових інгредієнтів, якщо вони задані
                    if (!string.IsNullOrWhiteSpace(contest.RequiredIngredients))
                    {
                        var requiredIngredients = contest.RequiredIngredients
                            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(word => word.Trim().ToLower())
                            .ToList();

                        var recipeIngredients = recipe.IngredientsList
                            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(word => word.Trim().ToLower())
                            .ToList();

                        // Перевіряємо, чи кожне слово з requiredIngredients міститься хоча б в одному з recipeIngredients
                        bool allFound = requiredIngredients.All(req => recipeIngredients.Any(ri => ri.Contains(req)));

                        if (!allFound)
                            continue; // Пропускаємо цей конкурс, якщо не всі інгредієнти знайдено
                    }

                    availableContests.Add(new ContestDto
                    {
                        Id = contest.Id,
                        ContestName = contest.ContestName,
                        PhotoUrl = contest.PhotoUrl,
                        StartDate = contest.StartDate,
                        EndDate = contest.EndDate,
                        RequiredIngredients = contest.RequiredIngredients,
                        ContestDetails = contest.ContestDetails,
                        CategoryId = contest.CategoryId,
                        Slug = contest.Slug,
                        ContestRecipes = contest.ContestRecipes.Select(r => new ContestRecipeDto
                        {
                            Id = r.Id,
                            Slug = r.Slug,
                            RecipeName = r.RecipeName,
                            PhotoUrl = r.PhotoUrl,
                            AuthorId = r.AuthorId,
                            CategoryId = r.CategoryId,
                            AverageRating = r.AverageRating,
                            ContestRating = r.ContestRating
                        }).ToList()
                    });
                }

                return availableContests;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка отримання доступних конкурсів для рецепта: {ex.Message}");
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

                // Перевіряємо, чи рецепт уже є в конкурсі
                if (contest.ContestRecipes.Any(r => r.Id == recipeId))
                    throw new InvalidOperationException("Рецепт вже бере участь у цьому конкурсі.");

                // Перевірка за категорією
                if (!string.IsNullOrWhiteSpace(contest.CategoryId) && recipe.CategoryId != contest.CategoryId)
                    throw new InvalidOperationException("Категорія рецепта не відповідає категорії конкурсу.");

                // Перевірка обов’язкових інгредієнтів, якщо вони задані
                if (!string.IsNullOrWhiteSpace(contest.RequiredIngredients))
                {
                    // Розбиваємо рядки за комами або крапкою з комою
                    var requiredIngredients = contest.RequiredIngredients
                        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(word => word.Trim().ToLower())
                        .ToList();

                    var recipeIngredients = recipe.IngredientsList
                        .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(word => word.Trim().ToLower())
                        .ToList();

                    // Перевіряємо, чи кожне слово з requiredIngredients міститься хоча б в одному з recipeIngredients
                    bool allFound = requiredIngredients.All(req => recipeIngredients.Any(ri => ri.Contains(req)));

                    if (!allFound)
                        throw new InvalidOperationException("Рецепт не містить всі обов’язкові інгредієнти для цього конкурсу.");
                }

                // Забезпечуємо, що поле ContestRating для рецепта починається з 0,
                // щоб підрахунок конкурсних балів був чесним
                recipe.ContestRating = 0;

                // Додаємо рецепт до конкурсу
                contest.ContestRecipes.Add(recipe);
                var contestUpdate = Builders<Contest>.Update.Set(c => c.ContestRecipes, contest.ContestRecipes);
                await _contests.UpdateOneAsync(c => c.Id == contestId, contestUpdate);

                // Оновлюємо поле IsParticipatedInContest у рецепта
                var recipeUpdate = Builders<Recipe>.Update.Set(r => r.IsParticipatedInContest, true);
                await _recipes.UpdateOneAsync(r => r.Id == recipeId, recipeUpdate);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка додавання рецепта до конкурсу: {ex.Message}");
            }
        }

        // Створити конкурс
        public async Task<Contest> CreateContestAsync(Contest newContest, string userId)
        {
            try
            {
                if (newContest == null)
                    throw new ArgumentNullException(nameof(newContest), "Дані конкурсу не можуть бути порожніми.");

                if (string.IsNullOrWhiteSpace(userId))
                    throw new UnauthorizedAccessException("Користувач не авторизований.");

                // 🔹 Отримуємо користувача з бази
                var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
                if (user == null)
                    throw new KeyNotFoundException("Користувача не знайдено.");

                // 🔹 Перевіряємо, чи є користувач адміністратором
                if (user.RoleId != 1) // Переконайся, що "Admin" відповідає назві ролі у твоїй базі
                    throw new UnauthorizedAccessException("Тільки адміністратор може створювати конкурси.");

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

        public async Task<List<Recipe>> DetermineContestWinnersAsync(string contestId, int topCount = 3)
        {
            // **Крок 1.** Отримуємо дані конкурсу за ID
            var contest = await _contests.Find(c => c.Id == contestId).FirstOrDefaultAsync();
            if (contest == null)
                throw new KeyNotFoundException("Конкурс не знайдено.");

            // Якщо в конкурсі немає учасників – повертаємо порожній список
            if (contest.ContestRecipes == null || contest.ContestRecipes.Count == 0)
                return new List<Recipe>();

            // **Крок 2.** Якщо конкурс ще не закритий, виконуємо обчислення та заморожуємо рейтинги
            if (!contest.IsClosed)
            {
                // Позначаємо конкурс як завершений
                contest.IsClosed = true;
                var updateContestStatus = Builders<Contest>.Update.Set(c => c.IsClosed, true);
                await _contests.UpdateOneAsync(c => c.Id == contestId, updateContestStatus);
            }
            // Якщо конкурс вже закритий, далі обчислення не виконуються, і ми використовуємо вже встановлені значення

            // **Крок 3.** Обчислюємо конкурсні бали для кожного рецепта, але лише якщо рейтинг ще не встановлено (тобто дорівнює 0)
            var candidateRecipes = new List<Recipe>();
            foreach (var recipe in contest.ContestRecipes)
            {
                // Якщо значення ContestRating ще не встановлено (0), обчислюємо його
                if (recipe.ContestRating == 0)
                {
                    // Отримуємо всі рейтинги для даного рецепта
                    var filter = Builders<Rating>.Filter.Eq(r => r.RecipeId, recipe.Id);
                    var ratingsList = await _ratings.Find(filter).ToListAsync();

                    // Обчислюємо конкурсний бал: оцінка 4 дає 1 бал, оцінка 5 дає 2 бали
                    int count4 = ratingsList.Count(r => r.Likes == 4);
                    int count5 = ratingsList.Count(r => r.Likes == 5);
                    int contestRating = count4 * 1 + count5 * 2;

                    // Записуємо обчислений бал у поле ContestRating
                    recipe.ContestRating = contestRating;

                    // **За бажанням:** Якщо рецепти зберігаються окремо в базі даних, можна виконати оновлення документу:
                    // var updateRecipe = Builders<Recipe>.Update.Set(r => r.ContestRating, contestRating);
                    // await _recipes.UpdateOneAsync(r => r.Id == recipe.Id, updateRecipe);
                }

                // Додаємо рецепт до кандидатів, якщо його середній рейтинг на момент закриття конкурсу не нижчий за 4
                if (recipe.AverageRating >= 4)
                {
                    candidateRecipes.Add(recipe);
                }
            }

            // **Крок 4.** Сортуємо кандидатів за спаданням конкурсного рейтингу та обираємо топ-N рецептів
            var winningRecipes = candidateRecipes.OrderByDescending(r => r.ContestRating).ToList();
            var topRecipes = winningRecipes.Take(topCount).ToList();

            // Оновлюємо поле WinningRecipes у конкурсі (зберігається повний список учасників у ContestRecipes)
            contest.WinningRecipes = topRecipes;
            var updateWinning = Builders<Contest>.Update.Set(c => c.WinningRecipes, contest.WinningRecipes);
            await _contests.UpdateOneAsync(c => c.Id == contestId, updateWinning);

            // **Крок 5.** Відправляємо повідомлення учасникам конкурсу
            // Отримуємо унікальні ID авторів-учасників
            var participantAuthorIds = contest.ContestRecipes
                .Select(r => r.AuthorId)
                .Distinct()
                .ToList();
            var winningAuthorIds = contest.WinningRecipes
                .Select(r => r.AuthorId)
                .ToList();

            foreach (var authorId in participantAuthorIds)
            {
                // Отримуємо дані користувача за його ID
                var user = await _users.Find(u => u.Id == authorId).FirstOrDefaultAsync();
                if (user == null || string.IsNullOrWhiteSpace(user.Email))
                    continue; // Пропускаємо, якщо немає даних чи email

                string subject = "Конкурс завершено: перегляньте результати";
                string body = "Шановний учаснику, конкурс завершено. Будь ласка, перегляньте результати конкурсу.";

                // Якщо користувач є автором рецепта-переможця, додаємо привітальне повідомлення
                if (winningAuthorIds.Contains(authorId))
                {
                    body = "Вітаємо! Ваш рецепт переміг у конкурсі!";
                }

                // Відправляємо повідомлення (метод SendEmailAsync повинен бути реалізований у IEmailService)
                await _emailService.SendEmailAsync(user.Email, subject, body);
            }

            return topRecipes;
        }

    }
}
