using MongoDB.Driver;
using TaskApp.Domain.Entities;
using TaskApp.Domain.Enums;
using TaskApp.Infrastructure.Auth;
using TaskApp.Infrastructure.Persistence;

namespace TaskApp.Infrastructure.Seeder;

public class DatabaseSeeder
{
    private readonly MongoDbContext _context;
    private readonly PasswordHasher _passwordHasher;

    public DatabaseSeeder(MongoDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher();
    }

    public async Task SeedAsync()
    {
        await SeedUsersAsync();
        await SeedTasksAsync();
    }

    private async Task SeedUsersAsync()
    {
        long userCount = await _context.Users.CountDocumentsAsync(Builders<User>.Filter.Empty);
        if (userCount > 0) return;

        List<User> users = new List<User>
        {
            new User
            {
                FirstName = "Demo",
                LastName = "User",
                Username = "demo",
                Email = "demo@taskapp.com",
                PasswordHash = _passwordHasher.Hash("Demo1234!"),
                Role = UserRole.User,
                CreatedAt = DateTime.UtcNow,
                FailedLoginAttempts = 0,
                IsLocked = false
            },
            new User
            {
                FirstName = "Admin",
                LastName = "User",
                Username = "admin",
                Email = "admin@taskapp.com",
                PasswordHash = _passwordHasher.Hash("Admin1234!"),
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow,
                FailedLoginAttempts = 0,
                IsLocked = false
            }
        };

        await _context.Users.InsertManyAsync(users);
    }

    private async Task SeedTasksAsync()
    {
        long taskCount = await _context.Tasks.CountDocumentsAsync(Builders<TaskItem>.Filter.Empty);
        if (taskCount > 0) return;

        User? demoUser = await _context.Users
            .Find(Builders<User>.Filter.Eq(u => u.Username, "demo"))
            .FirstOrDefaultAsync();

        if (demoUser is null) return;

        DateTime now = DateTime.UtcNow;
        List<TaskItem> tasks = new List<TaskItem>
        {
            new TaskItem
            {
                Title = "Set up project environment",
                Description = "Install dependencies and configure development tools",
                Status = TaskApp.Domain.Enums.TaskStatus.Completed,
                DueDate = now.AddDays(-2),
                UserId = demoUser.Id,
                CreatedAt = now.AddDays(-5),
                UpdatedAt = now.AddDays(-2)
            },
            new TaskItem
            {
                Title = "Design database schema",
                Description = "Define MongoDB collections and document structures",
                Status = TaskApp.Domain.Enums.TaskStatus.Completed,
                DueDate = now.AddDays(-1),
                UserId = demoUser.Id,
                CreatedAt = now.AddDays(-4),
                UpdatedAt = now.AddDays(-1)
            },
            new TaskItem
            {
                Title = "Implement authentication",
                Description = "Build JWT-based login and registration endpoints",
                Status = TaskApp.Domain.Enums.TaskStatus.InProgress,
                DueDate = now.AddDays(1),
                UserId = demoUser.Id,
                CreatedAt = now.AddDays(-3),
                UpdatedAt = now
            },
            new TaskItem
            {
                Title = "Build task management API",
                Description = "Create CRUD endpoints for task management",
                Status = TaskApp.Domain.Enums.TaskStatus.Pending,
                DueDate = now.AddDays(3),
                UserId = demoUser.Id,
                CreatedAt = now.AddDays(-2),
                UpdatedAt = now.AddDays(-2)
            },
            new TaskItem
            {
                Title = "Write integration tests",
                Description = "Cover all API endpoints with xUnit integration tests",
                Status = TaskApp.Domain.Enums.TaskStatus.Pending,
                DueDate = now.AddDays(5),
                UserId = demoUser.Id,
                CreatedAt = now.AddDays(-1),
                UpdatedAt = now.AddDays(-1)
            }
        };

        await _context.Tasks.InsertManyAsync(tasks);
    }
}
