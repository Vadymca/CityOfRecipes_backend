using CityOfRecipes_backend.Models;
using CityOfRecipes_backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CityOfRecipes_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CitiesController : ControllerBase
    {
        private readonly CityService _cityService;

        public CitiesController(CityService cityService)
        {
            _cityService = cityService;
        }

        // GET: api/cities
        [HttpGet]
        public async Task<ActionResult<List<City>>> GetAll()
        {
            var cities = await _cityService.GetAllAsync();
            return Ok(cities);
        }

        // GET: api/cities/{id}
        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<City>> GetById(string id)
        {
            var city = await _cityService.GetByIdAsync(id);

            if (city == null)
            {
                return NotFound($"Місто з ID '{id}' не знайдено.");
            }

            return Ok(city);
        }

        // GET: api/cities/byCountry/{countryId}
        [HttpGet("byCountry/{countryId:length(24)}")]
        public async Task<ActionResult<List<City>>> GetByCountryId(string countryId)
        {
            var cities = await _cityService.GetByCountryIdAsync(countryId);
            return Ok(cities);
        }

        // POST: api/cities
        [HttpPost]
        public async Task<ActionResult<City>> Create(City city)
        {
            try
            {
                await _cityService.CreateAsync(city);
                return Ok(city);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT: api/cities/{id}
        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(string id, City updatedCity)
        {
            var city = await _cityService.GetByIdAsync(id);
            if (city == null)
            {
                return NotFound($"Місто з ID '{id}' не знайдено.");
            }

            try
            {
                updatedCity.Id = id; // Зберігаємо ID для оновлення
                await _cityService.UpdateAsync(id, updatedCity);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE: api/cities/{id}
        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _cityService.DeleteAsync(id);
            return NoContent();
        }
    }
}
