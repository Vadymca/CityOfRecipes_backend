using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace CityOfRecipes_backend.Models
{
    public class Report
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        [BsonElement("AuthorId")]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonRequired]
        public string AuthorId { get; set; } = null!;

        [BsonElement("RecipeId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? RecipeId { get; set; }

        [BsonElement("CommentId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? CommentId { get; set; }

        [BsonElement("DateTime")]
        [BsonRequired]
        public DateTime DateTime { get; set; } = DateTime.UtcNow;

        [BsonElement("ReportText")]
        [BsonRequired]
        public string ReportText { get; set; } = string.Empty;

        [BsonElement("Status")]
        [BsonRequired]
        public string Status { get; set; } = "Open";

        [BsonElement("CloseDateTime")]
        public DateTime? CloseDateTime { get; set; }

        [BsonIgnore]
        public bool IsValid =>
            (RecipeId != null && CommentId == null) ||
            (RecipeId == null && CommentId != null);
        public void Validate()
        {
            if (ReportText.Length > 500)
                throw new ArgumentException("Текст звіту не повинно перевищувати 500 символів.");
            if (Status.Length > 10)
                throw new ArgumentException("Статус не повинно перевищувати 10 символів.");
        }
    }
}
