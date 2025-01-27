using CityOfRecipes_backend.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CityOfRecipes_backend.Services
{
    public class CityService
    {
        private readonly IMongoCollection<City> _cities;
        private readonly IMongoCollection<Country> _countries;

        public CityService(IOptions<MongoDBSettings> mongoSettings)
        {
            try
            {
                var client = new MongoClient(mongoSettings.Value.ConnectionString);
                var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
                _cities = database.GetCollection<City>("Cities");
                _countries = database.GetCollection<Country>("Countries");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Не вдалося підключитися до бази даних; {ex.Message}");
            }
        }

        public async Task<List<City>> GetAllAsync()
        {
            try
            {
                return await _cities.Find(_ => true).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }

        public async Task<List<City>> GetByCountryIdAsync(string countryId)
        {
            try
            {
                return await _cities.Find(c => c.CountryId == countryId).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка під час отримання міст для CountryId: {countryId}; {ex.Message}");
            }
        }

        public async Task<City> GetByIdAsync(string id)
        {
            try
            {
                return await _cities.Find(c => c.Id == id).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка під час отримання міста з Id: {id}; {ex.Message}");
            }
        }

        public async Task<City> GetByNameAndCountryAsync(string cityName, string countryId)
        {
            try
            {
                return await _cities.Find(c => c.CityName == cityName && c.CountryId == countryId).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка під час отримання міста з назвою '{cityName}' і CountryId: {countryId}; {ex.Message}");
            }
        }

        public async Task<City> EnsureCityExistsAsync(string cityName, string countryId)
        {
            try
            {
                var existingCity = await GetByNameAndCountryAsync(cityName, countryId);
                if (existingCity != null)
                    return existingCity;

                var newCity = new City
                {
                    CityName = cityName,
                    CountryId = countryId
                };

                await CreateAsync(newCity);
                return newCity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка під час перевірки або створення міста '{cityName}'; {ex.Message}");
            }
        }

        public async Task CreateAsync(City city)
        {
            try
            {
                // Перевіряємо, чи правильний ID країни
                var country = await _countries.Find(c => c.Id == city.CountryId).FirstOrDefaultAsync();
                if (country == null)
                {
                    // Якщо країни з таким ID не знайдено, кидаємо виняток
                    throw new InvalidOperationException($"Країна з ID '{city.CountryId}' не знайдена.");
                }

                // Перевіряємо, чи місто з таким ім'ям вже існує
                var existingCity = await _cities.Find(c => c.CityName == city.CityName && c.CountryId == city.CountryId).FirstOrDefaultAsync();
                if (existingCity != null)
                {
                    // Якщо таке місто вже є, кидаємо виняток
                    throw new InvalidOperationException($"Місто з назвою '{city.CityName}' вже існує в країні з ID '{city.CountryId}'.");
                }

                // Якщо міста не знайдено і країна існує, перевіряємо на валідність і вставляємо нове місто
                city.Validate();
                await _cities.InsertOneAsync(city);
            }
            catch (Exception ex)
            {
                // Логування або інші дії при виникненні помилки
                throw new InvalidOperationException(ex.Message);
            }
        }

        public async Task UpdateAsync(string id, City updatedCity)
        {
            try
            {
                updatedCity.Validate();
                await _cities.ReplaceOneAsync(c => c.Id == id, updatedCity);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка під час оновлення міста з Id: {id}; {ex.Message}");
            }
        }

        public async Task DeleteAsync(string id)
        {
            try
            {
                await _cities.DeleteOneAsync(c => c.Id == id);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка під час видалення міста з Id: {id}; {ex.Message}");
            }
        }
    }
}
