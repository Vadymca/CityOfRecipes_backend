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
            try
            {
            if (start < 0)
                return BadRequest("Параметр 'start' не може бути від'ємним.");
            if (limit <= 0)
                return BadRequest("Параметр 'limit' має бути більшим за нуль.");

            var users = await _userService.GetAsync(start, limit);

            var result = users.Select(x => x).ToList();

            return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
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
            try
            {
            if (start < 0)
                return BadRequest("Параметр 'start' не може бути від'ємним.");
            if (limit <= 0)
                return BadRequest("Параметр 'limit' має бути більшим за нуль.");

            var popularAuthors = await _userService.GetPopularAuthorsAsync(start, limit);

            var result = popularAuthors.Select(x => x).ToList();

            return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                // Обробка специфічних винятків із сервісу
                return StatusCode(500, new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                // Обробка інших несподіваних винятків
                return StatusCode(500, new { Message = ex.Message });
            }
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
                return BadRequest(new { message = "Необхідні ідентифікатор користувача та оновлені дані користувача." });
            }

            try
            {
                var updatedUserDto = await _userService.UpdateAsync(userId, updatedUser);

                if (updatedUserDto == null)
                {
                    return NotFound(new { message = $"Користувач з ID {userId} не знайдено." });
                }

                return Ok(new {  user = updatedUserDto });
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(new {  error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new {  error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
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
                return BadRequest(new { Message = "Ідентифікатор користувача є обов'язковим." });
            }

            try
            {
                var token = await _userService.InitiateEmailConfirmationAsync(userId);
                return Ok(new { Message = "Лист для підтвердження електронної пошти надіслано." });
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new { Message = "Токен є обов'язковим." });
            }

            try
            {
                var success = await _userService.ConfirmEmailAsync(token);
                if (success)
                    return Ok(new { Message = "Електронна пошта успішно підтверджена." });
                else
                    return BadRequest(new { Message = "Підтвердження електронної пошти не вдалося." });
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        //Скидання пароля через email

        [HttpPost("initiate-password-reset")]
        public async Task<IActionResult> InitiatePasswordReset([FromBody] ResetEmailDto request)
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                return BadRequest(new { Message = "Email є обов'язковим." });
            }

            try
            {
                var token = await _userService.InitiatePasswordResetAsync(request.Email);
                return Ok(new { Message = "Лист для скидання пароля надіслано." });
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromQuery] string token, [FromBody] ResetPasswordDto request)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(request.NewPassword))
            {
                return BadRequest(new { Message = "Токен та новий пароль є обов'язковими." });
            }

            try
            {
                await _userService.ResetPasswordAsync(token, request.NewPassword);
                return Ok(new { Message = "Пароль успішно оновлено." });
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest($"Помилка: {ex.Message}");
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("toggle-favorite-author")]
        public async Task<IActionResult> ToggleFavoriteAuthor([FromBody] ToggleFavoriteAuthorRequestDto request)
        {
            // Перевіряємо, чи запит не порожній
            if (request == null || string.IsNullOrWhiteSpace(request.AuthorId))
            {
                return BadRequest(new { message = "AuthorId не може бути порожнім." });
            }

            try
            {
                // Отримуємо ID авторизованого користувача з токена
                var userId = User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Не вдалося визначити користувача." });
                }

                // Викликаємо метод сервісу
                var isAdded = await _userService.ToggleFavoriteAuthorAsync(userId, request.AuthorId);

                // Формуємо відповідь
                var message = isAdded
                    ? "Автор успішно доданий до улюблених."
                    : "Автор успішно видалений з улюблених.";

                return Ok(new { message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Загальна помилка сервера
                return StatusCode(500, new { message = "Сталася помилка: " + ex.Message });
            }
        }


        [Authorize]
        [HttpGet("favorite-authors")]
        public async Task<IActionResult> GetFavoriteAuthors()
        {
            try
            {
                // Отримуємо ID авторизованого користувача з токена
                var userId = User.FindFirst("id")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Не вдалося визначити користувача." });
                }

                // Викликаємо метод сервісу
                var favoriteAuthors = await _userService.GetFavoriteAuthorsAsync(userId);

                return Ok(new
                {
                    message = "Успішно отримано список улюблених авторів.",
                    authors = favoriteAuthors
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = $"Помилка: {ex.Message}" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = $"Помилка: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Внутрішня помилка сервера. Спробуйте пізніше." });
            }
        }


    }
}
