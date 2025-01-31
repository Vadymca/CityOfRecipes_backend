namespace CityOfRecipes_backend.DTOs
{
    public class RateRecipeRequestDto
    {
        public string RecipeId { get; set; } = string.Empty;
        public int Rating { get; set; }
    }
}
