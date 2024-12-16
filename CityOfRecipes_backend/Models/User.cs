using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace CityOfRecipes_backend.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("RoleId")]
        [BsonRequired]
        public int RoleId { get; set; }

        [BsonElement("Email")]
        [BsonRequired]
        public string Email { get; set; } = string.Empty;

        [BsonElement("Password")]
        [BsonRequired]
        [BsonIgnoreIfNull]
        public string? Password { get; set; }

        [BsonElement("RegistrationDate")]
        [BsonRequired]
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        [BsonElement("FirstName")]
        [BsonRequired]
        public string FirstName { get; set; } = string.Empty;

        [BsonElement("LastName")]
        [BsonRequired]
        public string LastName { get; set; } = string.Empty;

        [BsonElement("About")]
        [BsonIgnoreIfNull]
        public string? About { get; set; }

        [BsonElement("ProfilePhotoUrl")]
        [BsonIgnoreIfNull]
        public string? ProfilePhotoUrl { get; set; }

        [BsonElement("CountryId")]
        [BsonIgnoreIfNull]
        public int? CountryId { get; set; }

        [BsonElement("Country")]
        [BsonIgnoreIfNull]
        public string? Country { get; set; }

        [BsonElement("City")]
        [BsonIgnoreIfNull]
        public string? City { get; set; }

        [BsonElement("Rating")]
        public double Rating { get; set; } = 0;

        [BsonElement("FavoriteRecipes")]
        [BsonIgnoreIfNull]
        public ICollection<string>? FavoriteRecipes { get; set; }

        [BsonElement("FavoriteAuthors")]
        [BsonIgnoreIfNull]
        public ICollection<string>? FavoriteAuthors { get; set; }

        [BsonElement("EmailConfirmed")]
        public bool EmailConfirmed { get; set; } = false;

        [BsonElement("TemporaryBan")]
        public bool TemporaryBan { get; set; } = false;

        [BsonElement("PermanentBan")]
        public bool PermanentBan { get; set; } = false;

        public void Validate()
        {
            if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(Email))
                throw new ArgumentException("Формат електронної пошти невірний.");
            if (Password.Length < 6)
                throw new ArgumentException("Пароль має бути не менше 6 символів.");
            if (About?.Length > 500)
                throw new ArgumentException("Текст 'About' не має перевищувати 500 символів.");
            if (Country?.Length > 100)
                throw new ArgumentException("Країна не повинно перевищувати 100 символів.");
            if (City?.Length > 100)
                throw new ArgumentException("Місто не повинно перевищувати 100 символів.");
            if (string.IsNullOrWhiteSpace(FirstName))
                throw new ArgumentException("Ім'я не може бути порожнім.");
            if (string.IsNullOrWhiteSpace(LastName))
                throw new ArgumentException("Прізвище не може бути порожнім.");
        }
    }
}
