﻿using CityOfRecipes_backend.Models;
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
        private readonly CountryService _countryService;
        private readonly CityService _cityService;

        public UserController(UserService userService, CountryService countryService, CityService cityService)
        {
            _userService = userService;
            _countryService = countryService;
            _cityService = cityService;
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
            // Ensure Country Exists
            var country = await _countryService.EnsureCountryExistsAsync(newUser.Country);
            newUser.CountryId = country.Id;

            // Ensure City Exists
            if (!string.IsNullOrWhiteSpace(newUser.City))
            {
                var city = await _cityService.EnsureCityExistsAsync(newUser.City, country.Id);
                newUser.City = city.CityName;
            }

            newUser.Validate();
            await _userService.CreateAsync(newUser);

            return CreatedAtAction(nameof(GetUserById), new { id = newUser.Id }, newUser);
        }

        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> UpdateUser(string id, User updatedUser)
        {
            var user = await _userService.GetByIdAsync(id);

            if (user is null)
                return NotFound();

            // Ensure Country Exists
            var country = await _countryService.EnsureCountryExistsAsync(updatedUser.Country);
            updatedUser.CountryId = country.Id;

            // Ensure City Exists
            if (!string.IsNullOrWhiteSpace(updatedUser.City))
            {
                var city = await _cityService.EnsureCityExistsAsync(updatedUser.City, country.Id);
                updatedUser.City = city.CityName;
            }

            updatedUser.Id = user.Id;
            updatedUser.Validate();
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
        public async Task<ActionResult<List<object>>> GetPopularAuthors([FromQuery] int start = 0,[FromQuery] int limit = 4)
        {
            if (start < 0)
                return BadRequest("Параметр 'start' не може бути від'ємним.");
            if (limit <= 0)
                return BadRequest("Параметр 'limit' має бути більшим за нуль.");

            var popularAuthors = await _userService.GetPopularAuthorsAsync(start, limit);

            return Ok(popularAuthors.Select(a => new
            {
                a.User.Id,
                a.User.FirstName,
                a.User.LastName,
                a.User.ProfilePhotoUrl,
                a.User.Country,
                a.User.City,
                a.User.RegistrationDate,
                a.User.Rating
            }));
        }
    }
}
