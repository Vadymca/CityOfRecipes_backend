using CityOfRecipes_backend.Models;
using MongoDB.Driver;

namespace CityOfRecipes_backend.Services
{
    public class RatingService
    {
        private readonly IMongoCollection<Rating> _ratings;
        private readonly IMongoCollection<Recipe> _recipes;
        private readonly IMongoCollection<User> _users;

        public RatingService(MongoDbContext context)
        {
            _ratings = context.GetCollection<Rating>("Ratings");
            _recipes = context.GetCollection<Recipe>("Recipes");
            _users = context.GetCollection<User>("Users");
        }

        public async Task<bool> AddOrUpdateRatingAsync(string recipeId, string userId, int ratingValue)
        {
            if (string.IsNullOrWhiteSpace(recipeId) || string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("RecipeId і UserId не можуть бути порожніми.");

            if (ratingValue < 1 || ratingValue > 5)
                throw new ArgumentException("Оцінка повинна бути в діапазоні від 1 до 5.");

            try
            {
                var filter = Builders<Rating>.Filter.Where(r => r.RecipeId == recipeId && r.UserId == userId);
                var existingRating = await _ratings.Find(filter).FirstOrDefaultAsync();

                if (existingRating != null)
                {
                    existingRating.Likes = ratingValue;
                    existingRating.DateTime = DateTime.UtcNow;
                    await _ratings.ReplaceOneAsync(filter, existingRating);
                }
                else
                {
                    var newRating = new Rating
                    {
                        RecipeId = recipeId,
                        UserId = userId,
                        Likes = ratingValue,
                        DateTime = DateTime.UtcNow
                    };

                    await _ratings.InsertOneAsync(newRating);
                }

                // Перерахунок середньої оцінки рецепта
                await RecalculateRecipeRating(recipeId);

                // Перерахунок середнього рейтингу автора рецепта
                await RecalculateUserRating(recipeId);

                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка під час додавання/оновлення оцінки: {ex.Message}", ex);
            }
        }
        private async Task RecalculateRecipeRating(string recipeId)
        {
            var filter = Builders<Rating>.Filter.Eq(r => r.RecipeId, recipeId);
            var ratings = await _ratings.Find(filter).ToListAsync();

            if (ratings == null || !ratings.Any())
            {
                // Якщо немає оцінок, оновлюємо рейтинг на 0
                var resetUpdate = Builders<Recipe>.Update
                    .Set(r => r.AverageRating, 0)
                    .Set(r => r.TotalRatings, 0);

                await _recipes.UpdateOneAsync(Builders<Recipe>.Filter.Eq(r => r.Id, recipeId), resetUpdate);
                return;
            }

            double newAverage = ratings.Average(r => r.Likes);
            int totalRatings = ratings.Count;

            var updateRecipe = Builders<Recipe>.Update
                .Set(r => r.AverageRating, newAverage)
                .Set(r => r.TotalRatings, totalRatings);

            await _recipes.UpdateOneAsync(Builders<Recipe>.Filter.Eq(r => r.Id, recipeId), updateRecipe);
        }

        private async Task RecalculateUserRating(string recipeId)
        {
            // Отримуємо рецепт, щоб дізнатися його автора
            var recipe = await _recipes.Find(r => r.Id == recipeId).FirstOrDefaultAsync();
            if (recipe == null) return;

            string authorId = recipe.AuthorId;

            // Отримуємо всі рецепти цього автора
            var authorRecipes = await _recipes.Find(r => r.AuthorId == authorId).ToListAsync();
            if (!authorRecipes.Any()) return;

            // Отримуємо всі оцінки для цих рецептів
            var recipeIds = authorRecipes.Select(r => r.Id).ToList();
            var filter = Builders<Rating>.Filter.In(r => r.RecipeId, recipeIds);
            var ratings = await _ratings.Find(filter).ToListAsync();

            if (ratings.Any())
            {
                double newAverage = ratings.Average(r => r.Likes);

                var updateUser = Builders<User>.Update.Set(u => u.Rating, newAverage);
                await _users.UpdateOneAsync(Builders<User>.Filter.Eq(u => u.Id, authorId), updateUser);
            }
        }
    }
}
