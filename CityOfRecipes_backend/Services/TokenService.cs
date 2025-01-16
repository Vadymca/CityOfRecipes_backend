namespace CityOfRecipes_backend.Services
{
    public class TokenService
    {
        public string GenerateEmailConfirmationToken()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString(); 
        }
    }
}
