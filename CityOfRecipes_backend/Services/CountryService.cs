using CityOfRecipes_backend.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            try
            {
                return await _countries.Find(_ => true).ToListAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Помилка при отриманні списку країн.", ex);
            }
        }

        public async Task<Country> GetByIdAsync(string id)
        {
            try
            {
                return await _countries.Find(c => c.Id == id).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка при отриманні країни з ID '{id}'.", ex);
            }
        }

        public async Task<Country> GetByNameAsync(string countryName)
        {
            try
            {
                return await _countries.Find(c => c.CountryName == countryName).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка при отриманні країни з назвою '{countryName}'.", ex);
            }
        }

        public async Task<Country> EnsureCountryExistsAsync(string countryName)
        {
            try
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
            catch (Exception ex)
            {
                throw new InvalidOperationException("Помилка при перевірці або додаванні нової країни.", ex);
            }
        }

        public async Task CreateAsync(Country country)
        {
            try
            {
                // Перевіряємо, чи країна з такою назвою вже існує
                var existingCountry = await _countries.Find(c => c.CountryName == country.CountryName).FirstOrDefaultAsync();
                if (existingCountry != null)
                {
                    // Якщо країна з такою назвою вже існує, кидаємо виняток
                    throw new InvalidOperationException($"Країна з назвою '{country.CountryName}' вже існує.");
                }

                // Якщо країна не знайдена, виконуємо валідацію та вставляємо нову країну
                country.Validate();
                await _countries.InsertOneAsync(country);
            }
            catch (Exception ex)
            {
                // Логування або інші дії при виникненні помилки
                throw new InvalidOperationException(ex.Message);
            }
        }

        public async Task UpdateAsync(string id, Country updatedCountry)
        {
            try
            {
                updatedCountry.Validate();
                await _countries.ReplaceOneAsync(c => c.Id == id, updatedCountry);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка при оновленні країни з ID '{id}'.", ex);
            }
        }

        public async Task DeleteAsync(string id)
        {
            try
            {
                await _countries.DeleteOneAsync(c => c.Id == id);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Помилка при видаленні країни з ID '{id}'.", ex);
            }
        }
    }
}
