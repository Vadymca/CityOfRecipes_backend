using CityOfRecipes_backend.DTOs;
using CityOfRecipes_backend.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Security.Cryptography;

namespace CityOfRecipes_backend.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<City> _cities;
        private readonly IMongoCollection<Country> _countries;

        public UserService(IOptions<MongoDBSettings> mongoSettings)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _users = database.GetCollection<User>("Users");
            _cities = database.GetCollection<City>("Cities");
            _countries = database.GetCollection<Country>("Countries");
        }

        public async Task<List<AuthorDto>> GetAsync(int start, int limit)
        {
            var pipeline = new[]
            {
        // Зв'язок з рецептами
        new BsonDocument
        {
            { "$lookup", new BsonDocument
                {
                    { "from", "Recipes" },
                    { "localField", "_id" },
                    { "foreignField", "AuthorId" },
                    { "as", "Recipes" }
                }
            }
        },
        // Пропустити і обмежити
        new BsonDocument
        {
            { "$skip", start }
        },
        new BsonDocument
        {
            { "$limit", limit }
        },
        // Зв'язок з містами
        new BsonDocument
        {
            { "$lookup", new BsonDocument
                {
                    { "from", "Cities" },
                    { "localField", "CityId" },
                    { "foreignField", "_id" },
                    { "as", "CityDetails" }
                }
            }
        },
        new BsonDocument
        {
            { "$unwind", new BsonDocument
                {
                    { "path", "$CityDetails" },
                    { "preserveNullAndEmptyArrays", true }
                }
            }
        },
        // Зв'язок з країнами через місто
        new BsonDocument
        {
            { "$lookup", new BsonDocument
                {
                    { "from", "Countries" },
                    { "localField", "CityDetails.CountryId" },
                    { "foreignField", "_id" },
                    { "as", "CountryDetails" }
                }
            }
        },
        new BsonDocument
        {
            { "$unwind", new BsonDocument
                {
                    { "path", "$CountryDetails" },
                    { "preserveNullAndEmptyArrays", true }
                }
            }
        },
        // Вибір необхідних полів
        new BsonDocument
        {
            { "$project", new BsonDocument
                {
                    { "Id", "$_id" },
                    { "FirstName", 1 },
                    { "LastName", 1 },
                    { "ProfilePhotoUrl", 1 },
                    { "City", "$CityDetails.CityName" },
                    { "Country", "$CountryDetails.CountryName" },
                    { "RegistrationDate", 1 },
                    { "Rating", 1 }
                }
            }
        }
    };

            var results = await _users.Aggregate<BsonDocument>(pipeline).ToListAsync();

            // Формування DTO
            return results.Select(result =>
            {
                var author = new AuthorDto
                {
                    Id = result["Id"].ToString(),
                    FirstName = result.Contains("FirstName") ? result["FirstName"].ToString() : null,
                    LastName = result.Contains("LastName") ? result["LastName"].ToString() : null,
                    ProfilePhotoUrl = result.Contains("ProfilePhotoUrl") ? result["ProfilePhotoUrl"].ToString() : null,
                    City = result.Contains("City") ? result["City"].ToString() : "Невідоме місто",
                    Country = result.Contains("Country") ? result["Country"].ToString() : "Невідома країна",
                    RegistrationDate = result.Contains("RegistrationDate") ? result["RegistrationDate"].ToUniversalTime() : DateTime.MinValue,
                    Rating = result.Contains("Rating") ? result["Rating"].ToDouble() : 0
                };

                return author;
            }).ToList();
        }

        public async Task<AuthorDto?> GetByIdAsync(string authorId)
        {
            if (!ObjectId.TryParse(authorId, out _))
            {
                throw new ArgumentException("Invalid author ID format");
            }

            var pipeline = new[]
            {
        // Фільтр за Id
        new BsonDocument
        {
            { "$match", new BsonDocument
                {
                    { "_id", new ObjectId(authorId) }
                }
            }
        },
        // Зв'язок з містами
        new BsonDocument
        {
            { "$lookup", new BsonDocument
                {
                    { "from", "Cities" },
                    { "localField", "CityId" },
                    { "foreignField", "_id" },
                    { "as", "CityDetails" }
                }
            }
        },
        new BsonDocument
        {
            { "$unwind", new BsonDocument
                {
                    { "path", "$CityDetails" },
                    { "preserveNullAndEmptyArrays", true }
                }
            }
        },
        // Зв'язок з країнами через місто
        new BsonDocument
        {
            { "$lookup", new BsonDocument
                {
                    { "from", "Countries" },
                    { "localField", "CityDetails.CountryId" },
                    { "foreignField", "_id" },
                    { "as", "CountryDetails" }
                }
            }
        },
        new BsonDocument
        {
            { "$unwind", new BsonDocument
                {
                    { "path", "$CountryDetails" },
                    { "preserveNullAndEmptyArrays", true }
                }
            }
        },
        // Вибір необхідних полів
        new BsonDocument
        {
            { "$project", new BsonDocument
                {
                    { "_id", 1 }, 
                    { "FirstName", 1 },
                    { "LastName", 1 },
                    { "ProfilePhotoUrl", 1 },
                    { "City", new BsonDocument
                        {
                            { "$ifNull", new BsonArray { "$CityDetails.CityName", "Невідоме місто" } }
                        }
                    },
                    { "Country", new BsonDocument
                        {
                            { "$ifNull", new BsonArray { "$CountryDetails.CountryName", "Невідома країна" } }
                        }
                    },
                    { "RegistrationDate", 1 },
                    { "Rating", 1 }
                }
            }
        }
    };

            var result = await _users.Aggregate<AuthorDto>(pipeline).FirstOrDefaultAsync();
            return result;
        }

        public async Task<UserDto?> GetAboutMeAsync(string userId)
        {
            // Перевіряємо, чи валідний ObjectId
            if (!ObjectId.TryParse(userId, out _))
                throw new ArgumentException("Невірний формат UserId.");

            // Шукаємо користувача за Id
            var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null)
                throw new KeyNotFoundException("Користувача не знайдено.");

            // Отримуємо місто користувача
            var city = await _cities.Find(c => c.Id == user.CityId).FirstOrDefaultAsync();
            var cityName = city?.CityName ?? "Невідоме місто";

            // Отримуємо країну користувача
            var country = city != null ? await _countries.Find(c => c.Id == city.CountryId).FirstOrDefaultAsync() : null;
            var countryName = country?.CountryName ?? "Невідома країна";

            // Повертаємо DTO з інформацією про користувача
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                About = user.About,
                ProfilePhotoUrl = user.ProfilePhotoUrl,
                City = cityName,
                Country = countryName,
                FavoriteRecipes = user.FavoriteRecipes?.Select(r => new RecipeDto
                {
                    Id = r.Id,
                    Name = r.RecipeName,
                    PreviewImageUrl = r.PhotoUrl,
                }).ToList() ?? new List<RecipeDto>(),
                FavoriteAuthors = user.FavoriteAuthors?.Select(a => new AuthorDto
                {
                    Id = a.Id,
                    FirstName = a.FirstName,
                    LastName = a.LastName,
                    ProfilePhotoUrl = a.ProfilePhotoUrl,
                    Rating = a.Rating,
                }).ToList() ?? new List<AuthorDto>()
            };
        }

        public async Task<UserDto?> UpdateAsync(string userId, UserDto updatedUser)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(updatedUser.City))
                throw new ArgumentNullException("Потрібні ідентифікатор користувача та інформація про місто.");

            // Перевірка валідності ObjectId для CityId
            if (!ObjectId.TryParse(updatedUser.City, out _))
                throw new ArgumentException($"CityId '{updatedUser.City}' не є валідним ObjectId.");

            // Отримуємо місто за CityId
            var city = await _cities.Find(c => c.Id == updatedUser.City).FirstOrDefaultAsync();
            if (city == null)
                throw new Exception("Місто з вказаним ID не знайдено.");

            // Отримуємо країну за CountryId
            var country = await _countries.Find(c => c.Id == city.CountryId).FirstOrDefaultAsync();
            if (country == null)
                throw new Exception("Країна, пов'язана з цим містом, не знайдена.");

            // Хешування пароля перед оновленням
            string hashedPassword = HashPassword(updatedUser.Password);

            // Оновлюємо дані користувача
            var updateDefinition = Builders<User>.Update
                .Set(u => u.Email, updatedUser.Email)
                .Set(u => u.PasswordHash, hashedPassword) // Збереження хешу пароля
                .Set(u => u.FirstName, updatedUser.FirstName)
                .Set(u => u.LastName, updatedUser.LastName)
                .Set(u => u.About, updatedUser.About)
                .Set(u => u.ProfilePhotoUrl, updatedUser.ProfilePhotoUrl)
                .Set(u => u.CityId, city.Id); // Оновлюємо лише CityId

            var result = await _users.UpdateOneAsync(
                u => u.Id == userId,
                updateDefinition
            );

            if (result.MatchedCount == 0)
                return null;

            // Повертаємо оновлені дані користувача
            return new UserDto
            {
                Id = userId,
                Email = updatedUser.Email,
                Password = updatedUser.Password, 
                FirstName = updatedUser.FirstName,
                LastName = updatedUser.LastName,
                About = updatedUser.About,
                ProfilePhotoUrl = updatedUser.ProfilePhotoUrl,
                City = city.CityName,
                Country = country.CountryName
            };
        }
        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password), "Пароль не може бути пустим або порожнім.");

            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        public async Task RemoveAsync(string id) =>
            await _users.DeleteOneAsync(u => u.Id == id);

        public async Task<List<AuthorDto>> GetPopularAuthorsAsync(int start, int limit)
        {
            var authors = await GetAsync(start, limit);

            // Сортування за рейтингом
            authors = authors.OrderByDescending(a => a.Rating).ToList();

            return authors;
        }
    }
}
