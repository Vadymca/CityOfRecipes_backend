using CityOfRecipes_backend.Models;
using CityOfRecipes_backend.Services;
using Microsoft.AspNetCore.Authorization;
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
        public async Task<IActionResult> SearchRecipesByTag([FromQuery] string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return BadRequest(new { Message = "Тег не може бути порожнім." });
            }

            try
            {
                var recipes = await _recipeService.SearchRecipesByTagAsync(tag);

                // Формуємо список результатів
                var results = recipes.Select(recipe => new
                {
                    recipe.Id,
                    recipe.RecipeName,
                    recipe.CategoryId,
                    recipe.AuthorId,
                    recipe.PreparationTimeMinutes,
                    recipe.Tags,
                    recipe.PhotoUrl,
                    recipe.AverageRating,
                    recipe.TotalRatings,
                    recipe.Slug
                }).ToList();

                return Ok(results);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("searchByString")]
        public async Task<IActionResult> SearchRecipesByString([FromQuery] string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return BadRequest(new { Message = "Рядок пошуку не може бути порожнім." });
            }

            try
            {
                var recipes = await _recipeService.SearchRecipesByStringAsync(searchQuery);
                if (recipes == null || !recipes.Any())
                {
                    return NotFound(new { Message = "Рецепти не знайдено за заданим запитом." });
                }

                return Ok(recipes);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Помилка під час пошуку: {ex.Message}" });
            }
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
        public async Task<IActionResult> GetRecipeById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(new { Message = "Необхідно вказати ID рецепта." });
            }

            try
            {
                var recipe = await _recipeService.GetByIdAsync(id);

                // Формуємо відповідь
                return Ok(new
                {
                    recipe.Id,
                    recipe.RecipeName,
                    recipe.CategoryId,
                    recipe.AuthorId,
                    recipe.PreparationTimeMinutes,
                    recipe.CreatedAt,
                    recipe.Ingredients,
                    recipe.InstructionsText,
                    recipe.Tags,
                    recipe.PhotoUrl,
                    recipe.AverageRating,
                    recipe.TotalRatings,
                    recipe.Holidays,
                    recipe.Slug,
                    recipe.IsParticipatedInContest // Додано для перевірки участі в конкурсі
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
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
        [Authorize]
        public async Task<IActionResult> CreateRecipe([FromBody] Recipe newRecipe)
        {
            if (newRecipe == null)
            {
                return BadRequest(new { Message = "Потрібні дані про рецепт." });
            }

            try
            {
                await _recipeService.CreateAsync(newRecipe);
                return CreatedAtAction(
                    nameof(GetRecipeById), 
                    new { id = newRecipe.Id }, 
                    new
                    {
                        newRecipe.Id,
                        newRecipe.RecipeName,
                        newRecipe.CategoryId,
                        newRecipe.AuthorId,
                        newRecipe.PreparationTimeMinutes,
                        newRecipe.CreatedAt,
                        newRecipe.Ingredients,
                        newRecipe.IngredientsList,
                        newRecipe.InstructionsText,
                        newRecipe.Tags,
                        newRecipe.PhotoUrl,
                        newRecipe.AverageRating,
                        newRecipe.TotalRatings,
                        newRecipe.Holidays
                    }
                    
                    );
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{id:length(24)}")]
        [Authorize]
        public async Task<IActionResult> UpdateRecipe(string id, [FromBody] Recipe updatedData)
        {
            if (string.IsNullOrWhiteSpace(id) || updatedData == null)
            {
                return BadRequest(new { Message = "Неправильні дані для оновлення." });
            }

            try
            {
                await _recipeService.UpdateAsync(id, updatedData);
                return Ok(new { Message = "Рецепт успішно оновлено." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("{id:length(24)}")]
        [Authorize]
        public async Task<IActionResult> DeleteRecipe(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(new { Message = "Необхідно вказати ID рецепта." });
            }

            try
            {
                await _recipeService.DeleteAsync(id);
                return Ok(new { Message = "Рецепт успішно видалено." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
