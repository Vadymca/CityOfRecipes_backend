using CityOfRecipes_backend.Models;
using CityOfRecipes_backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CityOfRecipes_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IngredientController : ControllerBase
    {
        private readonly IngredientService _ingredientService;

        public IngredientController(IngredientService ingredientService)
        {
            _ingredientService = ingredientService;
        }

        [HttpGet]
        public async Task<ActionResult<List<string>>> GetAllIngredientNames()
        {
            var ingredients = await _ingredientService.GetAllIngredientsAsync();
            var ingredientNames = ingredients.Select(i => i.IngredientName).ToList(); // Витягуємо лише назви інгредієнтів
            return Ok(ingredientNames);
        }
    }
}
