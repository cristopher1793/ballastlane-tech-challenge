namespace TaskApp.Application.DTOs.Seed;

public class DemoSeedResultDto
{
    public string Message { get; set; } = string.Empty;
    public int TasksDeleted { get; set; }
    public int TasksCreated { get; set; }
}
