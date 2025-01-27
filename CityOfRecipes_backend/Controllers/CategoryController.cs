using CityOfRecipes_backend.Models;
using CityOfRecipes_backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CityOfRecipes_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly CategoryService _categoryService;

        public CategoryController(CategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // Отримати всі категорії
        [HttpGet]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _categoryService.GetAllAsync();
            return Ok(categories);
        }

        // Отримати категорію за ID
        [HttpGet("{id:length(24)}")]
        public async Task<IActionResult> GetCategoryById(string id)
        {
            try
            {
                var category = await _categoryService.GetByIdAsync(id);
                return Ok(category);
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

        // Створити нову категорію
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] Category newCategory)
        {
            if (newCategory == null)
                return BadRequest(new { Message = "Дані категорії є обов'язковими." });

            try
            {
                await _categoryService.CreateAsync(newCategory);
                return CreatedAtAction(nameof(GetCategoryById), new { id = newCategory.Id }, newCategory);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // Оновити категорію
        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> UpdateCategory(string id, [FromBody] Category updatedCategory)
        {
            if (updatedCategory == null)
                return BadRequest(new { Message = "Дані категорії є обов'язковими." });

            try
            {
                await _categoryService.UpdateAsync(id, updatedCategory);
                return NoContent();
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

        // Видалити категорію
        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> DeleteCategory(string id)
        {
            try
            {
                await _categoryService.DeleteAsync(id);
                return NoContent();
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
