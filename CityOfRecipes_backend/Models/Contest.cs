using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace CityOfRecipes_backend.Models
{
    public class Contest
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        [BsonElement("ContestName")]
        [BsonRequired]
        public string ContestName { get; set; } = string.Empty;

        [BsonElement("StartDate")]
        [BsonRequired]
        public DateTime StartDate { get; set; }

        [BsonElement("EndDate")]
        [BsonRequired]
        public DateTime EndDate { get; set; }

        [BsonElement("RequiredIngredients")]
        public string? RequiredIngredients { get; set; }

        [BsonElement("CategoryId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? CategoryId { get; set; }

        [BsonElement("InitialLikes")]
        public int InitialLikes { get; set; } = 0;

        [BsonElement("ContestRecipes")]
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> ContestRecipes { get; set; } = new();

        [BsonElement("WinningRecipeId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? WinningRecipeId { get; set; }

        [BsonElement("Slug")]
        [BsonRequired]
        public string Slug { get; set; } = string.Empty;
        public void Validate()
        {
            if (ContestName.Length > 200)
                throw new ArgumentException("Назва конкурсу перевищує максимальну довжину в 200 символів.");
            if (Slug.Length > 100)
                throw new ArgumentException("Слаг перевищує максимальну довжину в 100 символів.");
        }

    }
}
