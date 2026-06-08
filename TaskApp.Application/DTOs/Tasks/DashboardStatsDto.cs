namespace TaskApp.Application.DTOs.Tasks;

public class DashboardStatsDto
{
    public int TotalTasks { get; set; }
    public int ToDo { get; set; }
    public int Pending { get; set; }
    public int InProgress { get; set; }
    public int Completed { get; set; }
    public double OnTimeRate { get; set; }
    public double AverageDaysVariance { get; set; }
    public List<CompletionTimingDto> Timings { get; set; } = new();
    public List<WeeklyVelocityDto> WeeklyVelocity { get; set; } = new();
    public List<EstimationAccuracyDto> EstimationAccuracy { get; set; } = new();
}

public class CompletionTimingDto
{
    public string Title { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public DateTime CompletedAt { get; set; }
    public double DaysVariance { get; set; }
}

public class WeeklyVelocityDto
{
    public string Week { get; set; } = string.Empty;  // "Jun 2"
    public int Points { get; set; }
    public int Tasks { get; set; }
}

public class EstimationAccuracyDto
{
    public int StoryPoints { get; set; }
    public double AvgDays { get; set; }
    public int Count { get; set; }
}
