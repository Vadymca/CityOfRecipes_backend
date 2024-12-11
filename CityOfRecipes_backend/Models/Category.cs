using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace CityOfRecipes_backend.Models
{
    public class Category
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        [BsonElement("CategoryName")]
        [BsonRequired]
        [BsonRepresentation(BsonType.String)]
        [BsonIgnoreIfNull]
        public string CategoryName { get; set; } = string.Empty;

        [BsonElement("Slug")]
        [BsonRequired]
        [BsonRepresentation(BsonType.String)]
        [BsonIgnoreIfNull]
        public string Slug { get; set; } = string.Empty;
        public void Validate()
        {
            if (CategoryName.Length > 100)
                throw new ArgumentException("Назва категорії перевищує максимальну довжину в 100 символів.");
            if (Slug.Length > 100)
                throw new ArgumentException("Слаг перевищує максимальну довжину в 100 символів.");
        }

    }
}
