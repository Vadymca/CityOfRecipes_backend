namespace CityOfRecipes_backend.DTOs
{
    public class AuthorDto
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string ProfilePhotoUrl { get; set; } = string.Empty;
        public string City { get; set; } = "Невідомe місто";
        public string Country { get; set; } = "Невідома країна";
        public DateTime RegistrationDate { get; set; }
        public double Rating { get; set; }

    }
}
