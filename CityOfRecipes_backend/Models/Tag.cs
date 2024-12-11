using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace CityOfRecipes_backend.Models
{
    public class Tag
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        [BsonElement("TagName")]
        [BsonRequired]
        public string TagName { get; set; } = string.Empty;

        [BsonElement("UsageCount")]
        public int UsageCount { get; set; } = 0; // Частота використання

        public void Validate()
        {
            if (TagName.Length > 50)
                throw new ArgumentException("Назва тегу перевищує максимальну довжину в 100 символів.");
        }
    }
}
