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
            try
            {
                var cities = await _cityService.GetAllAsync();
                return Ok(cities);
            }
            catch (Exception ex)
            {
                return BadRequest($"Сталася помилка при отриманні міст: {ex.Message}");
            }
        }

        // GET: api/cities/{id}
        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<City>> GetById(string id)
        {
            try
            {
                var city = await _cityService.GetByIdAsync(id);

                if (city == null)
                {
                    return NotFound($"Місто з ID '{id}' не знайдено.");
                }

                return Ok(city);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // GET: api/cities/byCountry/{countryId}
        [HttpGet("byCountry/{countryId:length(24)}")]
        public async Task<ActionResult<List<City>>> GetByCountryId(string countryId)
        {
            try
            {
                var cities = await _cityService.GetByCountryIdAsync(countryId);
                return Ok(cities);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // POST: api/cities
        [HttpPost]
        public async Task<ActionResult<City>> Create(City city)
        {
            try
            {
                await _cityService.CreateAsync(city);
                return CreatedAtAction(nameof(GetById), new { id = city.Id }, city);
            }
            catch (ArgumentException ex)
            {
                return BadRequest($"Невірні дані: {ex.Message}");
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // PUT: api/cities/{id}
        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(string id, City updatedCity)
        {
            try
            {
                var city = await _cityService.GetByIdAsync(id);
                if (city == null)
                {
                    return NotFound($"Місто з ID '{id}' не знайдено.");
                }

                updatedCity.Id = id; // Зберігаємо ID для оновлення
                await _cityService.UpdateAsync(id, updatedCity);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest($"Невірні дані для оновлення: {ex.Message}");
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // DELETE: api/cities/{id}
        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var city = await _cityService.GetByIdAsync(id);
                if (city == null)
                {
                    return NotFound($"Місто з ID '{id}' не знайдено.");
                }

                await _cityService.DeleteAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
