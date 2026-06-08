using TaskApp.Application.DTOs.Seed;
using TaskApp.Application.Interfaces;
using TaskApp.Domain.Entities;
using TaskApp.Domain.Interfaces.Repositories;
using TaskStatus = TaskApp.Domain.Enums.TaskStatus;

namespace TaskApp.Application.Services;

public class DemoSeedService(ITaskRepository taskRepository) : IDemoSeedService
{
    private static readonly string[][] TaskPool =
    [
        ["Set up project scaffolding", "Initialise solution, configure CI pipeline, add Docker support."],
        ["Fix JWT token expiry edge case", "Tokens rejected inconsistently after password change."],
        ["Design task card UI components", "Reusable card, badge, and status pill components in React."],
        ["Implement REST API layer", "CRUD endpoints with JWT auth, validation, and rate limiting."],
        ["Fix responsive layout issues", "Bottom nav overlap and card spacing on small screens."],
        ["Performance optimisation", "Profile slow MongoDB queries and add missing indexes."],
        ["Dashboard analytics charts", "Velocity, timing dumbbell, and estimation accuracy charts."],
        ["Write integration tests", "Cover all task CRUD and auth endpoints with xUnit."],
        ["Dark mode support", "Add Tailwind dark variant and persisted theme toggle."],
        ["Email notification system", "Send reminders when tasks approach their due date."],
        ["Export tasks to CSV", "Allow users to download a filtered task list as CSV."],
        ["User activity audit log", "Record all create/update/delete events per user."],
        ["Team collaboration features", "Shared task boards, assignments, and real-time updates."],
        ["Refactor authentication middleware", "Extract token validation into reusable middleware pipeline."],
        ["Add search and filtering", "Full-text search on task titles with label and status filters."],
        ["Implement cursor pagination", "Cursor-based pagination for task list, default page size 20."],
        ["Fix memory leak in background job", "Service holding event subscriptions across request scope."],
        ["Improve error handling", "Standardise error response format with problem details middleware."],
        ["Add role-based permissions", "Guard admin endpoints with policy-based authorization."],
        ["Load testing and benchmarks", "k6 load test simulating 500 concurrent users on endpoints."],
        ["Mobile push notifications", "Firebase Cloud Messaging for task deadline reminders."],
        ["Implement file attachments", "Allow users to attach files to tasks via object storage."],
        ["Build admin dashboard", "Aggregate user activity metrics visible to admin role only."],
        ["Add two-factor authentication", "TOTP-based 2FA for accounts with elevated roles."],
        ["Database index review", "Audit and add missing indexes to tasks and users collections."],
    ];

    private static readonly string[][] LabelPool =
    [
        ["backend"],
        ["frontend"],
        ["bug"],
        ["feature"],
        ["testing"],
        ["devops"],
        ["backend", "feature"],
        ["frontend", "feature"],
        ["bug", "backend"],
        ["frontend", "bug"],
        ["testing", "backend"],
        ["feature", "devops"],
        ["backend", "devops"],
    ];

    private static readonly int[] FibSP = [1, 2, 3, 5, 8];

    public async Task<DemoSeedResultDto> SeedForUserAsync(string userId)
    {
        var existing = await taskRepository.GetAllByUserIdAsync(userId);
        var existingList = existing.ToList();
        foreach (var t in existingList)
            await taskRepository.DeleteAsync(t.Id);

        var rng = new Random();
        var now = DateTime.UtcNow;
        var tasks = BuildSeedTasks(userId, now, rng);

        foreach (var task in tasks)
            await taskRepository.CreateAsync(task);

        return new DemoSeedResultDto
        {
            Message = $"Seeded {tasks.Count} demo tasks.",
            TasksDeleted = existingList.Count,
            TasksCreated = tasks.Count,
        };
    }

    private static List<TaskItem> BuildSeedTasks(string userId, DateTime now, Random rng)
    {
        // Pick 13 distinct tasks from the pool in a random order each time
        var pool = TaskPool.OrderBy(_ => rng.Next()).Take(13).ToArray();
        int idx = 0;
        var tasks = new List<TaskItem>();

        // Completed (5): anchored to 4 distinct week buckets so velocity chart always has data.
        // Bases are far enough apart that ±1 day jitter cannot cross a week boundary.
        int[] completedBases = [-3, -10, -10, -17, -24];
        foreach (int baseDay in completedBases)
        {
            int sp = FibSP[rng.Next(FibSP.Length)];
            var labels = LabelPool[rng.Next(LabelPool.Length)];

            var completedAt = now.AddDays(baseDay + rng.Next(-1, 2)); // ±1 day within the week
            int variance = rng.Next(-4, 5);                            // negative = early, positive = late
            var dueDate = completedAt.AddDays(-variance);              // dueDate = completedAt − variance
            var createdAt = dueDate.AddDays(-rng.Next(5, 21));

            tasks.Add(Make(userId, pool[idx][0], pool[idx][1],
                TaskStatus.Completed, sp, labels, createdAt, dueDate, completedAt));
            idx++;
        }

        // InProgress (2): due in the next 2–12 days
        for (int i = 0; i < 2; i++)
        {
            int sp = FibSP[rng.Next(FibSP.Length)];
            var labels = LabelPool[rng.Next(LabelPool.Length)];
            var dueDate = now.AddDays(rng.Next(2, 13));
            tasks.Add(Make(userId, pool[idx][0], pool[idx][1],
                TaskStatus.InProgress, sp, labels,
                now.AddDays(-rng.Next(3, 10)), dueDate));
            idx++;
        }

        // Pending (3): due in the next 10–40 days
        for (int i = 0; i < 3; i++)
        {
            int sp = FibSP[rng.Next(FibSP.Length)];
            var labels = LabelPool[rng.Next(LabelPool.Length)];
            var dueDate = now.AddDays(rng.Next(10, 41));
            tasks.Add(Make(userId, pool[idx][0], pool[idx][1],
                TaskStatus.Pending, sp, labels,
                now.AddDays(-rng.Next(1, 7)), dueDate));
            idx++;
        }

        // ToDo (3): due in the next 35–100 days
        for (int i = 0; i < 3; i++)
        {
            int sp = FibSP[rng.Next(FibSP.Length)];
            var labels = LabelPool[rng.Next(LabelPool.Length)];
            var dueDate = now.AddDays(rng.Next(35, 101));
            tasks.Add(Make(userId, pool[idx][0], pool[idx][1],
                TaskStatus.ToDo, sp, labels,
                now.AddDays(-rng.Next(0, 3)), dueDate));
            idx++;
        }

        return tasks;
    }

    private static TaskItem Make(
        string userId, string title, string description,
        TaskStatus status, int? storyPoints, IEnumerable<string> labels,
        DateTime createdAt, DateTime dueDate, DateTime? completedAt = null)
    {
        return new TaskItem
        {
            Title = title,
            Description = description,
            Status = status,
            StoryPoints = storyPoints,
            Labels = labels.ToList(),
            UserId = userId,
            CreatedAt = createdAt,
            UpdatedAt = completedAt ?? createdAt,
            UpdatedBy = completedAt.HasValue ? userId : string.Empty,
            DueDate = dueDate,
            CompletedAt = completedAt,
        };
    }
}
