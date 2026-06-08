using TaskApp.Application.DTOs.Tasks;
using TaskApp.Application.Interfaces;
using TaskApp.Domain.Entities;
using TaskApp.Domain.Exceptions;
using TaskApp.Domain.Interfaces.Repositories;

namespace TaskApp.Application.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IUserRepository _userRepository;

    public TaskService(ITaskRepository taskRepository, IUserRepository userRepository)
    {
        _taskRepository = taskRepository;
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<TaskResponseDto>> GetAllAsync(string userId, bool isAdmin = false)
    {
        IEnumerable<TaskItem> tasks = isAdmin
            ? await _taskRepository.GetAllAsync()
            : await _taskRepository.GetAllByUserIdAsync(userId);

        if (isAdmin)
        {
            IEnumerable<User> users = await _userRepository.GetAllAsync();
            Dictionary<string, string> usernameMap = users.ToDictionary(u => u.Id, u => u.Username);
            return tasks.Select(t => MapToDto(t, usernameMap.GetValueOrDefault(t.UserId)));
        }

        return tasks.Select(t => MapToDto(t));
    }

    public async Task<IEnumerable<string>> GetAllLabelsAsync(string userId, bool isAdmin = false)
    {
        return isAdmin
            ? await _taskRepository.GetAllLabelsAsync()
            : await _taskRepository.GetAllLabelsByUserIdAsync(userId);
    }

    public async Task<TaskResponseDto> GetByIdAsync(string id, string userId, bool isAdmin = false)
    {
        TaskItem? task = await _taskRepository.GetByIdAsync(id);
        if (task is null)
            throw new DomainException("Task not found.");
        if (!isAdmin && task.UserId != userId)
            throw new DomainException("Access denied.");
        return MapToDto(task);
    }

    public async Task<TaskResponseDto> CreateAsync(CreateTaskDto dto, string userId)
    {
        ValidateTitle(dto.Title);
        if (dto.DueDate.ToUniversalTime() < DateTime.UtcNow)
            throw new DomainException("Due date cannot be in the past.");
        ValidateStoryPoints(dto.StoryPoints);

        DateTime now = DateTime.UtcNow;
        TaskItem task = new TaskItem
        {
            Title = dto.Title,
            Description = dto.Description,
            DueDate = dto.DueDate,
            Labels = dto.Labels.Select(l => l.Trim()).Where(l => l.Length > 0).Distinct().ToList(),
            StoryPoints = dto.StoryPoints,
            UserId = userId,
            Status = TaskApp.Domain.Enums.TaskStatus.ToDo,
            CreatedAt = now,
            UpdatedAt = now
        };

        TaskItem created = await _taskRepository.CreateAsync(task);
        return MapToDto(created);
    }

    public async Task<TaskResponseDto> UpdateAsync(string id, UpdateTaskDto dto, string userId, bool isAdmin = false)
    {
        TaskItem? task = await _taskRepository.GetByIdAsync(id);
        if (task is null)
            throw new DomainException("Task not found.");
        if (!isAdmin && task.UserId != userId)
            throw new DomainException("Access denied.");

        ValidateTitle(dto.Title);
        ValidateStoryPoints(dto.StoryPoints);

        bool wasCompleted = task.Status == TaskApp.Domain.Enums.TaskStatus.Completed;
        bool isNowCompleted = dto.Status == TaskApp.Domain.Enums.TaskStatus.Completed;

        task.Title = dto.Title;
        task.Description = dto.Description;
        task.Status = dto.Status;
        task.DueDate = dto.DueDate;
        task.Labels = dto.Labels.Select(l => l.Trim()).Where(l => l.Length > 0).Distinct().ToList();
        task.StoryPoints = dto.StoryPoints;
        task.UpdatedAt = DateTime.UtcNow;
        task.UpdatedBy = userId;

        if (isNowCompleted && !wasCompleted)
            task.CompletedAt = DateTime.UtcNow;
        else if (!isNowCompleted && wasCompleted)
            task.CompletedAt = null;

        TaskItem updated = await _taskRepository.UpdateAsync(task);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(string id, string userId, bool isAdmin = false)
    {
        TaskItem? task = await _taskRepository.GetByIdAsync(id);
        if (task is null)
            throw new DomainException("Task not found.");
        if (!isAdmin && task.UserId != userId)
            throw new DomainException("Access denied.");
        await _taskRepository.DeleteAsync(id);
    }

    public async Task<DashboardStatsDto> GetDashboardStatsAsync(string userId, bool isAdmin = false)
    {
        IEnumerable<TaskItem> allTasks = isAdmin
            ? await _taskRepository.GetAllAsync()
            : await _taskRepository.GetAllByUserIdAsync(userId);
        List<TaskItem> list = allTasks.ToList();

        List<TaskItem> completedWithDates = list
            .Where(t => t.Status == TaskApp.Domain.Enums.TaskStatus.Completed && t.CompletedAt.HasValue)
            .ToList();

        // Completion timing (due date vs actual)
        List<CompletionTimingDto> timings = completedWithDates
            .Select(t => new CompletionTimingDto
            {
                Title = t.Title,
                DueDate = t.DueDate,
                CompletedAt = t.CompletedAt!.Value,
                DaysVariance = Math.Round((t.DueDate - t.CompletedAt!.Value).TotalDays, 1)
            })
            .OrderBy(t => t.CompletedAt)
            .ToList();

        int onTimeCount = completedWithDates.Count(t => t.CompletedAt!.Value <= t.DueDate);
        double onTimeRate = completedWithDates.Count > 0
            ? Math.Round((double)onTimeCount / completedWithDates.Count * 100, 1)
            : 0;
        double avgVariance = timings.Count > 0
            ? Math.Round(timings.Average(t => t.DaysVariance), 1)
            : 0;

        // Weekly velocity — group completed tasks by the Monday of their completion week
        List<WeeklyVelocityDto> weeklyVelocity = completedWithDates
            .GroupBy(t => GetWeekStart(t.CompletedAt!.Value))
            .OrderBy(g => g.Key)
            .Select(g => new WeeklyVelocityDto
            {
                Week = g.Key.ToString("MMM d"),
                Points = g.Sum(t => t.StoryPoints ?? 0),
                Tasks = g.Count()
            })
            .ToList();

        // Estimation accuracy — avg days from creation to completion, grouped by story point value
        List<EstimationAccuracyDto> estimationAccuracy = completedWithDates
            .Where(t => t.StoryPoints.HasValue)
            .GroupBy(t => t.StoryPoints!.Value)
            .OrderBy(g => g.Key)
            .Select(g => new EstimationAccuracyDto
            {
                StoryPoints = g.Key,
                AvgDays = Math.Round(g.Average(t => (t.CompletedAt!.Value - t.CreatedAt).TotalDays), 1),
                Count = g.Count()
            })
            .ToList();

        return new DashboardStatsDto
        {
            TotalTasks = list.Count,
            ToDo = list.Count(t => t.Status == TaskApp.Domain.Enums.TaskStatus.ToDo),
            Pending = list.Count(t => t.Status == TaskApp.Domain.Enums.TaskStatus.Pending),
            InProgress = list.Count(t => t.Status == TaskApp.Domain.Enums.TaskStatus.InProgress),
            Completed = list.Count(t => t.Status == TaskApp.Domain.Enums.TaskStatus.Completed),
            OnTimeRate = onTimeRate,
            AverageDaysVariance = avgVariance,
            Timings = timings,
            WeeklyVelocity = weeklyVelocity,
            EstimationAccuracy = estimationAccuracy
        };
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        int diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff).Date;
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title is required.");
        if (title.Length > 200)
            throw new DomainException("Title cannot exceed 200 characters.");
    }

    private static void ValidateStoryPoints(int? sp)
    {
        if (sp.HasValue && (sp.Value < 1 || sp.Value > 100))
            throw new DomainException("Story points must be between 1 and 100.");
    }

    private static TaskResponseDto MapToDto(TaskItem task, string? ownerUsername = null)
    {
        return new TaskResponseDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            DueDate = task.DueDate,
            Labels = task.Labels,
            StoryPoints = task.StoryPoints,
            UserId = task.UserId,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            UpdatedBy = task.UpdatedBy,
            CompletedAt = task.CompletedAt,
            OwnerUsername = ownerUsername,
        };
    }
}
