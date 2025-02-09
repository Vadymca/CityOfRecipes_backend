using CityOfRecipes_backend.Models;
using CityOfRecipes_backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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
            builder.Services.AddSingleton<CategoryService>();
            builder.Services.AddSingleton<TagService>();
            builder.Services.AddSingleton<IngredientService>();
            builder.Services.AddSingleton<CityService>();
            builder.Services.AddSingleton<IImageUploadService, ImageUploadService>();
            builder.Services.AddSingleton<CountryService>();
            builder.Services.AddSingleton<ContestService>();
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<TokenService>();
            builder.Services.AddSingleton<RatingService>();
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Secret"])),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });
            builder.Services.AddSingleton<IEmailService>(provider =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                var emailSettings = configuration.GetSection("EmailSettings");
                return new EmailService(
                    emailSettings["SmtpHost"],
                    int.Parse(emailSettings["SmtpPort"]),
                    emailSettings["SmtpUser"],
                    emailSettings["SmtpPass"],
                    emailSettings["FromEmail"]
                );
            });

            // –еЇстрац≥€ Background Service
            builder.Services.AddHostedService<ContestResultScheduler>();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "¬вед≥ть токен у формат≥: Bearer {токен}"
                });

                options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
            });
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
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
