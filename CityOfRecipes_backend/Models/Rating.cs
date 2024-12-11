using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace CityOfRecipes_backend.Models
{
    public class Rating
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        [BsonElement("RecipeId")]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonRequired]
        public string RecipeId { get; set; } = null!;

        [BsonElement("UserId")]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonRequired]
        public string UserId { get; set; } = null!;

        [BsonElement("Likes")]
        [BsonRequired]
        public int Likes { get; set; } 

        [BsonElement("DateTime")]
        [BsonRequired]
        public DateTime DateTime { get; set; } = DateTime.UtcNow;
        public void Validate()
        {
            if (Likes < 1 || Likes > 5)
                throw new ArgumentException("Оцінка має бути від 1 до 5 зірок.");
        }

    }
}
