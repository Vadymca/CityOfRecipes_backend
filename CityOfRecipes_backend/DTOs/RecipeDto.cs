using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace CityOfRecipes_backend.DTOs
{
    public class RecipeDto
    {
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_id")]
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty; 
        public string PreviewImageUrl { get; set; } = string.Empty; 
    }
}
