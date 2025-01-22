using CityOfRecipes_backend.DTOs;
using CityOfRecipes_backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace CityOfRecipes_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { Error = "Електронна пошта та пароль є обов'язковими." });

            try
            {
                await _authService.RegisterAsync(request.Email, request.Password);
                return Ok(new { Message = "Реєстрація успішна" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Error = ex.Message }); // Некоректні дані
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Сталася внутрішня помилка сервера.", Details = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { Error = "Електронна пошта та пароль є обов'язковими." });

            try
            {
                var token = await _authService.AuthenticateAsync(request.Email, request.Password);
                return Ok(new { Token = token });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { Error = "Недійсні облікові дані." }); // Некоректний email або пароль
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Error = ex.Message }); // Некоректний email
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Сталася внутрішня помилка сервера.", Details = ex.Message });
            }
        }
    }
}
