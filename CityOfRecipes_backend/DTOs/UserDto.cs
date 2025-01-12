namespace CityOfRecipes_backend.DTOs
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string About { get; set; } = string.Empty;
        public string ProfilePhotoUrl { get; set; } = string.Empty;
        public string City { get; set; } = "Невідомий";
        public string Country { get; set; } = "Невідомий";
        public double Rating { get; set; }
        public DateTime RegistrationDate { get; set; }
        public List<RecipeDto> FavoriteRecipes { get; set; } = new();
        public List<AuthorDto> FavoriteAuthors { get; set; } = new();
        public bool EmailConfirmed { get; set; }
        public bool TemporaryBan { get; set; }
        public bool PermanentBan { get; set; }
    }
}
