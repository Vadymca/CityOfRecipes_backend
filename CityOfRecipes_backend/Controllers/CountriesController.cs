using CityOfRecipes_backend.Models;
using CityOfRecipes_backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CityOfRecipes_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountriesController : ControllerBase
    {
        private readonly CountryService _countryService;
        private readonly CityService _cityService;

        public CountriesController(CountryService countryService, CityService cityService)
        {
            _countryService = countryService;
            _cityService = cityService;
        }

        // GET: api/countries
        [HttpGet]
        public async Task<ActionResult<List<Country>>> GetAll()
        {
            try
            {
                var countries = await _countryService.GetAllAsync();
                return Ok(countries);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        // GET: api/countries/{id}
        [HttpGet("{id:length(24)}", Name = "GetCountry")]
        public async Task<ActionResult<Country>> GetById(string id)
        {
            try
            {
                var country = await _countryService.GetByIdAsync(id);

                if (country == null)
                {
                    return NotFound(new { message = $"Країну з ID '{id}' не знайдено." });
                }

                return Ok(country);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        // GET: api/countries/{id}/cities
        [HttpGet("{id:length(24)}/cities")]
        public async Task<ActionResult<List<City>>> GetCitiesByCountryId(string id)
        {
            try
            {
                var country = await _countryService.GetByIdAsync(id);
                if (country == null)
                {
                    return NotFound(new { message = $"Країну з ID '{id}' не знайдено." });
                }

                var cities = await _cityService.GetByCountryIdAsync(id);
                return Ok(cities);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new {error = ex.Message });
            }
        }

        // POST: api/countries
        [HttpPost]
        public async Task<ActionResult<Country>> Create(Country country)
        {
            try
            {
                await _countryService.CreateAsync(country);
                return CreatedAtRoute("GetCountry", new { id = country.Id }, country);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        // PUT: api/countries/{id}
        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(string id, Country updatedCountry)
        {
            try
            {
                var existingCountry = await _countryService.GetByIdAsync(id);

                if (existingCountry == null)
                {
                    return NotFound(new { message = $"Країну з ID '{id}' не знайдено." });
                }

                updatedCountry.Id = id; // переконуємось, що ID не змінюється
                await _countryService.UpdateAsync(id, updatedCountry);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        // DELETE: api/countries/{id}
        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var country = await _countryService.GetByIdAsync(id);

                if (country == null)
                {
                    return NotFound(new { message = $"Країну з ID '{id}' не знайдено." });
                }

                await _countryService.DeleteAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }
    }
}
