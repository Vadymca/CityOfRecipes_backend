using CityOfRecipes_backend.Models;
using MongoDB.Driver;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace CityOfRecipes_backend.Services
{
    public class AuthService
    {
        private readonly MongoDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public AuthService(MongoDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        public async Task RegisterAsync(string email, string password)
        {
            // Перевірка: чи є email коректним
            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
                throw new ArgumentException("Некоректний формат електронної пошти.", nameof(email));

            // Перевірка: чи відповідає пароль вимогам
            if (!IsValidPassword(password))
                throw new ArgumentException(
                    "Пароль має бути не менше 6 символів, містити хоча б одну велику букву, одну малу букву та одну цифру.",
                    nameof(password));

            // Перевірка: чи існує вже користувач із таким email
            var existingUser = await _dbContext.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (existingUser != null)
                throw new Exception("Користувач із такою електронною поштою вже існує.");

            // Хешування пароля
            var passwordHash = HashPassword(password);

            // Створення нового користувача
            var user = new User
            {
                Email = email,
                PasswordHash = passwordHash
            };

            // Збереження в базі даних
            await _dbContext.Users.InsertOneAsync(user);
        }

        public async Task<string> AuthenticateAsync(string email, string password)
        {
            // Перевірка: чи є email коректним
            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
                throw new ArgumentException("Некоректний формат електронної пошти.", nameof(email));

            // Перевірка: чи не порожній пароль
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Пароль не може бути порожнім.", nameof(password));

            var user = await _dbContext.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (user == null || !VerifyPassword(password, user.PasswordHash))
                throw new Exception("Недійсні облікові дані");

            return GenerateJwtToken(user);
        }

        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password), "Пароль не може бути пустим або порожнім.");

            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private bool VerifyPassword(string password, string hash) => HashPassword(password) == hash;

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = System.Text.Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new[]
                {
                new System.Security.Claims.Claim("id", user.Id.ToString()),
                new System.Security.Claims.Claim("email", user.Email)
            }),
                Expires = DateTime.UtcNow.AddDays(30),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private bool IsValidEmail(string email)
        {
            return new EmailAddressAttribute().IsValid(email);
        }

        private bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                return false;

            // Регулярний вираз для перевірки вимог :
            // - Мінімум одна мала буква
            // - Мінімум одна велика буква
            // - Мінімум одна цифра
            var passwordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$");
            return passwordRegex.IsMatch(password);
        }

    }
}
