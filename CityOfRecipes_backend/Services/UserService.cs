using CityOfRecipes_backend.DTOs;
using CityOfRecipes_backend.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace CityOfRecipes_backend.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;

        public UserService(IOptions<MongoDBSettings> mongoSettings)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _users = database.GetCollection<User>("Users");
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


        //    public async Task<User?> GetByIdAsync(string id) =>
        //        await _users.Find(u => u.Id == id).FirstOrDefaultAsync();

        //public async Task CreateAsync(User user)
        //{
        //    await _users.InsertOneAsync(user);
        //}

        //public async Task UpdateAsync(string id, User updatedUser)
        //{
        //    await _users.ReplaceOneAsync(u => u.Id == id, updatedUser);
        //}

        //public async Task RemoveAsync(string id) =>
        //    await _users.DeleteOneAsync(u => u.Id == id);

        public async Task<List<AuthorDto>> GetPopularAuthorsAsync(int start, int limit)
        {
            var authors = await GetAsync(start, limit);

            // Сортування за рейтингом
            authors = authors.OrderByDescending(a => a.Rating).ToList();

            return authors;
        }
    }
}
