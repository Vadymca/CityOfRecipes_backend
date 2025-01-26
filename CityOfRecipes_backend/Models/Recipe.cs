using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using Microsoft.AspNetCore.Server.HttpSys;

namespace CityOfRecipes_backend.Models
{
    [Flags]
    public enum Holiday
    {
        None = 0,
        Christmas = 1,
        NewYear = 2,
        Children = 4,
        Easter = 8,
        FirstCourses = 16,
        SecondCourses = 32,
        Salads = 64,
        Baking = 128,
        Drinks = 256,
        Canning = 512,
        Grill = 1024
    }
    public class Recipe
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("CategoryId")]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfNull]
        public string CategoryId { get; set; } = string.Empty;

        [BsonElement("AuthorId")]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfNull]
        public string AuthorId { get; set; } = string.Empty;

        [BsonElement("RecipeName")]
        [BsonRequired]
        [BsonIgnoreIfNull]
        public string RecipeName { get; set; } = string.Empty;

        [BsonElement("PreparationTimeMinutes")]
        [BsonIgnoreIfNull]
        public int PreparationTimeMinutes { get; set; } // Час приготування в хвилинах

        [BsonElement("CreatedAt")]
        [BsonIgnoreIfNull]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("IngredientsList")]
        [BsonIgnoreIfNull]
        public string IngredientsList { get; set; } = string.Empty; // Список інгредієнтів у довільному форматі

        [BsonElement("IngredientsText")]
        [BsonIgnoreIfNull]
        public string IngredientsText { get; set; } = string.Empty;

        [BsonElement("Ingredients")]
        [BsonIgnoreIfNull]
        public List<string> Ingredients { get; set; } = new(); // Оброблені інгредієнти

        [BsonElement("InstructionsText")]
        [BsonIgnoreIfNull]
        public string InstructionsText { get; set; } = string.Empty;

        [BsonElement("VideoUrl")]
        [BsonIgnoreIfNull]
        public string? VideoUrl { get; set; }

        [BsonElement("PhotoUrl")]
        [BsonIgnoreIfNull]
        public string? PhotoUrl { get; set; }

        [BsonElement("AverageRating")]
        [BsonIgnoreIfNull]
        public double AverageRating { get; set; } = 0.0;

        [BsonElement("TotalRatings")]
        [BsonIgnoreIfNull]
        public int TotalRatings { get; set; } = 0; // Загальна кількість оцінок

        [BsonElement("Slug")]
        [BsonIgnoreIfNull]
        public string Slug { get; set; } = string.Empty;

        [BsonElement("TagsText")]
        [BsonIgnoreIfNull]
        public string TagsText { get; set; } = string.Empty;

        [BsonElement("Tags")]
        [BsonIgnoreIfNull]
        public List<string> Tags { get; set; } = new(); // Оброблені теги

        public bool IsParticipatedInContest { get; set; } = false; // За замовчуванням - не брав участі

        [BsonIgnoreIfNull]
        public Holiday Holidays { get; set; } = Holiday.None;// Святкові прапори
        public void Validate()
        {
            if (RecipeName.Length > 200)
                throw new ArgumentException("Ім'я рецепта не повинно перевищувати 200 символів.");
            if (IngredientsText.Length < 10)
                throw new ArgumentException("Текст інгрідієнту має містити не менше 10 символів.");
            if (InstructionsText.Length < 10)
                throw new ArgumentException("Текст інструкції має містити не менше 10 символів.");
            if (Slug.Length > 255)
                throw new ArgumentException("Слаг перевищує максимальну довжину в 255 символів.");
        }
    }
}
