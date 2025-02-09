using CityOfRecipes_backend.DTOs;
using CityOfRecipes_backend.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Org.BouncyCastle.Crypto.Generators;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace CityOfRecipes_backend.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;
        private readonly IMongoCollection<City> _cities;
        private readonly IMongoCollection<Country> _countries;
        private readonly TokenService _tokenService;
        private readonly IEmailService _emailService;

        public UserService(IOptions<MongoDBSettings> mongoSettings, 
                           TokenService tokenService,
                           IEmailService emailService)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _users = database.GetCollection<User>("Users");
            _cities = database.GetCollection<City>("Cities");
            _countries = database.GetCollection<Country>("Countries");
            _tokenService = tokenService;
            _emailService = emailService;
        }

        public async Task<List<AuthorDto>> GetAsync(int start, int limit)
        {
            try
            {
                var pipeline = new[]
                {
                    // Зв'язок з рецептами
                    new BsonDocument
                    {
                        { "$lookup", new BsonDocument
                            {
                                { "from", "Recipes" },
                                { "localField", "_id" },
                                { "foreignField", "AuthorId" },
                                { "as", "Recipes" }
                            }
                        }
                    },
                    // 🔥 Додаємо сортування за рейтингом
                    new BsonDocument
                    {
                        { "$sort", new BsonDocument { { "Rating", -1 } } } // Сортуємо від найбільшого до найменшого
                    },
                    // Пропустити і обмежити (пагінація)
                    new BsonDocument
                    {
                        { "$skip", start }
                    },
                    new BsonDocument
                    {
                        { "$limit", limit }
                    },
                    // Зв'язок з містами
                    new BsonDocument
                    {
                        { "$lookup", new BsonDocument
                            {
                                { "from", "Cities" },
                                { "localField", "CityId" },
                                { "foreignField", "_id" },
                                { "as", "CityDetails" }
                            }
                        }
                    },
                    new BsonDocument
                    {
                        { "$unwind", new BsonDocument
                            {
                                { "path", "$CityDetails" },
                                { "preserveNullAndEmptyArrays", true }
                            }
                        }
                    },
                    // Зв'язок з країнами через місто
                    new BsonDocument
                    {
                        { "$lookup", new BsonDocument
                            {
                                { "from", "Countries" },
                                { "localField", "CityDetails.CountryId" },
                                { "foreignField", "_id" },
                                { "as", "CountryDetails" }
                            }
                        }
                    },
                    new BsonDocument
                    {
                        { "$unwind", new BsonDocument
                            {
                                { "path", "$CountryDetails" },
                                { "preserveNullAndEmptyArrays", true }
                            }
                        }
                    },
                    // Вибір необхідних полів
                    new BsonDocument
                    {
                        { "$project", new BsonDocument
                            {
                                { "Id", "$_id" },
                                { "FirstName", 1 },
                                { "LastName", 1 },
                                { "ProfilePhotoUrl", 1 },
                                { "City", "$CityDetails.CityName" },
                                { "Country", "$CountryDetails.CountryName" },
                                { "RegistrationDate", 1 },
                                { "Rating", 1 },
                                { "About", 1 }
                            }
                        }
                    }
                };

                var results = await _users.Aggregate<BsonDocument>(pipeline).ToListAsync();

                // Формування DTO
                return results.Select(result =>
                {
                    var author = new AuthorDto
                    {
                        Id = result["Id"].ToString(),
                        FirstName = result.Contains("FirstName") ? result["FirstName"].ToString() : null,
                        LastName = result.Contains("LastName") ? result["LastName"].ToString() : null,
                        ProfilePhotoUrl = result.Contains("ProfilePhotoUrl") ? result["ProfilePhotoUrl"].ToString() : null,
                        City = result.Contains("City") ? result["City"].ToString() : "Невідоме місто",
                        Country = result.Contains("Country") ? result["Country"].ToString() : "Невідома країна",
                        RegistrationDate = result.Contains("RegistrationDate") ? result["RegistrationDate"].ToUniversalTime() : DateTime.MinValue,
                        Rating = result.Contains("Rating") ? result["Rating"].ToDouble() : 0,
                        About = result.Contains("About") ? result["About"].ToString() : null
                    };

                    return author;
                }).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка: {ex.Message}");
            }
        }

        public async Task<AuthorDto?> GetByIdAsync(string authorId)
        {
            try
            {
                if (!ObjectId.TryParse(authorId, out _))
                {
                    throw new ArgumentException("Недійсний формат ідентифікатора автора");
                }

                var pipeline = new[]
                {
                    // Фільтр за Id
                    new BsonDocument
                    {
                        { "$match", new BsonDocument
                            {
                                { "_id", new ObjectId(authorId) }
                            }
                        }
                    },
                    // Зв'язок з містами
                    new BsonDocument
                    {
                        { "$lookup", new BsonDocument
                            {
                                { "from", "Cities" },
                                { "localField", "CityId" },
                                { "foreignField", "_id" },
                                { "as", "CityDetails" }
                            }
                        }
                    },
                    new BsonDocument
                    {
                        { "$unwind", new BsonDocument
                            {
                                { "path", "$CityDetails" },
                                { "preserveNullAndEmptyArrays", true }
                            }
                        }
                    },
                    // Зв'язок з країнами через місто
                    new BsonDocument
                    {
                        { "$lookup", new BsonDocument
                            {
                                { "from", "Countries" },
                                { "localField", "CityDetails.CountryId" },
                                { "foreignField", "_id" },
                                { "as", "CountryDetails" }
                            }
                        }
                    },
                    new BsonDocument
                    {
                        { "$unwind", new BsonDocument
                            {
                                { "path", "$CountryDetails" },
                                { "preserveNullAndEmptyArrays", true }
                            }
                        }
                    },
                    // Вибір необхідних полів
                    new BsonDocument
                    {
                        { "$project", new BsonDocument
                            {
                                { "_id", 1 },
                                { "FirstName", 1 },
                                { "LastName", 1 },
                                { "ProfilePhotoUrl", 1 },
                                { "City", new BsonDocument
                                    {
                                        { "$ifNull", new BsonArray { "$CityDetails.CityName", "Невідоме місто" } }
                                    }
                                },
                                { "Country", new BsonDocument
                                    {
                                        { "$ifNull", new BsonArray { "$CountryDetails.CountryName", "Невідома країна" } }
                                    }
                                },
                                { "RegistrationDate", 1 },
                                { "Rating", 1 },
                                { "About", 1 }
                            }
                        }
                    }
                };

                var result = await _users.Aggregate<AuthorDto>(pipeline).FirstOrDefaultAsync();
                return result;
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"Помилка у форматі ID: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Виникла помилка під час виконання GetByIdAsync: {ex.Message}");
                throw new InvalidOperationException($"Сталася помилка під час отримання автора: {ex.Message}");
            }
        }

        public async Task<AboutUserDto?> GetAboutMeAsync(string userId)
        {
            // Перевіряємо, чи валідний ObjectId
            if (!ObjectId.TryParse(userId, out _))
                throw new ArgumentException("Невірний формат UserId.");

            // Шукаємо користувача за Id
            var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null)
                throw new KeyNotFoundException("Користувача не знайдено.");

            // Отримуємо місто користувача
            var city = await _cities.Find(c => c.Id == user.CityId).FirstOrDefaultAsync();
            var cityName = city?.CityName ?? "Невідоме місто";

            // Отримуємо країну користувача
            var country = city != null ? await _countries.Find(c => c.Id == city.CountryId).FirstOrDefaultAsync() : null;
            var countryName = country?.CountryName ?? "Невідома країна";

            // Повертаємо DTO з інформацією про користувача
            return new AboutUserDto
            {
                Id = user.Id,
                RoleId = user.RoleId,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                About = user.About,
                ProfilePhotoUrl = user.ProfilePhotoUrl,
                City = cityName,
                Country = countryName,
                RegistrationDate = user.RegistrationDate,
                Rating = user.Rating,
                EmailConfirmed = user.EmailConfirmed
            };
        }
        public async Task<List<AuthorDto>> GetPopularAuthorsAsync(int start, int limit)
        {
            try
            {
            var authors = await GetAsync(start, limit);

            return authors;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Сталася помилка під час отримання популярних авторів: {ex.Message}");
            }
        }

        public async Task<UserDto?> UpdateAsync(string userId, UserDto updatedUser, string? newPassword=null)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new ArgumentNullException(nameof(userId), "Ідентифікатор користувача не може бути пустим.");

                // Завантажуємо поточного користувача з бази
                var existingUser = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
                if (existingUser == null)
                    throw new Exception("Користувача з вказаним ID не знайдено.");

                var updates = new List<UpdateDefinition<User>>();
                var updateDefinitionBuilder = Builders<User>.Update;

                // Оновлюємо лише ті поля, які передані (перевірка на null):
                if (!string.IsNullOrEmpty(updatedUser.Email))
                    updates.Add(updateDefinitionBuilder.Set(u => u.Email, updatedUser.Email));

                if (!string.IsNullOrEmpty(updatedUser.FirstName))
                    updates.Add(updateDefinitionBuilder.Set(u => u.FirstName, updatedUser.FirstName));

                if (!string.IsNullOrEmpty(updatedUser.LastName))
                    updates.Add(updateDefinitionBuilder.Set(u => u.LastName, updatedUser.LastName));

                if (!string.IsNullOrEmpty(updatedUser.About))
                    updates.Add(updateDefinitionBuilder.Set(u => u.About, updatedUser.About));

                if (!string.IsNullOrEmpty(updatedUser.ProfilePhotoUrl))
                    updates.Add(updateDefinitionBuilder.Set(u => u.ProfilePhotoUrl, updatedUser.ProfilePhotoUrl));

                if (!string.IsNullOrEmpty(updatedUser.Password))
                {
                    // Перевірка: чи відповідає пароль вимогам
                    if (!IsValidPassword(updatedUser.Password))
                        throw new ArgumentException(
                            "Пароль має бути не менше 6 символів, містити хоча б одну велику букву, одну малу букву та одну цифру.",
                            nameof(updatedUser.Password));

                    newPassword = updatedUser.Password;

                    // Генеруємо хеш нового пароля
                    var newHashedPassword = HashPassword(updatedUser.Password);

                    updates.Add(updateDefinitionBuilder.Set(u => u.PasswordHash, newHashedPassword));
                }

                // Обробляємо поле City, якщо воно передане
                if (updatedUser.CityId != null)
                {
                    if (string.IsNullOrWhiteSpace(updatedUser.CityId))
                    {
                        // Якщо поле передане як пустий рядок або пробіли, очищуємо CityId
                        updates.Add(updateDefinitionBuilder.Set(u => u.CityId, null));
                    }
                    else if (ObjectId.TryParse(updatedUser.CityId, out _))
                    {
                        // Якщо City є валідним ObjectId, виконуємо пошук у базі
                        var city = await _cities.Find(c => c.Id == updatedUser.CityId).FirstOrDefaultAsync();
                        if (city == null)
                            throw new Exception("Місто з вказаним ID не знайдено.");

                        // Додаємо оновлення, якщо місто знайдено
                        updates.Add(updateDefinitionBuilder.Set(u => u.CityId, city.Id));
                    }
                    else
                    {
                        // Якщо City не є валідним ObjectId, викидаємо помилку
                        throw new ArgumentException($"CityId '{updatedUser.CityId}' не є валідним ObjectId.");
                    }
                }

                // Якщо немає оновлень, нічого не змінюємо
                if (!updates.Any())
                    return new UserDto
                    {
                        Id = existingUser.Id,
                        Email = existingUser.Email,
                        FirstName = existingUser.FirstName,
                        LastName = existingUser.LastName,
                        About = existingUser.About,
                        ProfilePhotoUrl = existingUser.ProfilePhotoUrl,
                        CityId = existingUser.CityId,
                    };

                // Комбінуємо всі оновлення
                var updateDefinition = updateDefinitionBuilder.Combine(updates);
                var result = await _users.UpdateOneAsync(u => u.Id == userId, updateDefinition);

                if (result.MatchedCount == 0)
                    return null;

                // Повертаємо оновлені дані користувача
                var updatedCity = existingUser.CityId != null
                    ? await _cities.Find(c => c.Id == existingUser.CityId).FirstOrDefaultAsync()
                    : null;

                var updatedCountry = updatedCity != null
                    ? await _countries.Find(c => c.Id == updatedCity.CountryId).FirstOrDefaultAsync()
                    : null;

                return new UserDto
                {
                    Id = userId,
                    Email = !string.IsNullOrEmpty(updatedUser.Email) ? updatedUser.Email : existingUser.Email,
                    Password = newPassword,
                    FirstName = !string.IsNullOrEmpty(updatedUser.FirstName) ? updatedUser.FirstName : existingUser.FirstName,
                    LastName = !string.IsNullOrEmpty(updatedUser.LastName) ? updatedUser.LastName : existingUser.LastName,
                    About = !string.IsNullOrEmpty(updatedUser.About) ? updatedUser.About : existingUser.About,
                    ProfilePhotoUrl = !string.IsNullOrEmpty(updatedUser.ProfilePhotoUrl) ? updatedUser.ProfilePhotoUrl : existingUser.ProfilePhotoUrl,
                    CityId = !string.IsNullOrEmpty(updatedUser.CityId) ? updatedUser.CityId : existingUser.CityId
                };
            }
            catch (ArgumentNullException ex)
            {
                // Обробка помилки, коли userId порожній
                throw new InvalidOperationException($"Помилка: {ex.Message}", ex);
            }
            catch (ArgumentException ex)
            {
                // Обробка помилки при невалідному параметрі
                throw new InvalidOperationException($"Помилка: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Загальна обробка всіх інших помилок
                throw new InvalidOperationException("Помилка при оновленні користувача.", ex);
            }
        }

        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password), "Пароль не може бути пустим або порожнім.");

            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        public async Task RemoveAsync(string id)
        {
            // Знаходимо користувача за id
            var user = await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
            if (user == null)
                throw new Exception("Користувача з таким ID не знайдено.");

            // Оновлюємо дані користувача
            var updateDefinition = Builders<User>.Update
                .Set(u => u.Email, null)  // Видаллення електронної пошти
                .Set(u => u.FirstName, "Видалений ") // Заміна імені
                .Set(u => u.LastName, "аккаунт")  // Заміна прізвища
                .Set(u => u.ProfilePhotoUrl, null); // Видалення аватарки

            // Застосовуємо оновлення до користувача
            var result = await _users.UpdateOneAsync(u => u.Id == id, updateDefinition);

            // Перевірка, чи оновлені дані користувача
            if (result.ModifiedCount == 0)
                throw new Exception("Не вдалося оновити дані користувача.");

        }


        // Підтвердження електронної пошти
        public async Task<string> InitiateEmailConfirmationAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId), "Ідентифікатор користувача не може бути пустим.");
            try
            {
                // Знаходимо користувача
                var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
                if (user == null)
                    throw new Exception("Користувача не знайдено.");
                // Генеруємо токен
                var token = _tokenService.GenerateEmailConfirmationToken();
                user.EmailConfirmationToken = token;
                // Оновлюємо користувача
                var updateDefinition = Builders<User>.Update
                    .Set(u => u.EmailConfirmationToken, token);
                await _users.UpdateOneAsync(u => u.Id == userId, updateDefinition);
                // Надсилаємо листа користувачу
                //var confirmationLink = $"https://localhost:7186/api/user/confirm-email?token={token}";
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Підтвердження електронної пошти",
                    $"Ваш код підтвердження: {token}\n\n" +
                    "Скопіюйте цей код і введіть його в програмі, щоб підтвердити вашу електронну адресу."
                    );

                return token;
            }
            catch (ArgumentNullException ex)
            {
                throw new ArgumentNullException($"Некоректні дані: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Сталася помилка при ініціалізації підтвердження електронної пошти: {ex.Message}");
            }
        }

        public async Task<bool> ConfirmEmailAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token), "Токен не може бути пустим.");

            try
            {
                // Знаходимо користувача за токеном
                var user = await _users.Find(u => u.EmailConfirmationToken == token).FirstOrDefaultAsync();
                if (user == null)
                    throw new Exception("Токен недійсний або користувача не знайдено.");

                // Підтверджуємо email
                var updateDefinition = Builders<User>.Update
                    .Set(u => u.EmailConfirmed, true)
                    .Set(u => u.EmailConfirmationToken, null);
                var result = await _users.UpdateOneAsync(u => u.Id == user.Id, updateDefinition);

                return result.ModifiedCount > 0;
            }
            catch (ArgumentNullException ex)
            {
                throw new ArgumentNullException($"Некоректний токен: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Сталася помилка при підтвердженні електронної пошти: {ex.Message}");
            }
        }

        //Скидання пароля через email
        public async Task<string> InitiatePasswordResetAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentNullException(nameof(email), "Email не може бути пустим.");

            try
            {
                // Знаходимо користувача за email
                var user = await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
                if (user == null)
                    throw new Exception("Користувача з таким email не знайдено.");

                // Генеруємо токен
                var token = _tokenService.GenerateEmailConfirmationToken();
                user.PasswordResetToken = token;

                var updateDefinition = Builders<User>.Update
                    .Set(u => u.PasswordResetToken, token);

                // Оновлюємо користувача в базі даних
                var updateResult = await _users.UpdateOneAsync(u => u.Id == user.Id, updateDefinition);
                if (updateResult.ModifiedCount == 0)
                    throw new Exception("Не вдалося оновити користувача з новим токеном.");

                // Надсилаємо лист для скидання пароля
                var resetLink = $"http://localhost:4200/reset-password?token={token}";
                await _emailService.SendEmailAsync(user.Email, "Скидання пароля",
                    $"Перейдіть за посиланням, щоб скинути ваш пароль: {resetLink}");

                return token;
            }
            catch (ArgumentNullException ex)
            {
                throw new ArgumentNullException($"Некоректний email: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Сталася помилка при ініціалізації скидання пароля: {ex.Message}");
            }
        }

        public async Task ResetPasswordAsync(string token, string newPassword)
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token), "Токен не може бути порожнім.");

            if (string.IsNullOrEmpty(newPassword))
                throw new ArgumentNullException(nameof(newPassword), "Пароль не може бути порожнім.");

            try
            {
                // Знаходимо користувача за токеном
                var user = await _users.Find(u => u.PasswordResetToken == token).FirstOrDefaultAsync();
                if (user == null)
                    throw new Exception("Токен недійсний або користувача не знайдено.");

                // Перевірка: чи відповідає пароль вимогам
                if (!IsValidPassword(newPassword))
                    throw new ArgumentException(
                        "Пароль має бути не менше 6 символів, містити хоча б одну велику букву, одну малу букву та одну цифру.",
                        nameof(newPassword));

                // Хешуємо новий пароль
                var hashedPassword = HashPassword(newPassword);

                // Створюємо оновлення
                var updateDefinition = Builders<User>.Update
                    .Set(u => u.PasswordHash, hashedPassword)
                    .Set(u => u.PasswordResetToken, null); // Видаляємо токен

                // Оновлюємо пароль користувача в базі
                var result = await _users.UpdateOneAsync(u => u.Id == user.Id, updateDefinition);
                if (result.ModifiedCount == 0)
                    throw new Exception("Не вдалося оновити пароль. Можливо, користувач вже підтвердив скидання пароля.");

            }
            catch (ArgumentNullException ex)
            {
                throw new ArgumentNullException($"Помилка: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Помилка при скиданні пароля: {ex.Message}");
            }
        }

        public async Task<bool> ToggleFavoriteAuthorAsync(string userId, string authorId, bool isAdded=true)
        {

            if (string.IsNullOrEmpty(userId))
                throw new ArgumentException("Ідентифікатор користувача не може бути пустим.", nameof(userId));

            if (string.IsNullOrEmpty(authorId))
                throw new ArgumentException("Ідентифікатор автора не може бути пустим.", nameof(authorId));

            // Перевіряємо, чи користувач не намагається додати самого себе
            if (userId == authorId)
                throw new InvalidOperationException("Ви не можете додати самого себе до улюблених авторів.");

            // Перетворюємо authorId у ObjectId
            if (!ObjectId.TryParse(authorId, out ObjectId authorObjectId))
                throw new ArgumentException("Невірний формат ідентифікатора автора.", nameof(authorId));

            // Завантажуємо користувача
            var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null)
                throw new KeyNotFoundException("Користувача з вказаним ID не знайдено.");

            // Якщо список улюблених авторів ще не ініціалізований
            if (user.FavoriteAuthors == null)
            {
                user.FavoriteAuthors = new List<ObjectId> { authorObjectId };
            }
            else
            {
                
                // Додаємо або видаляємо автора
                if (user.FavoriteAuthors.Contains(authorObjectId))
                {
                    user.FavoriteAuthors.Remove(authorObjectId); // Видаляємо з улюблених
                    isAdded = false;
                }
                else
                {
                    user.FavoriteAuthors.Add(authorObjectId); // Додаємо в улюблені
                    isAdded = true;
                }
            }

            // Оновлюємо користувача в базі
            var updateDefinition = Builders<User>.Update.Set(u => u.FavoriteAuthors, user.FavoriteAuthors);
            var updateResult = await _users.UpdateOneAsync(u => u.Id == userId, updateDefinition);

            return isAdded;
        }

        public async Task<List<AuthorDto>> GetFavoriteAuthorsAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("Ідентифікатор користувача не може бути порожнім.", nameof(userId));

            // Отримуємо користувача з бази
            var user = await _users.Find(u => u.Id == userId).FirstOrDefaultAsync();
            if (user == null)
                throw new KeyNotFoundException("Користувача з вказаним ID не знайдено.");

            // Якщо у користувача немає улюблених авторів, повертаємо порожній список
            if (user.FavoriteAuthors == null || !user.FavoriteAuthors.Any())
                return new List<AuthorDto>();

            // Перетворюємо список ObjectId у користувача на список строкових ID
            var favoriteAuthorIds = user.FavoriteAuthors.Select(id => id.ToString()).ToList();

            // Отримуємо улюблених авторів, використовуючи їх ObjectId
            var favoriteAuthors = await _users
                .Find(u => favoriteAuthorIds.Contains(u.Id)) // Порівнюємо у вигляді рядків
                .ToListAsync();

            // Отримуємо ID міст та країн
            var cityIds = favoriteAuthors
                .Where(author => author.CityId != null)
                .Select(author => author.CityId)
                .Distinct()
                .ToList();

            var cities = await _cities.Find(c => cityIds.Contains(c.Id)).ToListAsync();

            var countryIds = cities
                .Where(city => city.CountryId != null)
                .Select(city => city.CountryId)
                .Distinct()
                .ToList();

            var countries = await _countries.Find(c => countryIds.Contains(c.Id)).ToListAsync();

            // Формуємо список AuthorDto
            var authorDtos = favoriteAuthors.Select(author =>
            {
                var city = cities.FirstOrDefault(c => c.Id == author.CityId);
                var country = city != null ? countries.FirstOrDefault(c => c.Id == city.CountryId) : null;

                return new AuthorDto
                {
                    Id = author.Id.ToString(), // Перетворення ObjectId в рядок
                    FirstName = author.FirstName,
                    LastName = author.LastName,
                    ProfilePhotoUrl = author.ProfilePhotoUrl,
                    City = city?.CityName ?? "Невідоме місто",
                    Country = country?.CountryName ?? "Невідома країна",
                    RegistrationDate = author.RegistrationDate,
                    Rating = author.Rating,
                    About = author.About
                };
            }).ToList();

            return authorDtos;
        }

        private bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                return false;

            // Регулярний вираз для перевірки вимог:
            // - Мінімум одна мала буква
            // - Мінімум одна велика буква
            // - Мінімум одна цифра
            var passwordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$");
            return passwordRegex.IsMatch(password);
        }
    }
}
