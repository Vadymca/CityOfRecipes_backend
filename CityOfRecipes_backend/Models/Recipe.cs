using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using Microsoft.AspNetCore.Server.HttpSys;

namespace CityOfRecipes_backend.Models
{
    public class Recipe
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("CategoryId")]
        [BsonRepresentation(BsonType.ObjectId)]
        
        public string CategoryId { get; set; } = null!;

        [BsonElement("AuthorId")]
        [BsonRepresentation(BsonType.ObjectId)]
       
        public string AuthorId { get; set; } = null!;

        [BsonElement("CountryOfOriginId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? CountryOfOriginId { get; set; }

        [BsonElement("RecipeName")]
        [BsonRequired]
        public string RecipeName { get; set; } = null!;

        [BsonElement("CreatedAt")]
      
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("IngredientsText")]
        public string IngredientsText { get; set; } = string.Empty; // Вхідний текст інгредієнтів

        [BsonElement("Ingredients")]
        public List<string> Ingredients { get; set; } = new(); // Оброблені інгредієнти

        [BsonElement("InstructionsText")]
       
        public string InstructionsText { get; set; } = null!;

        [BsonElement("VideoUrl")]
        public string? VideoUrl { get; set; }

        [BsonElement("PhotoUrl")]
        public string? PhotoUrl { get; set; }

        [BsonElement("AverageRating")]
        public double AverageRating { get; set; } = 0.0;

        [BsonElement("Slug")]
        public string Slug { get; set; } = string.Empty;

        [BsonElement("TagsText")]
        public string TagsText { get; set; } = string.Empty; // Вхідний текст

        [BsonElement("Tags")]
        public List<string> Tags { get; set; } = new(); // Оброблені теги
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
