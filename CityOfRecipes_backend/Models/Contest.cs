using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace CityOfRecipes_backend.Models
{
    public class Contest
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("ContestName")]
        [BsonRequired]
        public string ContestName { get; set; } = string.Empty;

        [BsonElement("PhotoUrl")]
        [BsonIgnoreIfNull]
        public string? PhotoUrl { get; set; }

        [BsonElement("StartDate")]
        [BsonRequired]
        public DateTime StartDate { get; set; }

        [BsonElement("EndDate")]
        [BsonRequired]
        public DateTime EndDate { get; set; }

        [BsonElement("RequiredIngredients")]
        public string? RequiredIngredients { get; set; }

        [BsonElement("ContestDetails")]
        public string? ContestDetails { get; set; }

        [BsonElement("CategoryId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? CategoryId { get; set; }


        [BsonElement("ContestRecipes")]
        public List<Recipe> ContestRecipes { get; set; } = new();

        [BsonElement("WinningRecipes")]
        public List<Recipe> WinningRecipes { get; set; } = new();

        [BsonElement("Slug")]
        [BsonRequired]
        public string Slug { get; set; } = string.Empty;

        public bool IsClosed { get; set; } = false;
        public void Validate()
        {
            if (ContestName.Length > 200)
                throw new ArgumentException("Назва конкурсу перевищує максимальну довжину в 200 символів.");
            
        }

    }
}
