using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace CityOfRecipes_backend.DTOs
{
    public class UserDto
    {
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_id")]
        public string? Id { get; set; } // Поле Id може бути null

        [BsonIgnoreIfNull]
        public string? Email { get; set; } // Email зроблено необов’язковим

        [BsonIgnoreIfNull]
        public string? Password { get; set; } // Password зроблено необов’язковим

        [BsonIgnoreIfNull]
        public string? FirstName { get; set; } // Ім'я може бути null

        [BsonIgnoreIfNull]
        public string? LastName { get; set; } // Прізвище може бути null

        [BsonIgnoreIfNull]
        public string? About { get; set; } // Поле About необов’язкове

        [BsonIgnoreIfNull]
        public string? ProfilePhotoUrl { get; set; } // Фото профілю необов’язкове

        [BsonIgnoreIfNull]
        public string? CityId { get; set; } // Місто може бути null
        
    }
}
