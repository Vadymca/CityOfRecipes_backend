namespace CityOfRecipes_backend.DTOs
{
    public class CreateRecipeDto
    {
        public string CategoryId { get; set; } = string.Empty;

        public string RecipeName { get; set; } = string.Empty;

        public int PreparationTimeMinutes { get; set; }

        public string IngredientsList { get; set; } = string.Empty; // Від користувача - у вигляді тексту

        public string InstructionsText { get; set; } = string.Empty;

        public string? VideoUrl { get; set; }

        public string? PhotoUrl { get; set; }

        public string TagsText { get; set; } = string.Empty; // Від користувача - у вигляді тексту

        public bool IsChristmas { get; set; } = false;

        public bool IsNewYear { get; set; } = false;

        public bool IsChildren { get; set; } = false;

        public bool IsEaster { get; set; } = false;
    }
}
