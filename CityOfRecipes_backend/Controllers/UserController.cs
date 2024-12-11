using CityOfRecipes_backend.Models;
using CityOfRecipes_backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CityOfRecipes_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<ActionResult<List<User>>> GetAllUsers() =>
            await _userService.GetAsync();

        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<User>> GetUserById(string id)
        {
            var user = await _userService.GetByIdAsync(id);

            if (user is null)
                return NotFound();

            return user;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(User newUser)
        {
            await _userService.CreateAsync(newUser);
            return CreatedAtAction(nameof(GetUserById), new { id = newUser.Id }, newUser);
        }

        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> UpdateUser(string id, User updatedUser)
        {
            var user = await _userService.GetByIdAsync(id);

            if (user is null)
                return NotFound();

            updatedUser.Id = user.Id;
            await _userService.UpdateAsync(id, updatedUser);

            return NoContent();
        }

        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userService.GetByIdAsync(id);

            if (user is null)
                return NotFound();

            await _userService.RemoveAsync(id);

            return NoContent();
        }

        [HttpGet("popular-authors")]
        public async Task<ActionResult<List<object>>> GetPopularAuthors([FromQuery] int limit = 10)
        {
            if (limit <= 0)
                return BadRequest("Параметр 'limit' має бути більшим за нуль.");

            var popularAuthors = await _userService.GetPopularAuthorsAsync(limit);

            return Ok(popularAuthors.Select(a => new
            {
                a.User.Login,
                a.User.ProfilePhotoUrl,
                a.User.About,
                a.RecipeCount
            }));
        }
    }
}
