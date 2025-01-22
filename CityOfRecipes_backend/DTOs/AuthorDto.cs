using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace CityOfRecipes_backend.DTOs
{
    public class AuthorDto
    {
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_id")]
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string ProfilePhotoUrl { get; set; } = string.Empty;
        public string City { get; set; } = "Невідомe місто";
        public string Country { get; set; } = "Невідома країна";
        public DateTime RegistrationDate { get; set; }
        public double Rating { get; set; }
        public string About { get; set; } = string.Empty;

    }
}
