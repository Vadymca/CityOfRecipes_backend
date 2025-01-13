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
            await _authService.RegisterAsync(request.Email, request.Password);
            return Ok("Реєстрація успішна");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            var token = await _authService.AuthenticateAsync(request.Email, request.Password);
            return Ok(new { Token = token });
        }
    }
}
