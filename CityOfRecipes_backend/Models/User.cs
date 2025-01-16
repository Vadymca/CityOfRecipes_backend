using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace CityOfRecipes_backend.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("RoleId")]
        [BsonIgnoreIfDefault]
        public int RoleId { get; set; } = 0;

        [BsonElement("Email")]
        [BsonRequired]
        public string Email { get; set; } = string.Empty;

        [BsonElement("Password")]
        [BsonRequired]
        [BsonIgnoreIfNull]
        [MinLength(6, ErrorMessage = "Пароль має бути не менше 6 символів.")]
        public string? PasswordHash { get; set; }

        [BsonElement("RegistrationDate")]
        [BsonRequired]
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        [BsonElement("FirstName")]
        [BsonIgnoreIfNull]
        [MinLength(1, ErrorMessage = "Ім'я не може бути порожнім.")]
        public string FirstName { get; set; } = string.Empty;

        [BsonElement("LastName")]
        [BsonIgnoreIfNull]
        [MinLength(1, ErrorMessage = "Прізвище не може бути порожнім.")]
        public string LastName { get; set; } = string.Empty;

        [BsonElement("About")]
        [BsonIgnoreIfNull]
        [MaxLength(500, ErrorMessage = "Текст 'About' не має перевищувати 500 символів.")]
        public string? About { get; set; }

        [BsonElement("ProfilePhotoUrl")]
        [BsonIgnoreIfNull]
        public string ProfilePhotoUrl { get; set; }

        [BsonElement("CityId")]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonIgnoreIfNull]
        public string? CityId { get; set; } 
        [BsonElement("Rating")]
        public double Rating { get; set; } = 0;

        [BsonElement("FavoriteRecipes")]
        [BsonIgnoreIfNull]
        public ICollection<Recipe>? FavoriteRecipes { get; set; }

        [BsonElement("FavoriteAuthors")]
        [BsonIgnoreIfNull]
        public ICollection<User>? FavoriteAuthors { get; set; }

        [BsonElement("EmailConfirmed")]
        public bool EmailConfirmed { get; set; } = false;
        public string? EmailConfirmationToken { get; set; } = null;
        public string? PasswordResetToken { get; set; } = null;

        [BsonElement("TemporaryBan")]
        public bool TemporaryBan { get; set; } = false;

        [BsonElement("PermanentBan")]
        public bool PermanentBan { get; set; } = false;

    }
}
