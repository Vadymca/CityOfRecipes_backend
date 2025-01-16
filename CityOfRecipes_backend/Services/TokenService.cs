namespace CityOfRecipes_backend.Services
{
    public class TokenService
    {
        public string GenerateEmailConfirmationToken()
        {
            return Guid.NewGuid().ToString(); // Унікальний токен
        }
    }
}
