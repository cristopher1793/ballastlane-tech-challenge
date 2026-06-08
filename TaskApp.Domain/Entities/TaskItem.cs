using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TaskApp.Domain.Entities;

public class TaskItem
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("status")]
    public Enums.TaskStatus Status { get; set; } = Enums.TaskStatus.Pending;

    [BsonElement("due_date")]
    public DateTime DueDate { get; set; }

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [BsonElement("labels")]
    public List<string> Labels { get; set; } = new List<string>();

    [BsonElement("storyPoints")]
    public int? StoryPoints { get; set; }

    [BsonElement("updatedBy")]
    public string UpdatedBy { get; set; } = string.Empty;

    [BsonElement("completedAt")]
    public DateTime? CompletedAt { get; set; }
}
