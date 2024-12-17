using CityOfRecipes_backend.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace CityOfRecipes_backend.Services
{
    public class CountryService
    {
        private readonly IMongoCollection<Country> _countries;

        public CountryService(IOptions<MongoDBSettings> mongoSettings)
        {
            var client = new MongoClient(mongoSettings.Value.ConnectionString);
            var database = client.GetDatabase(mongoSettings.Value.DatabaseName);
            _countries = database.GetCollection<Country>("Countries");
        }

        public async Task<List<Country>> GetAllAsync()
        {
            return await _countries.Find(_ => true).ToListAsync();
        }

        public async Task<Country> GetByIdAsync(string id)
        {
            return await _countries.Find(c => c.Id == id).FirstOrDefaultAsync();
        }

        // Метод для отримання країни за назвою
        public async Task<Country> GetByNameAsync(string countryName)
        {
            return await _countries.Find(c => c.CountryName == countryName).FirstOrDefaultAsync();
        }

        // Метод для перевірки існування або додавання нової країни
        public async Task<Country> EnsureCountryExistsAsync(string countryName)
        {
            var existingCountry = await GetByNameAsync(countryName);
            if (existingCountry != null)
                return existingCountry;

            var newCountry = new Country
            {
                CountryName = countryName
            };

            await CreateAsync(newCountry);
            return newCountry;
        }

        public async Task CreateAsync(Country country)
        {
            country.Validate();
            await _countries.InsertOneAsync(country);
        }

        public async Task UpdateAsync(string id, Country updatedCountry)
        {
            updatedCountry.Validate();
            await _countries.ReplaceOneAsync(c => c.Id == id, updatedCountry);
        }

        public async Task DeleteAsync(string id)
        {
            await _countries.DeleteOneAsync(c => c.Id == id);
        }
    }
}
