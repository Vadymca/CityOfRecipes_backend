using CityOfRecipes_backend.Models;
using CityOfRecipes_backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace CityOfRecipes_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecipeController : ControllerBase
    {
        private readonly RecipeService _recipeService;

        public RecipeController(RecipeService recipeService)
        {
            _recipeService = recipeService;
        }

        [HttpGet("searchByTag")]
        public async Task<IActionResult> SearchByTag([FromQuery] List<string> tags)
        {
            if (tags == null || !tags.Any())
            {
                return BadRequest("Потрібно вказати хоча б один тег.");
            }

            var recipes = await _recipeService.SearchRecipesByTagAsync(tags);
            return Ok(recipes);
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetAllRecipes([FromQuery] int skip = 0, [FromQuery] int limit = 10)
        {
            if (skip < 0 || limit <= 0)
            {
                return BadRequest("Параметри 'skip' і 'limit' мають бути додатними.");
            }

            var (recipes, totalRecipes) = await _recipeService.GetPaginatedRecipesAsync(skip, limit);

            if (recipes == null || recipes.Count == 0)
            {
                return NotFound("Рецептів не знайдено.");
            }

            return Ok(new
            {
                Total = totalRecipes,
                Recipes = recipes
            });
        }


        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<Recipe>> GetRecipeById(string id)
        {
            var recipe = await _recipeService.GetByIdAsync(id);

            if (recipe == null)
            {
                return NotFound(new { Message = $"Рецепт з ID {id} не знайдено." });
            }

            return Ok(recipe);
        }

        [HttpGet("{slug}")]
        public async Task<ActionResult<Recipe>> GetRecipeBySlug(string slug)
        {
            var recipe = await _recipeService.GetBySlugAsync(slug);

            if (recipe == null)
            {
                return NotFound($"Рецепт зі слагом '{slug}' не знайдено.");
            }

            return Ok(recipe);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRecipe([FromBody] Recipe newRecipe)
        {
            if (newRecipe == null)
            {
                return BadRequest(new { Message = "Потрібні дані про рецепт." });
            }

            try
            {
                await _recipeService.CreateAsync(newRecipe);
                return CreatedAtAction(nameof(GetRecipeById), new { id = newRecipe.Id }, newRecipe);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> UpdateRecipe(string id, [FromBody] Recipe updatedRecipe)
        {
            if (updatedRecipe == null)
            {
                return BadRequest(new { Message = "Потрібні оновлені дані рецепту." });
            }

            var existingRecipe = await _recipeService.GetByIdAsync(id);

            if (existingRecipe == null)
            {
                return NotFound(new { Message = $"Рецепт з ID {id} не знайдено." });
            }

            try
            {
                await _recipeService.UpdateAsync(id, updatedRecipe);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }

        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> DeleteRecipe(string id)
        {
            var recipe = await _recipeService.GetByIdAsync(id);

            if (recipe == null)
            {
                return NotFound(new { Message = $"Рецепт з ID {id} не знайдено." });
            }

            await _recipeService.RemoveAsync(id);

            return NoContent();
        }
    }
}
