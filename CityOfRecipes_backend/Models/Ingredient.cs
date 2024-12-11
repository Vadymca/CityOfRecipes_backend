using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace CityOfRecipes_backend.Models
{
    public class Ingredient
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        [BsonElement("IngredientName")]
        [BsonRequired]
        public string IngredientName { get; set; } = string.Empty;
        public void Validate()
        {
            if (IngredientName.Length > 100)
                throw new ArgumentException("Назва інгредієнта перевищує максимальну довжину в 100 символів.");
        }

    }
}
