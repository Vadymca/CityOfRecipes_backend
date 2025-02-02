using CityOfRecipes_backend.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace CityOfRecipes_backend.Services
{
    public class TagService
    {
        private readonly IMongoCollection<Models.Tag> _tags;

        public TagService(IOptions<MongoDBSettings> mongoSettings)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _tags = database.GetCollection<Models.Tag>("Tags");
        }

        // Очищення рядка та перетворення в хештеги
        public List<string> ParseTags(string rawTags)
        {
            if (string.IsNullOrWhiteSpace(rawTags))
                return new List<string>();

            var tags = rawTags
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries) // Розділення по комі та пробілу
                .Select(tag => tag.Trim().ToLower()) // Видаляємо пробіли та переводимо в нижній регістр
                .Select(tag => tag.StartsWith("#") ? tag : $"#{tag}") // Додаємо решітку, якщо її немає
                .Where(tag => tag.Length > 1 && tag.Skip(1).All(char.IsLetterOrDigit)) // Переконуємося, що тег містить букви/цифри
                .Distinct() // Видаляємо дублікати
                .ToList();

            return tags;
        }

        // Оновлення загальної колекції тегів
        public async Task UpdateGlobalTagsAsync(List<string> tags)
        {
            foreach (var tagName in tags)
            {
                var filter = Builders<Models.Tag>.Filter.Eq(t => t.TagName, tagName);
                var update = Builders<Models.Tag>.Update.Inc(t => t.UsageCount, 1); // Збільшуємо UsageCount на 1
                var options = new UpdateOptions { IsUpsert = true }; // Якщо тег не знайдено, створюємо новий

                await _tags.UpdateOneAsync(filter, update, options);
            }
        }

        // Отримання всіх тегів
        public async Task<List<Models.Tag>> GetAllTagsAsync() =>
            await _tags.Find(_ => true).ToListAsync();

        // Отримання отримання найбільш популярних тегів
        public async Task<List<Models.Tag>> GetTopTagsAsync(int limit = 50)
        {
            var sort = Builders<Models.Tag>.Sort.Descending(t => t.UsageCount); // Сортування за UsageCount у порядку спадання
            return await _tags.Find(_ => true).Sort(sort).Limit(limit).ToListAsync();
        }

        // Отримання топових тегів
        public async Task<List<string>> GetTopTagNamesAsync(int limit = 50)
        {
            var sort = Builders<Models.Tag>.Sort.Descending(t => t.UsageCount); // Сортування за частотою використання
            var projection = Builders<Models.Tag>.Projection.Include(t => t.TagName); // Вибираємо лише TagName
            var tags = await _tags.Find(_ => true)
                                  .Sort(sort)
                                  .Limit(limit)
                                  .Project<Models.Tag>(projection)
                                  .ToListAsync();
            return tags.Select(t => t.TagName).ToList(); // Повертаємо лише імена тегів
        }

        // Видалення тегу (опціонально, якщо потрібно)
        public async Task RemoveTagAsync(string tagId)
        {
            var deleteResult = await _tags.DeleteOneAsync(tag => tag.Id == tagId);
            if (deleteResult.DeletedCount == 0)
                throw new KeyNotFoundException($"Тег з ID {tagId} не знайдено.");
        }
    }
}
