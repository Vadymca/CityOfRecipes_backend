using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace CityOfRecipes_backend.Models
{
    public class Country
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        [BsonElement("CountryName")]
        [BsonRequired]
        public string CountryName { get; set; } = string.Empty;
        public void Validate()
        {
            if (CountryName.Length > 100)
                throw new ArgumentException("Назва країни перевищує максимальну довжину в 100 символів.");
        }

    }
}
