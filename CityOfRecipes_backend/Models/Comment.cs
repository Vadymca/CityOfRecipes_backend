using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace CityOfRecipes_backend.Models
{
    public class Comment
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        [BsonElement("RecipeId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? RecipeId { get; set; }

        [BsonElement("ParentCommentId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? ParentCommentId { get; set; }

        [BsonElement("AuthorId")]
        [BsonRequired]
        [BsonRepresentation(BsonType.ObjectId)]
        public string AuthorId { get; set; } = null!;

        [BsonElement("DateTime")]
        [BsonRequired]
        public DateTime DateTime { get; set; } = DateTime.UtcNow;

        [BsonElement("CommentText")]
        [BsonRequired]
        public string CommentText { get; set; } = string.Empty;

        [BsonElement("AttachmentUrl")]
        public string? AttachmentUrl { get; set; }

        [BsonIgnore]
        public bool IsValid =>
            (RecipeId != null && ParentCommentId == null) ||
            (RecipeId == null && ParentCommentId != null);
        public void Validate()
        {
            if (CommentText.Length > 1000)
                throw new ArgumentException("Текст коментаря перевищує максимальну довжину в 1000 символів.");
        }

    }
}
