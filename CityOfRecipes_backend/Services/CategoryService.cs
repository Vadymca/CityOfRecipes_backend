using CityOfRecipes_backend.Models;
using MongoDB.Driver;

namespace CityOfRecipes_backend.Services
{
    public class CategoryService
    {
        private readonly IMongoCollection<Category> _categories;

        public CategoryService(MongoDbContext context)
        {
            _categories = context.GetCollection<Category>("Categories");
        }

        // Отримати всі категорії
        public async Task<List<Category>> GetAllAsync()
        {
            return await _categories.Find(_ => true).ToListAsync();
        }

        // Отримати категорію за ID
        public async Task<Category> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id не може бути порожнім.");

            var category = await _categories.Find(c => c.Id == id).FirstOrDefaultAsync();
            if (category == null)
                throw new KeyNotFoundException($"Категорію з ID {id} не знайдено.");

            return category;
        }

        // Створити нову категорію
        public async Task CreateAsync(Category newCategory)
        {
            newCategory.Validate();

            // Перевіряємо, чи існує категорія з таким самим слагом
            var existingCategory = await _categories.Find(c => c.Slug == newCategory.Slug).FirstOrDefaultAsync();
            if (existingCategory != null)
                throw new InvalidOperationException("Категорія з таким слагом вже існує.");

            await _categories.InsertOneAsync(newCategory);
        }

        // Оновити категорію
        public async Task UpdateAsync(string id, Category updatedCategory)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id не може бути порожнім.");

            updatedCategory.Validate();

            var result = await _categories.ReplaceOneAsync(c => c.Id == id, updatedCategory);
            if (result.MatchedCount == 0)
                throw new KeyNotFoundException($"Категорію з ID {id} не знайдено.");
        }

        // Видалити категорію
        public async Task DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Id не може бути порожнім.");

            var result = await _categories.DeleteOneAsync(c => c.Id == id);
            if (result.DeletedCount == 0)
                throw new KeyNotFoundException($"Категорію з ID {id} не знайдено.");
        }
    }
}
