using CityOfRecipes_backend.Models;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CityOfRecipes_backend.Services
{
    public class CityService
    {
        private readonly IMongoCollection<City> _cities;

        public CityService(IOptions<MongoDBSettings> mongoSettings)

        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _cities = database.GetCollection<City>("Cities");
        }

        public async Task<List<City>> GetAllAsync()
        {
            return await _cities.Find(_ => true).ToListAsync();
        }

        public async Task<List<City>> GetByCountryIdAsync(string countryId)
        {
            return await _cities.Find(c => c.CountryId == countryId).ToListAsync();
        }

        public async Task<City> GetByIdAsync(string id)
        {
            return await _cities.Find(c => c.Id == id).FirstOrDefaultAsync();
        }

        // Метод для отримання міста за назвою і CountryId
        public async Task<City> GetByNameAndCountryAsync(string cityName, string countryId)
        {
            return await _cities.Find(c => c.CityName == cityName && c.CountryId == countryId).FirstOrDefaultAsync();
        }

        // Метод для перевірки існування або додавання нового міста
        public async Task<City> EnsureCityExistsAsync(string cityName, string countryId)
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

        public async Task CreateAsync(City city)
        {
            city.Validate();
            await _cities.InsertOneAsync(city);
        }

        public async Task UpdateAsync(string id, City updatedCity)
        {
            updatedCity.Validate();
            await _cities.ReplaceOneAsync(c => c.Id == id, updatedCity);
        }

        public async Task DeleteAsync(string id)
        {
            await _cities.DeleteOneAsync(c => c.Id == id);
        }
    }
}
