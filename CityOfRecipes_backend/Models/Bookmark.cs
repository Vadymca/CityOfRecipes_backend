using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace CityOfRecipes_backend.Models
{
    public class Bookmark
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("UserId")]
        public string UserId { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("RecipeId")]
        public string RecipeId { get; set; } = null!;

        [BsonIgnore]
        public User? User { get; set; }

        [BsonIgnore]
        public Recipe? Recipe { get; set; }
    }
}
