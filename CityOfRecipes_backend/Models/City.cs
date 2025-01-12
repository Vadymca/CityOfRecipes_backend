using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace CityOfRecipes_backend.Models
{
    public class City
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("CityName")]
        [BsonRequired]
        public string CityName { get; set; } = string.Empty;

        [BsonElement("CountryId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string CountryId { get; set; } = string.Empty;

        public void Validate()
        {
            if (CityName.Length > 100)
                throw new ArgumentException("Назва міста перевищує максимальну довжину в 100 символів.");
        }
    }
}
