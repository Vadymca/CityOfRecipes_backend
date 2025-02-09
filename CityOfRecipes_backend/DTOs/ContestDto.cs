using CityOfRecipes_backend.Models;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace CityOfRecipes_backend.DTOs
{
    public class ContestDto
    {
        public string Id { get; set; } = string.Empty;
        public string ContestName { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? RequiredIngredients { get; set; }
        public string? ContestDetails { get; set; }
        public string? CategoryId { get; set; }
        public List<ContestRecipeDto> ContestRecipes { get; set; } = new();
        public List<ContestRecipeDto> WinningRecipes { get; set; } = new();
        public string Slug { get; set; } = string.Empty;
    }
}
