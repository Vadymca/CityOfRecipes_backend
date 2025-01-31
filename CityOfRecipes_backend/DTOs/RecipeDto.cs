using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace CityOfRecipes_backend.DTOs
{

    public class RecipeDto
    {
        public string Id { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string RecipeName { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public string AuthorId { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;

    }
}
