namespace CityOfRecipes_backend.DTOs
{
    public class ContestRecipeDto
    {
        public string Id { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string RecipeName { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
        public string AuthorId { get; set; } = string.Empty;
        public string CategoryId { get; set; } = string.Empty;
        public double AverageRating { get; set; } = 0.0;
        public int ContestRating { get; set; } = 0;
    }
}
