using CityOfRecipes_backend.Models;
using CityOfRecipes_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CityOfRecipes_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContestController : ControllerBase
    {
        private readonly ContestService _contestService;

        public ContestController(ContestService contestService)
        {
            _contestService = contestService;
        }

        // Отримати список поточних конкурсів
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveContests()
        {
            try
            {
                var contests = await _contestService.GetActiveContestsAsync();
                return Ok(contests);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Помилка отримання активних конкурсів: {ex.Message}" });
            }
        }

        // Отримати список завершених конкурсів
        [HttpGet("finished")]
        public async Task<IActionResult> GetFinishedContests()
        {
            try
            {
                var contests = await _contestService.GetFinishedContestsAsync();
                return Ok(contests);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Помилка отримання завершених конкурсів: {ex.Message}" });
            }
        }

        // Отримати конкретний конкурс за ID
        [HttpGet("{contestId}")]
        public async Task<IActionResult> GetContestById(string contestId)
        {
            try
            {
                var contest = await _contestService.GetContestByIdAsync(contestId);
                return Ok(contest);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Помилка отримання конкурсу: {ex.Message}" });
            }
        }

        // Отримати список конкурсів, у яких бере участь рецепт
        [HttpGet("by-recipe/{recipeId}")]
        public async Task<IActionResult> GetContestsByRecipeId(string recipeId)
        {
            try
            {
                var contests = await _contestService.GetContestsByRecipeIdAsync(recipeId);
                return Ok(contests);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Помилка отримання конкурсів для рецепта: {ex.Message}" });
            }
        }

        // Отримати список конкурсів, у яких рецепт може взяти участь
        [HttpGet("available-for-recipe/{recipeId}")]
        public async Task<IActionResult> GetAvailableContestsForRecipe(string recipeId)
        {
            try
            {
                var contests = await _contestService.GetAvailableContestsForRecipeAsync(recipeId);
                return Ok(contests);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Помилка отримання доступних конкурсів для рецепта: {ex.Message}" });
            }
        }

        // Отримати список рецептів у конкурсі
        [HttpGet("{contestId}/recipes")]
        public async Task<IActionResult> GetRecipesByContestId(string contestId)
        {
            try
            {
                var recipes = await _contestService.GetRecipesByContestIdAsync(contestId);
                return Ok(recipes);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Помилка отримання рецептів для конкурсу: {ex.Message}" });
            }
        }

        // Додати рецепт до конкурсу (авторизований користувач)
        [HttpPost("{contestId}/add-recipe/{recipeId}")]
        [Authorize]
        public async Task<IActionResult> AddRecipeToContest(string contestId, string recipeId)
        {
            try
            {
                var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Message = "Не вдалося отримати ID користувача." });
                }

                await _contestService.AddRecipeToContestAsync(contestId, recipeId, userId);
                return Ok(new { Message = "Рецепт успішно додано до конкурсу." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Внутрішня помилка сервера: {ex.Message}" });
            }
        }


        // Створити конкурс
        [HttpPost("create")]
        [Authorize] 
        public async Task<IActionResult> CreateContest([FromBody] Contest newContest)
        {
            try
            {
                if (newContest == null)
                    return BadRequest(new { Message = "Дані конкурсу не можуть бути порожніми." });

                var createdContest = await _contestService.CreateContestAsync(newContest);
                return CreatedAtAction(nameof(GetContestById), new { contestId = createdContest.Id }, createdContest);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = $"Внутрішня помилка сервера: {ex.Message}" });
            }
        }

    }
}
