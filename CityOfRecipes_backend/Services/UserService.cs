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

        public async Task<List<User>> GetAsync() =>
            await _users.Find(_ => true).ToListAsync();

        public async Task<User?> GetByIdAsync(string id) =>
            await _users.Find(u => u.Id == id).FirstOrDefaultAsync();

        public async Task CreateAsync(User user)
        {
            user.Validate(); // Виклик методу валідації
            await _users.InsertOneAsync(user);
        }

        public async Task UpdateAsync(string id, User updatedUser)
        {
            updatedUser.Validate(); // Виклик методу валідації
            await _users.ReplaceOneAsync(u => u.Id == id, updatedUser);
        }

        public async Task RemoveAsync(string id) =>
            await _users.DeleteOneAsync(u => u.Id == id);

        public async Task<List<(User User, int RecipeCount)>> GetPopularAuthorsAsync(int limit)
        {
            var pipeline = new[]
            {
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
        new BsonDocument
        {
            { "$addFields", new BsonDocument
                {
                    { "RecipeCount", new BsonDocument
                        {
                            { "$size", "$Recipes" }
                        }
                    }
                }
            }
        },
        new BsonDocument
        {
            { "$sort", new BsonDocument
                {
                    { "RecipeCount", -1 }
                }
            }
        },
        new BsonDocument
        {
            { "$limit", limit }
        },
        new BsonDocument
        {
            { "$project", new BsonDocument
                {
                    { "Id", "$_id" },
                    { "Login", 1 },
                    { "Rating", 1 },
                    { "About", 1 },
                    { "ProfilePhotoUrl", 1 },
                    { "RecipeCount", 1 }
                }
            }
        }
    };

            var results = await _users.Aggregate<BsonDocument>(pipeline).ToListAsync();

            return results.Select(result =>
            {
                var user = new User
                {
                    Id = result["Id"].IsObjectId ? result["Id"].AsObjectId.ToString() : result["Id"].AsString,
                    Login = result["Login"].AsString,
                    Rating = result.Contains("Rating") ? result["Rating"].AsDouble : 0,
                    About = result.Contains("About") ? result["About"].AsString : null,
                    ProfilePhotoUrl = result.Contains("ProfilePhotoUrl") ? result["ProfilePhotoUrl"].AsString : null
                };

                var recipeCount = result["RecipeCount"].AsInt32;

                return (user, recipeCount);
            }).ToList();

        }
    }
}
