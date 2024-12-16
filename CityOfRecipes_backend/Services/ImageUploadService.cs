namespace CityOfRecipes_backend.Services
{
    public interface IImageUploadService
    {
        Task<string> UploadImageAsync(IFormFile file);
    }

    public class ImageUploadService : IImageUploadService
    {
        private readonly string _tempFolder;
        private readonly string _ibbcoApiKey = "255b0c6decd4b32a3d979a2beedc0e09";

        public ImageUploadService()
        {
            _tempFolder = Path.Combine(Path.GetTempPath(), "TempImages");

            // Перевіряємо, чи існує тимчасова папка, і створюємо її, якщо ні
            if (!Directory.Exists(_tempFolder))
            {
                Directory.CreateDirectory(_tempFolder);
            }
        }

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Файл не завантажений або порожній.");

            // Генеруємо унікальне ім'я для тимчасового файлу
            var tempFilePath = Path.Combine(_tempFolder, Guid.NewGuid() + Path.GetExtension(file.FileName));

            try
            {
                // Зберігаємо файл у тимчасову папку
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Завантажуємо файл на ImgBB
                var imageUrl = await UploadToImgBB(tempFilePath);

                if (string.IsNullOrEmpty(imageUrl))
                    throw new Exception("Помилка завантаження зображення на ImgBB.");

                return imageUrl;
            }
            finally
            {
                // Видаляємо тимчасовий файл
                await Task.Delay(100); // Add a small delay before deleting the file
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        private async Task<string> UploadToImgBB(string imagePath)
        {
            using var client = new HttpClient();
            var form = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(imagePath); // Ensure proper disposal
            var fileName = Path.GetFileName(imagePath);
            form.Add(new StreamContent(fileStream), "image", fileName);

            try
            {
                var response = await client.PostAsync($"https://api.imgbb.com/1/upload?key={_ibbcoApiKey}", form);
                var responseContent = await response.Content.ReadAsStringAsync();
                dynamic result = Newtonsoft.Json.JsonConvert.DeserializeObject(responseContent);
                return result.data.url;
            }
            catch
            {
                return null;
            }
        }
    }
}
