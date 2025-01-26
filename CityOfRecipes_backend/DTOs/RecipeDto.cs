using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace CityOfRecipes_backend.DTOs
{

    public class RecipeDto
    {

        [Required]
        [MinLength(10, ErrorMessage = "Список інгредієнтів має містити не менше 10 символів.")]
        public string IngredientsText { get; set; } = string.Empty; // Вхідний текст інгредієнтів

        [Required]
        [MinLength(3, ErrorMessage = "Список тегів має містити хоча б один тег.")]
        public string TagsText { get; set; } = string.Empty; // Вхідний текст тегів

    }
}
