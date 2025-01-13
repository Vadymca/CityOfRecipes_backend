using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace CityOfRecipes_backend.DTOs
{
    public class UserDto
    {
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_id")]
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        [BsonElement("Password")]
        [BsonRequired]
        [BsonIgnoreIfNull]
        [MinLength(6, ErrorMessage = "Пароль має бути не менше 6 символів.")]
        public string? Password { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string About { get; set; } = string.Empty;
        public string ProfilePhotoUrl { get; set; } = string.Empty;
        public string City { get; set; } = "Невідоме місто";
        public string Country { get; set; } = "Невідома країна";
        public List<RecipeDto> FavoriteRecipes { get; set; } = new();
        public List<AuthorDto> FavoriteAuthors { get; set; } = new();
 
    }
}
