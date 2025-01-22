using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace CityOfRecipes_backend.DTOs
{
    public class AboutUserDto
    {
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_id")]
        public string? Id { get; set; } // Поле Id може бути null

        [BsonIgnoreIfNull]
        public int RoleId { get; set; } = 0;

        [BsonIgnoreIfNull]
        public string? Email { get; set; } // Email зроблено необов’язковим

        [BsonIgnoreIfNull]
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        [BsonIgnoreIfNull]
        public string? FirstName { get; set; } // Ім'я може бути null

        [BsonIgnoreIfNull]
        public string? LastName { get; set; } // Прізвище може бути null

        [BsonIgnoreIfNull]
        public string? About { get; set; } // Поле About необов’язкове

        [BsonIgnoreIfNull]
        public double Rating { get; set; } = 0;

        [BsonIgnoreIfNull]
        public string? ProfilePhotoUrl { get; set; } // Фото профілю необов’язкове

        [BsonIgnoreIfNull]
        public string? City { get; set; } // Місто може бути null

        [BsonIgnoreIfNull]
        public string? Country { get; set; } // Країна може бути null

        [BsonIgnoreIfNull]
        public bool EmailConfirmed { get; set; } = false;

    }
}
