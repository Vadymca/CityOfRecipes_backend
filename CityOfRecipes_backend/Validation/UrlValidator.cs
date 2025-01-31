namespace CityOfRecipes_backend.Validation
{
    public class UrlValidator
    {
        public static bool IsValidUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return true; // Дозволяємо пусті значення

            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}
