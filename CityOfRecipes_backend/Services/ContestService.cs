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

        public async Task<List<ContestDto>> GetActiveContestsAsync()
        {
            try
            {
                var now = DateTime.Now;
                var contests = await _contests
                    .Find(c => c.StartDate <= now && c.EndDate >= now)
                    .ToListAsync();

                // Якщо відповідних конкурсів немає, повертаємо порожній список
                if (contests == null || contests.Count == 0)
                    return new List<ContestDto>();

                // Для активних конкурсів (IsClosed == false) обчислюємо конкурсний рейтинг та середню оцінку для кожного рецепта "на льоту"
                foreach (var contest in contests)
                {
                    if (!contest.IsClosed)
                    {
                        foreach (var recipe in contest.ContestRecipes)
                        {
                            // Отримуємо всі оцінки для даного рецепта
                            var filter = Builders<Rating>.Filter.Eq(r => r.RecipeId, recipe.Id);
                            var ratingsList = await _ratings.Find(filter).ToListAsync();

                            // Обчислюємо конкурсний рейтинг: оцінка 4 дає 1 бал, оцінка 5 дає 2 бали
                            int count4 = ratingsList.Count(r => r.Likes == 4);
                            int count5 = ratingsList.Count(r => r.Likes == 5);
                            recipe.ContestRating = count4 * 1 + count5 * 2;

                            // Обчислюємо середню оцінку (AverageRating) як середнє значення Likes, або 0 якщо оцінок немає
                            recipe.AverageRating = ratingsList.Any() ? ratingsList.Average(r => r.Likes) : 0;
                        }
                    }
                }

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


        public async Task<List<ContestDto>> GetFinishedContestsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var contests = await _contests.Find(c => c.EndDate < now).ToListAsync();

                // Якщо відповідних конкурсів немає, повертаємо порожній список
                if (contests == null || contests.Count == 0)
                    return new List<ContestDto>();

                // Маппінг з моделі Contest до ContestDto із заміною значень ContestRating з FinalContestRatings
                var contestDtos = contests.Select(c =>
                {
                    // Маппінг для списку рецептів конкурсу
                    var contestRecipeDtos = c.ContestRecipes.Select(r =>
                    {
                        int finalRating = r.ContestRating; // Поточне значення за замовчуванням
                        if (c.FinalContestRatings != null && c.FinalContestRatings.Any())
                        {
                            var fixedRating = c.FinalContestRatings.FirstOrDefault(fr => fr.RecipeId == r.Id);
                            if (fixedRating != null)
                                finalRating = fixedRating.ContestRating;
                        }

                        return new ContestRecipeDto
                        {
                            Id = r.Id,
                            Slug = r.Slug,
                            RecipeName = r.RecipeName,
                            PhotoUrl = r.PhotoUrl,
                            AuthorId = r.AuthorId,
                            CategoryId = r.CategoryId,
                            AverageRating = r.AverageRating,
                            ContestRating = finalRating
                        };
                    }).ToList();

                    // Маппінг для переможців (якщо є)
                    var winningRecipeDtos = c.WinningRecipes.Select(r =>
                    {
                        int finalRating = r.ContestRating;
                        if (c.FinalContestRatings != null && c.FinalContestRatings.Any())
                        {
                            var fixedRating = c.FinalContestRatings.FirstOrDefault(fr => fr.RecipeId == r.Id);
                            if (fixedRating != null)
                                finalRating = fixedRating.ContestRating;
                        }

                        return new ContestRecipeDto
                        {
                            Id = r.Id,
                            Slug = r.Slug,
                            RecipeName = r.RecipeName,
                            PhotoUrl = r.PhotoUrl,
                            AuthorId = r.AuthorId,
                            CategoryId = r.CategoryId,
                            AverageRating = r.AverageRating,
                            ContestRating = finalRating
                        };
                    }).ToList();

                    return new ContestDto
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
                        ContestRecipes = contestRecipeDtos,
                        WinningRecipes = winningRecipeDtos
                    };
                }).ToList();

                return contestDtos;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка отримання завершених конкурсів: {ex.Message}");
            }
        }

        public async Task<ContestDto?> GetContestByIdAsync(string contestId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(contestId))
                    throw new ArgumentException("ID конкурсу не може бути порожнім.");

                var contest = await _contests.Find(c => c.Id == contestId).FirstOrDefaultAsync();
                if (contest == null)
                    throw new KeyNotFoundException($"Конкурс з ID {contestId} не знайдено.");

                // Маппінг для ContestRecipes з підставленням зафіксованих рейтингів, якщо конкурс закритий
                var contestRecipeDtos = contest.ContestRecipes.Select(r =>
                {
                    int finalRating = r.ContestRating; // Поточне значення за замовчуванням
                    if (contest.IsClosed && contest.FinalContestRatings != null && contest.FinalContestRatings.Any())
                    {
                        var fixedRating = contest.FinalContestRatings.FirstOrDefault(fr => fr.RecipeId == r.Id);
                        if (fixedRating != null)
                            finalRating = fixedRating.ContestRating;
                    }

                    return new ContestRecipeDto
                    {
                        Id = r.Id,
                        Slug = r.Slug,
                        RecipeName = r.RecipeName,
                        PhotoUrl = r.PhotoUrl,
                        AuthorId = r.AuthorId,
                        CategoryId = r.CategoryId,
                        AverageRating = r.AverageRating,
                        ContestRating = finalRating
                    };
                }).ToList();

                // Маппінг для WinningRecipes з підставленням зафіксованих рейтингів, якщо конкурс закритий
                var winningRecipeDtos = contest.WinningRecipes.Select(r =>
                {
                    int finalRating = r.ContestRating;
                    if (contest.IsClosed && contest.FinalContestRatings != null && contest.FinalContestRatings.Any())
                    {
                        var fixedRating = contest.FinalContestRatings.FirstOrDefault(fr => fr.RecipeId == r.Id);
                        if (fixedRating != null)
                            finalRating = fixedRating.ContestRating;
                    }

                    return new ContestRecipeDto
                    {
                        Id = r.Id,
                        Slug = r.Slug,
                        RecipeName = r.RecipeName,
                        PhotoUrl = r.PhotoUrl,
                        AuthorId = r.AuthorId,
                        CategoryId = r.CategoryId,
                        AverageRating = r.AverageRating,
                        ContestRating = finalRating
                    };
                }).ToList();

                // Формуємо DTO для конкурсу
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
                    ContestRecipes = contestRecipeDtos,
                    WinningRecipes = winningRecipeDtos
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
                if (contest == null)
                    throw new KeyNotFoundException($"Конкурс з slug '{slug}' не знайдено.");

                // Маппінг для списку рецептів учасників з підстановкою зафіксованих рейтингів,
                // якщо конкурс закритий
                var contestRecipeDtos = contest.ContestRecipes.Select(r =>
                {
                    int finalRating = r.ContestRating; // за замовчуванням беремо поточне значення
                    if (contest.IsClosed && contest.FinalContestRatings != null && contest.FinalContestRatings.Any())
                    {
                        var fixedRating = contest.FinalContestRatings.FirstOrDefault(fr => fr.RecipeId == r.Id);
                        if (fixedRating != null)
                            finalRating = fixedRating.ContestRating;
                    }
                    return new ContestRecipeDto
                    {
                        Id = r.Id,
                        Slug = r.Slug,
                        RecipeName = r.RecipeName,
                        PhotoUrl = r.PhotoUrl,
                        AuthorId = r.AuthorId,
                        CategoryId = r.CategoryId,
                        AverageRating = r.AverageRating,
                        ContestRating = finalRating
                    };
                }).ToList();

                // Маппінг для переможців з підстановкою зафіксованих рейтингів
                var winningRecipeDtos = contest.WinningRecipes.Select(r =>
                {
                    int finalRating = r.ContestRating;
                    if (contest.IsClosed && contest.FinalContestRatings != null && contest.FinalContestRatings.Any())
                    {
                        var fixedRating = contest.FinalContestRatings.FirstOrDefault(fr => fr.RecipeId == r.Id);
                        if (fixedRating != null)
                            finalRating = fixedRating.ContestRating;
                    }
                    return new ContestRecipeDto
                    {
                        Id = r.Id,
                        Slug = r.Slug,
                        RecipeName = r.RecipeName,
                        PhotoUrl = r.PhotoUrl,
                        AuthorId = r.AuthorId,
                        CategoryId = r.CategoryId,
                        AverageRating = r.AverageRating,
                        ContestRating = finalRating
                    };
                }).ToList();

                // Формування DTO конкурсу
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
                    ContestRecipes = contestRecipeDtos,
                    WinningRecipes = winningRecipeDtos
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

                var contestDtos = contests.Select(c =>
                {
                    // Маппінг для рецептів, що беруть участь у конкурсі
                    var contestRecipeDtos = c.ContestRecipes.Select(r =>
                    {
                        int finalRating = r.ContestRating; // за замовчуванням поточне значення
                        if (c.IsClosed && c.FinalContestRatings != null && c.FinalContestRatings.Any())
                        {
                            var fixedRating = c.FinalContestRatings.FirstOrDefault(fr => fr.RecipeId == r.Id);
                            if (fixedRating != null)
                                finalRating = fixedRating.ContestRating;
                        }

                        return new ContestRecipeDto
                        {
                            Id = r.Id,
                            Slug = r.Slug,
                            RecipeName = r.RecipeName,
                            PhotoUrl = r.PhotoUrl,
                            AuthorId = r.AuthorId,
                            CategoryId = r.CategoryId,
                            AverageRating = r.AverageRating,
                            ContestRating = finalRating
                        };
                    }).ToList();

                    // Маппінг для переможців, якщо такі є
                    var winningRecipeDtos = c.WinningRecipes.Select(r =>
                    {
                        int finalRating = r.ContestRating;
                        if (c.IsClosed && c.FinalContestRatings != null && c.FinalContestRatings.Any())
                        {
                            var fixedRating = c.FinalContestRatings.FirstOrDefault(fr => fr.RecipeId == r.Id);
                            if (fixedRating != null)
                                finalRating = fixedRating.ContestRating;
                        }
                        return new ContestRecipeDto
                        {
                            Id = r.Id,
                            Slug = r.Slug,
                            RecipeName = r.RecipeName,
                            PhotoUrl = r.PhotoUrl,
                            AuthorId = r.AuthorId,
                            CategoryId = r.CategoryId,
                            AverageRating = r.AverageRating,
                            ContestRating = finalRating
                        };
                    }).ToList();

                    return new ContestDto
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
                        ContestRecipes = contestRecipeDtos,
                        WinningRecipes = winningRecipeDtos
                    };
                }).ToList();

                return contestDtos;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка отримання конкурсів для рецепта: {ex.Message}");
            }
        }

        public async Task<List<ContestDto>> GetAvailableContestsForRecipeAsync(string recipeId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(recipeId))
                    throw new ArgumentException("ID рецепта не може бути порожнім.");

                var recipe = await _recipes.Find(r => r.Id == recipeId).FirstOrDefaultAsync();
                if (recipe == null)
                    throw new KeyNotFoundException("Рецепт не знайдено.");

                var now = DateTime.Now;
                var contests = await _contests.Find(c =>
                    c.StartDate <= now && c.EndDate >= now &&
                    (string.IsNullOrEmpty(c.CategoryId) || c.CategoryId == recipe.CategoryId) &&
                    !c.ContestRecipes.Any(r => r.Id == recipeId) // Перевіряємо, що рецепт ще не бере участь
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
                            .Split(new[] { ',', ';', '—' }, StringSplitOptions.RemoveEmptyEntries) // Додаємо '—' як роздільник
                            .Select(word => word.Trim().ToLower().Split(' ')[0]) // Беремо тільки перше слово (щоб "імбир - 1шт" стало "імбир")
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
            // Крок 1. Отримуємо дані конкурсу за ID
            var contest = await _contests.Find(c => c.Id == contestId).FirstOrDefaultAsync();
            if (contest == null)
                throw new KeyNotFoundException("Конкурс не знайдено.");

            if (contest.ContestRecipes == null || contest.ContestRecipes.Count == 0)
                return new List<Recipe>();

            // Якщо конкурс уже закритий – використовуємо збережені результати
            if (contest.IsClosed)
            {
                // Замінюємо в кожному рецепті значення конкурсного рейтингу на зафіксоване з масиву FinalContestRatings
                foreach (var recipe in contest.ContestRecipes)
                {
                    var finalRating = contest.FinalContestRatings.FirstOrDefault(f => f.RecipeId == recipe.Id);
                    if (finalRating != null)
                        recipe.ContestRating = finalRating.ContestRating;
                }
                return contest.WinningRecipes ?? new List<Recipe>();
            }

            // Крок 2. Конкурс ще активний – позначаємо його як завершений
            contest.IsClosed = true;
            var updateContestStatus = Builders<Contest>.Update.Set(c => c.IsClosed, true);

            // Крок 3. Обчислюємо конкурсні бали для кожного рецепта (якщо ще не встановлено)
            foreach (var recipe in contest.ContestRecipes)
            {
                if (recipe.ContestRating == 0)
                {
                    var filter = Builders<Rating>.Filter.Eq(r => r.RecipeId, recipe.Id);
                    var ratingsList = await _ratings.Find(filter).ToListAsync();

                    int count4 = ratingsList.Count(r => r.Likes == 4);
                    int count5 = ratingsList.Count(r => r.Likes == 5);
                    recipe.ContestRating = count4 * 1 + count5 * 2;
                }
            }

            // Крок 4. Формуємо знімок конкурсних рейтингів – заповнюємо масив FinalContestRatings
            contest.FinalContestRatings = contest.ContestRecipes
                .Select(r => new FinalContestRating { RecipeId = r.Id, ContestRating = r.ContestRating })
                .ToList();

            // Відбираємо кандидатські рецепти, у яких середній рейтинг >= 4
            var candidateRecipes = contest.ContestRecipes.Where(r => r.AverageRating >= 4).ToList();

            // Сортуємо кандидатів за спаданням конкурсного рейтингу та обираємо топ-N переможців
            var topRecipes = candidateRecipes.OrderByDescending(r => r.ContestRating).Take(topCount).ToList();

            // Зберігаємо переможців у полі WinningRecipes
            contest.WinningRecipes = topRecipes;
            var updateWinning = Builders<Contest>.Update
                .Set(c => c.WinningRecipes, contest.WinningRecipes)
                .Set(c => c.FinalContestRatings, contest.FinalContestRatings);

            // Оновлюємо статус та результати конкурсу в базі даних
            await _contests.UpdateOneAsync(c => c.Id == contestId, Builders<Contest>.Update.Combine(updateContestStatus, updateWinning));

            // Крок 5. Відправляємо повідомлення учасникам  
            var participantAuthorIds = contest.ContestRecipes.Select(r => r.AuthorId).Distinct().ToList();
            var winningAuthorIds = contest.WinningRecipes.Select(r => r.AuthorId).ToList();

            string contestUrl = $"http://localhost:4200/contests/{contest.Slug}";
            string subject = $"Конкурс \"{contest.ContestName}\" завершено";

            foreach (var authorId in participantAuthorIds)
            {
                var user = await _users.Find(u => u.Id == authorId).FirstOrDefaultAsync();
                if (user == null || string.IsNullOrWhiteSpace(user.Email))
                    continue;

                string body;

                if (winningAuthorIds.Count == 0)
                {
                    // Жоден рецепт не переміг
                    body = $"Конкурс \"{contest.ContestName}\" завершився.\n\n" +
                           "Переможців визначено не було, оскільки жоден рецепт не виконав необхідну умову.";
                }
                else if (winningAuthorIds.Contains(authorId))
                {
                    // Користувач є переможцем
                    var winningRecipe = contest.WinningRecipes.FirstOrDefault(r => r.AuthorId == authorId);
                    string recipeName = winningRecipe != null ? winningRecipe.RecipeName : "Ваш рецепт";

                    body = $"Вітаємо! 🎉\n\nВаш рецепт \"{recipeName}\" потрапив до числа переможців у конкурсі \"{contest.ContestName}\"!\n" +
                           $"Всі деталі на сторінці конкурсу: {contestUrl}";
                }
                else
                {
                    // Конкурс має переможців, але цей користувач не серед них
                    body = $"Конкурс \"{contest.ContestName}\" завершився.\n\n" +
                           $"Було визначено переможців конкурсу. Ви можете переглянути результати тут: {contestUrl}";
                }

                await _emailService.SendEmailAsync(user.Email, subject, body);
            }

            return topRecipes;
        }


    }
}
