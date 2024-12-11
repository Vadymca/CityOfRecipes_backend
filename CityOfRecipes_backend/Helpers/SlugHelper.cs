namespace CityOfRecipes_backend.Helpers
{
    public static class SlugHelper
    {
        private static readonly Dictionary<char, string> TransliterationMap = new()
        {
            { 'а', "a" }, { 'б', "b" }, { 'в', "v" }, { 'г', "h" }, { 'ґ', "g" },
            { 'д', "d" }, { 'е', "e" }, { 'є', "ye" }, { 'ж', "zh" }, { 'з', "z" },
            { 'и', "y" }, { 'і', "i" }, { 'ї', "yi" }, { 'й', "y" }, { 'к', "k" },
            { 'л', "l" }, { 'м', "m" }, { 'н', "n" }, { 'о', "o" }, { 'п', "p" },
            { 'р', "r" }, { 'с', "s" }, { 'т', "t" }, { 'у', "u" }, { 'ф', "f" },
            { 'х', "kh" }, { 'ц', "ts" }, { 'ч', "ch" }, { 'ш', "sh" }, { 'щ', "shch" },
            { 'ь', "" }, { 'ю', "yu" }, { 'я', "ya" }, { ' ', "-" }, { 'ъ', "" },
            { 'э', "e" }, { 'ы', "y" }, { 'ё', "yo" }, { 'ў', "u" }
        };

        public static string Transliterate(string text)
        {
            var result = new List<string>();

            foreach (var ch in text.ToLower())
            {
                result.Add(TransliterationMap.ContainsKey(ch) ? TransliterationMap[ch] : ch.ToString());
            }

            return string.Join("", result)
                .Replace("--", "-") 
                .Trim('-');          
        }
    }
}
