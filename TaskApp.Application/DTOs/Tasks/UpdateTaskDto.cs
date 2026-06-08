using TaskApp.Domain.Enums;

namespace TaskApp.Application.DTOs.Tasks;

public class UpdateTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskApp.Domain.Enums.TaskStatus Status { get; set; }
    public DateTime DueDate { get; set; }
    public List<string> Labels { get; set; } = new List<string>();
    public int? StoryPoints { get; set; }
}
