using CityOfRecipes_backend.DTOs;
using CityOfRecipes_backend.Models;
using CityOfRecipes_backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CityOfRecipes_backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly CountryService _countryService;
        private readonly CityService _cityService;

        public UserController(UserService userService, CountryService countryService, CityService cityService)
        {
            _userService = userService;
            _countryService = countryService;
            _cityService = cityService;
        }

        [HttpGet]
        public async Task<ActionResult<List<AuthorDto>>> GetAllUsers([FromQuery] int start = 0, [FromQuery] int limit = 10)
        {
            if (start < 0)
                return BadRequest("Параметр 'start' не може бути від'ємним.");
            if (limit <= 0)
                return BadRequest("Параметр 'limit' має бути більшим за нуль.");

            var users = await _userService.GetAsync(start, limit);

            var result = users.Select(x => x).ToList();

            return Ok(result);
        }
    

        //[HttpGet("{id:length(24)}")]
        //public async Task<ActionResult<User>> GetUserById(string id)
        //{
        //    var user = await _userService.GetByIdAsync(id);

        //    if (user is null)
        //        return NotFound();

        //    return Ok(user);
        //}

        //[HttpPost]
        //public async Task<IActionResult> CreateUser(User newUser)
        //{

        //    // Ensure City Exists
        //    if (!string.IsNullOrEmpty(newUser.CityId))
        //    {
        //        var city = await _cityService.GetByIdAsync(newUser.CityId);

        //        if (city is null || city.CountryId != newUser.CountryId)
        //            return BadRequest("Місто не знайдено або не належить до вибраної країни.");
        //    }

        //    await _userService.CreateAsync(newUser);

        //    return CreatedAtAction(nameof(GetUserById), new { id = newUser.Id }, newUser);
        //}

        //[HttpPut("{id:length(24)}")]
        //public async Task<IActionResult> UpdateUser(string id, User updatedUser)
        //{
        //    var user = await _userService.GetByIdAsync(id);

        //    if (user is null)
        //        return NotFound();

        //    // Ensure Country Exists
        //    var country = await _countryService.GetByIdAsync(updatedUser.CountryId);

        //    if (country is null)
        //        return BadRequest("Країна не знайдена.");

        //    // Ensure City Exists
        //    if (updatedUser.CityId != null)
        //    {
        //        var city = await _cityService.GetByIdAsync(updatedUser.CityId);

        //        if (city is null || city.CountryId != updatedUser.CountryId)
        //            return BadRequest("Місто не знайдено або не належить до вибраної країни.");
        //    }

        //    updatedUser.Id = user.Id;

        //    await _userService.UpdateAsync(id, updatedUser);

        //    return NoContent();
        //}

        //[HttpDelete("{id:length(24)}")]
        //public async Task<IActionResult> DeleteUser(string id)
        //{
        //    var user = await _userService.GetByIdAsync(id);

        //    if (user is null)
        //        return NotFound();

        //    await _userService.RemoveAsync(id);

        //    return NoContent();
        //}

        [HttpGet("popular-authors")]
        public async Task<ActionResult<List<AuthorDto>>> GetPopularAuthors([FromQuery] int start = 0, [FromQuery] int limit = 4)
        {
            if (start < 0)
                return BadRequest("Параметр 'start' не може бути від'ємним.");
            if (limit <= 0)
                return BadRequest("Параметр 'limit' має бути більшим за нуль.");

            var popularAuthors = await _userService.GetPopularAuthorsAsync(start, limit);

            var result = popularAuthors.Select(x => x).ToList();

            return Ok(result);
        }
    }
}
