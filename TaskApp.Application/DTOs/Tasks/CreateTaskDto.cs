namespace TaskApp.Application.DTOs.Tasks;

public class CreateTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public List<string> Labels { get; set; } = new List<string>();
    public int? StoryPoints { get; set; }
}
