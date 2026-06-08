using MongoDB.Driver;
using TaskApp.Domain.Entities;
using TaskApp.Infrastructure.Persistence;
using TaskApp.Infrastructure.Persistence.Repositories;

namespace TaskApp.Tests.Integration;

public class TaskRepositoryTests : IAsyncLifetime
{
    private readonly MongoDbContext _context;
    private readonly TaskRepository _repository;
    private const string UserId = "user_test_001";
    private const string OtherUserId = "user_test_002";

    public TaskRepositoryTests()
    {
        _context = new MongoDbContext("mongodb://localhost:27017", "taskapp_test");
        _repository = new TaskRepository(_context);
    }

    public async Task InitializeAsync()
    {
        await _context.Tasks.DeleteManyAsync(Builders<TaskItem>.Filter.Empty);
    }

    public async Task DisposeAsync()
    {
        await _context.Tasks.DeleteManyAsync(Builders<TaskItem>.Filter.Empty);
    }

    [Fact]
    public async Task CreateAsync_InsertsTaskAndReturnsWithId()
    {
        TaskItem task = BuildTask("Task A", UserId);

        TaskItem result = await _repository.CreateAsync(task);

        Assert.NotEmpty(result.Id);
        Assert.Equal("Task A", result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsInsertedTask()
    {
        TaskItem task = BuildTask("Task B", UserId);
        await _repository.CreateAsync(task);

        TaskItem? found = await _repository.GetByIdAsync(task.Id);

        Assert.NotNull(found);
        Assert.Equal("Task B", found!.Title);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        TaskItem? found = await _repository.GetByIdAsync("000000000000000000000000");

        Assert.Null(found);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        TaskItem task = BuildTask("Original", UserId);
        await _repository.CreateAsync(task);
        task.Title = "Updated";

        await _repository.UpdateAsync(task);
        TaskItem? found = await _repository.GetByIdAsync(task.Id);

        Assert.NotNull(found);
        Assert.Equal("Updated", found!.Title);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTask()
    {
        TaskItem task = BuildTask("To Delete", UserId);
        await _repository.CreateAsync(task);

        await _repository.DeleteAsync(task.Id);
        TaskItem? found = await _repository.GetByIdAsync(task.Id);

        Assert.Null(found);
    }

    [Fact]
    public async Task GetAllByUserIdAsync_ReturnsOnlyUserTasks()
    {
        await _repository.CreateAsync(BuildTask("User task 1", UserId));
        await _repository.CreateAsync(BuildTask("User task 2", UserId));
        await _repository.CreateAsync(BuildTask("Other user task", OtherUserId));

        IEnumerable<TaskItem> results = await _repository.GetAllByUserIdAsync(UserId);

        List<TaskItem> list = results.ToList();
        Assert.Equal(2, list.Count);
        Assert.All(list, t => Assert.Equal(UserId, t.UserId));
    }

    private static TaskItem BuildTask(string title, string userId)
    {
        return new TaskItem
        {
            Title = title,
            Description = "Integration test task",
            Status = TaskApp.Domain.Enums.TaskStatus.Pending,
            DueDate = DateTime.UtcNow.AddDays(5),
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
