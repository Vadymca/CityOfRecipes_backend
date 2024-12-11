using CityOfRecipes_backend.Models;
using CityOfRecipes_backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace CityOfRecipes_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TagsController : ControllerBase
    {
        private readonly TagService _tagService;

        public TagsController(TagService tagService)
        {
            _tagService = tagService;
        }

        [HttpGet]
        public async Task<ActionResult<List<string>>> GetAllTagNames()
        {
            var tags = await _tagService.GetAllTagsAsync();

            if (tags == null || tags.Count == 0)
            {
                return NotFound("Теги не знайдено.");
            }

            var tagNames = tags.Select(tag => tag.TagName).ToList();
            return Ok(tagNames);
        }

        [HttpGet("popular")]
        public async Task<ActionResult<List<Tag>>> GetPopularTags([FromQuery] int limit = 50)
        {
            if (limit <= 0)
            {
                return BadRequest($"Параметр 'limit' має бути більшим за нуль.");
            }

            var popularTags = await _tagService.GetTopTagsAsync(limit);
            return Ok(popularTags);
        }

        [HttpGet("popularTagName")]
        public async Task<ActionResult<List<string>>> GetPopularTagNames([FromQuery] int limit = 50)
        {
            if (limit <= 0)
            {
                return BadRequest("Параметр 'limit' має бути більшим за нуль.");
            }

            var popularTagNames = await _tagService.GetTopTagNamesAsync(limit);
            return Ok(popularTagNames);
        }

    }
}
