using CityOfRecipes_backend.Models;
using CityOfRecipes_backend.Services;

namespace CityOfRecipes_backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<MongoDBSettings>(
                    builder.Configuration.GetSection("MongoDB"));
            builder.Services.AddSingleton<MongoDbContext>();
            builder.Services.AddSingleton<UserService>();
            builder.Services.AddSingleton<RecipeService>();
            builder.Services.AddSingleton<TagService>();
            builder.Services.AddSingleton<IngredientService>();
            builder.Services.AddSingleton<IImageUploadService, ImageUploadService>();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            // CORS policy
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("*", builder =>
                {
                    builder.WithOrigins("http://localhost:4200")
                           .AllowAnyHeader()
                           .AllowAnyMethod()
                           .AllowCredentials();
                });
            });

            var app = builder.Build();
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("*");
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
