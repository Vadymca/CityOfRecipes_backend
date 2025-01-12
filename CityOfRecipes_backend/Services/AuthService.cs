using CityOfRecipes_backend.Models;
using MongoDB.Driver;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

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
            var existingUser = await _dbContext.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (existingUser != null) throw new Exception("User already exists");

            var passwordHash = HashPassword(password);
            var user = new User { Email = email, Password = passwordHash };

            await _dbContext.Users.InsertOneAsync(user);
        }

        public async Task<string> AuthenticateAsync(string email, string password)
        {
            var user = await _dbContext.Users.Find(u => u.Email == email).FirstOrDefaultAsync();
            if (user == null || !VerifyPassword(password, user.Password))
                throw new Exception("Invalid credentials");

            return GenerateJwtToken(user);
        }

        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password), "Password cannot be null or empty.");

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
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
