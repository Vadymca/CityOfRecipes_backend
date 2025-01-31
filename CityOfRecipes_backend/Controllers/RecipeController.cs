﻿using CityOfRecipes_backend.DTOs;
using CityOfRecipes_backend.Models;
using CityOfRecipes_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CityOfRecipes_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecipeController : ControllerBase
    {
        private readonly RecipeService _recipeService;
        private readonly RatingService _ratingService;

        public RecipeController(RecipeService recipeService, RatingService ratingService)
        {
            _recipeService = recipeService;
            _ratingService = ratingService;
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
        public async Task<IActionResult> SearchRecipesByString(
                            [FromQuery] string query,
                            [FromQuery] int page = 1,
                            [FromQuery] int pageSize = 10
            )
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { Message = "Рядок пошуку не може бути порожнім." });
            }

            try
            {
                var (recipes, totalCount) = await _recipeService.SearchRecipesByStringAsync(query, page, pageSize);

                var response = new
                {
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    Recipes = recipes
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Помилка під час пошуку: {ex.Message}" });
            }
        }
    
        [HttpGet]
        public async Task<ActionResult<object>> GetAllRecipes([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var (recipes, totalCount) = await _recipeService.GetPaginatedRecipesAsync(page, pageSize);

                var response = new
                {
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    Recipes = recipes
                };

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                // Обробка бізнес-логічних або специфічних помилок
                return StatusCode(500, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                // Обробка несподіваних помилок
                return StatusCode(500, new { Message = "Сталася несподівана помилка. Спробуйте пізніше." });
            }
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

        [HttpGet("by-author/{authorId}")]
        public async Task<IActionResult> GetRecipesByAuthorId(string authorId, int page = 1, int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(authorId))
            {
                return BadRequest(new { Message = "Ідентифікатор автора не може бути порожнім." });
            }

            if (page < 1 || pageSize < 1)
            {
                return BadRequest(new { Message = "Невірні параметри пагінації. Сторінка та розмір сторінки повинні бути більше 0." });
            }

            try
            {
                var (recipes, total) = await _recipeService.GetRecipesByAuthorIdAsync(authorId, page, pageSize);

                return Ok(new
                {
                    TotalCount = total,
                    Page = page,
                    PageSize = pageSize,
                    Recipes = recipes
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

        [HttpGet("by-category/{categoryId}")]
        public async Task<IActionResult> GetRecipesByCategoryId(string categoryId, int page = 1, int pageSize = 10)
        {
            try
            {
                var (recipes, total) = await _recipeService.GetRecipesByCategoryIdAsync(categoryId, page, pageSize);

                return Ok(new
                {
                    TotalCount = total,
                    Page = page,
                    PageSize = pageSize,
                    Recipes = recipes
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

        [HttpGet("favorite-recipes")]
        [Authorize]
        public async Task<IActionResult> GetFavoriteRecipes([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                // Отримуємо `userId` з токена
                var userId = User.FindFirst("id")?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "Не вдалося ідентифікувати користувача." });
                }

                // Виклик сервісу для отримання улюблених рецептів
                var (recipes, totalCount) = await _recipeService.GetFavoriteRecipesByUserIdAsync(userId, page, pageSize);

                // Формування відповіді з пагінацією
                var response = new
                {
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    Recipes = recipes
                };

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest($"Помилка: {ex.Message}");
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

        [HttpGet("by-slug/{slug}")]
        public async Task<ActionResult<Recipe>> GetRecipeBySlug(string slug)
        {
            try
            {
                // Виклик сервісного методу для отримання рецепта
                var recipe = await _recipeService.GetBySlugAsync(slug);

                // Перевірка, чи знайдено рецепт
                if (recipe == null)
                {
                    return NotFound($"Рецепт зі слагом '{slug}' не знайдено.");
                }

                return Ok(recipe);
            }
            catch (ArgumentException ex)
            {
                // Помилка через некоректний параметр
                return BadRequest(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                // Обробка ситуації, коли рецепт не знайдено
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Помилка виконання бізнес-логіки або доступу до бази даних
                return StatusCode(500, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                // Обробка несподіваних помилок
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        [HttpGet("holiday")]
        public async Task<IActionResult> GetRecipesByHoliday(
                                        [FromQuery] string holiday, 
                                        [FromQuery] int page = 1, 
                                        [FromQuery] int pageSize = 10)
        {
            try
            {
                // Виклик сервісу для отримання рецептів за святом
                var (recipes, totalCount) = await _recipeService.GetRecipesByHolidayAsync(holiday, page, pageSize);

                // Формування відповіді з пагінацією
                var response = new
                {
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    Recipes = recipes
                };

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Внутрішня помилка сервера.", Error = ex.Message });
            }
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
                        newRecipe.TotalRatings
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

        [Authorize]
        [HttpPost("toggle-favorite-recipe")]
        public async Task<IActionResult> ToggleFavoriteRecipe([FromQuery] string recipeId)
        {
            try
            {
                // Отримуємо ID авторизованого користувача з токена
                var userId = User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Не вдалося визначити користувача." });
                }

                // Викликаємо сервіс для додавання/видалення улюбленого рецепта
                var isAdded = await _recipeService.ToggleFavoriteRecipeAsync(userId, recipeId);

                return Ok(new
                {
                    message = isAdded ? "Рецепт додано до улюблених." : "Рецепт видалено з улюблених."
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = $"Помилка: {ex.Message}" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = $"Помилка: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Внутрішня помилка сервера. Спробуйте пізніше." });
            }
        }

        [Authorize] 
        [HttpPost("rate")]
        public async Task<IActionResult> RateRecipe([FromBody] RateRecipeRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RecipeId))
            {
                return BadRequest("Некоректні дані.");
            }

            // Отримуємо `userId` з токена
            var userId = User.FindFirst("id")?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized("Користувач не авторизований.");
            }

            bool success = await _ratingService.AddOrUpdateRatingAsync(request.RecipeId, userId, request.Rating);

            if (success)
            {
                return Ok(new { message = "Оцінку додано або оновлено" });
            }

            return BadRequest("Не вдалося оновити оцінку.");
        }
    }
}
