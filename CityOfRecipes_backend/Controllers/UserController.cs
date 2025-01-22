using CityOfRecipes_backend.DTOs;
using CityOfRecipes_backend.Models;
using CityOfRecipes_backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

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


        [HttpGet("{id:length(24)}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var author = await _userService.GetByIdAsync(id);
                if (author == null)
                {
                    return NotFound($"Автора с ID {id} не знайдено");
                }

                return Ok(author);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Внутрішня помилка сервера: {ex.Message}");
            }
        }

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

        [HttpGet("aboutme")]
        [Authorize]
        public async Task<IActionResult> GetAboutMe()
        {
            try
            {
                // Отримуємо Id користувача з токена
                var userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Неможливо отримати ідентифікатор користувача.");

                // Викликаємо сервіс для отримання інформації
                var user = await _userService.GetAboutMeAsync(userId);
                if (user == null)
                    return NotFound("Користувача не знайдено.");

                return Ok(user);
            }
            catch (ArgumentException ex)
            {
                return BadRequest($"Помилка формату даних: {ex.Message}");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound($"Користувача не знайдено: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("{userId:length(24)}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(string userId, [FromBody] UserDto updatedUser)
        {
            if (string.IsNullOrEmpty(userId) || updatedUser == null)
            {
                return BadRequest("Необхідні ідентифікатор користувача та оновлені дані користувача.");
            }

            try
            {
                var updatedUserDto = await _userService.UpdateAsync(userId, updatedUser);

                if (updatedUserDto == null)
                {
                    return NotFound($"Користувач з ID {userId} не знайдено.");
                }

                return Ok(updatedUserDto);
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest($"Невірні дані: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                return BadRequest($"Помилка: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Внутрішня помилка сервера: {ex.Message}");
            }
        }

        [HttpDelete("{id:length(24)}")]
        [Authorize]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                // Знаходимо користувача за ID
                var user = await _userService.GetByIdAsync(id);

                if (user is null)
                    return NotFound("Користувача не знайдено.");

                // Оновлюємо дані користувача (заміна email, ім'я та аватарки)
                await _userService.RemoveAsync(id);

                // Повертаємо успішну відповідь з повідомленням
                return Ok(new { Message = "Аккаунт успішно видалений" });
            }
            catch (Exception ex)
            {
                // Логування або інша обробка помилки
                return StatusCode(StatusCodes.Status500InternalServerError, $"Сталася помилка: {ex.Message}");
            }
        }

        // Підтвердження електронної пошти

        [HttpPost("initiate-email-confirmation/{userId}")]
        public async Task<IActionResult> InitiateEmailConfirmation(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("Ідентифікатор користувача є обов'язковим.");
            }

            try
            {
                var token = await _userService.InitiateEmailConfirmationAsync(userId);
                return Ok(new { Message = "Лист для підтвердження електронної пошти надіслано." });
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest($"Помилка: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Внутрішня помилка сервера: {ex.Message}");
            }
        }

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Токен є обов'язковим.");
            }

            try
            {
                var success = await _userService.ConfirmEmailAsync(token);
                if (success)
                    return Ok("Електронна пошта успішно підтверджена.");
                else
                    return BadRequest("Підтвердження електронної пошти не вдалося.");
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest($"Помилка: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Внутрішня помилка сервера: {ex.Message}");
            }
        }

        //Скидання пароля через email

        [HttpPost("initiate-password-reset")]
        public async Task<IActionResult> InitiatePasswordReset([FromBody] string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email є обов'язковим.");
            }

            try
            {
                var token = await _userService.InitiatePasswordResetAsync(email);
                return Ok(new { Message = "Лист для скидання пароля надіслано." });
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest($"Помилка: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Внутрішня помилка сервера: {ex.Message}");
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromQuery] string token, [FromBody] string newPassword)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(newPassword))
            {
                return BadRequest("Токен та новий пароль є обов'язковими.");
            }

            try
            {
                await _userService.ResetPasswordAsync(token, newPassword);
                return Ok("Пароль успішно оновлено.");
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest($"Помилка: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"Внутрішня помилка сервера: {ex.Message}");
            }
        }

    }
}
