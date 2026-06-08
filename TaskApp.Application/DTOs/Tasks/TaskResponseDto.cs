namespace TaskApp.Application.DTOs.Tasks;

public class TaskResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskApp.Domain.Enums.TaskStatus Status { get; set; }
    public DateTime DueDate { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<string> Labels { get; set; } = new List<string>();
    public int? StoryPoints { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
    public string? OwnerUsername { get; set; }
}
