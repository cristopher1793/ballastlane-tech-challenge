using Moq;
using TaskApp.Application.DTOs.Tasks;
using TaskApp.Application.Interfaces;
using TaskApp.Application.Services;
using TaskApp.Domain.Entities;
using TaskApp.Domain.Exceptions;
using TaskApp.Domain.Interfaces.Repositories;

namespace TaskApp.Tests.Unit.Services;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _taskRepo;
    private readonly Mock<IUserRepository> _userRepo;
    private readonly TaskService _sut;
    private const string UserId = "user123";
    private const string OtherUserId = "other456";

    public TaskServiceTests()
    {
        _taskRepo = new Mock<ITaskRepository>();
        _userRepo = new Mock<IUserRepository>();
        _sut = new TaskService(_taskRepo.Object, _userRepo.Object);
    }

    // --- Create ---

    [Fact]
    public async Task CreateAsync_ValidInput_ReturnsTaskResponseDto()
    {
        DateTime dueDate = DateTime.UtcNow.AddDays(5);
        CreateTaskDto dto = new CreateTaskDto { Title = "Test task", Description = "Desc", DueDate = dueDate };
        TaskItem saved = new TaskItem
        {
            Id = "abc123",
            Title = dto.Title,
            Description = dto.Description,
            DueDate = dueDate,
            UserId = UserId,
            Status = TaskApp.Domain.Enums.TaskStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _taskRepo.Setup(r => r.CreateAsync(It.IsAny<TaskItem>())).ReturnsAsync(saved);

        TaskResponseDto result = await _sut.CreateAsync(dto, UserId);

        Assert.Equal("Test task", result.Title);
        Assert.Equal(UserId, result.UserId);
    }

    [Fact]
    public async Task CreateAsync_EmptyTitle_ThrowsDomainException()
    {
        CreateTaskDto dto = new CreateTaskDto { Title = "", DueDate = DateTime.UtcNow.AddDays(1) };

        await Assert.ThrowsAsync<DomainException>(() => _sut.CreateAsync(dto, UserId));
    }

    [Fact]
    public async Task CreateAsync_TitleExceeds200Chars_ThrowsDomainException()
    {
        CreateTaskDto dto = new CreateTaskDto { Title = new string('A', 201), DueDate = DateTime.UtcNow.AddDays(1) };

        await Assert.ThrowsAsync<DomainException>(() => _sut.CreateAsync(dto, UserId));
    }

    [Fact]
    public async Task CreateAsync_PastDueDate_ThrowsDomainException()
    {
        CreateTaskDto dto = new CreateTaskDto { Title = "Task", DueDate = DateTime.UtcNow.AddDays(-1) };

        await Assert.ThrowsAsync<DomainException>(() => _sut.CreateAsync(dto, UserId));
    }

    [Fact]
    public async Task CreateAsync_AssignsCreatedAtAndUpdatedAt()
    {
        DateTime dueDate = DateTime.UtcNow.AddDays(3);
        CreateTaskDto dto = new CreateTaskDto { Title = "Timestamps", DueDate = dueDate };
        TaskItem? captured = null;
        _taskRepo.Setup(r => r.CreateAsync(It.IsAny<TaskItem>()))
            .Callback<TaskItem>(t => captured = t)
            .ReturnsAsync((TaskItem t) => t);

        await _sut.CreateAsync(dto, UserId);

        Assert.NotNull(captured);
        Assert.NotEqual(default, captured!.CreatedAt);
        Assert.NotEqual(default, captured.UpdatedAt);
    }

    // --- Update ---

    [Fact]
    public async Task UpdateAsync_ValidInput_ReturnsUpdatedDto()
    {
        string taskId = "task1";
        TaskItem existing = new TaskItem { Id = taskId, Title = "Old", UserId = UserId, DueDate = DateTime.UtcNow.AddDays(1) };
        UpdateTaskDto dto = new UpdateTaskDto { Title = "New Title", DueDate = DateTime.UtcNow.AddDays(2), Status = TaskApp.Domain.Enums.TaskStatus.InProgress };
        _taskRepo.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(existing);
        _taskRepo.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>())).ReturnsAsync((TaskItem t) => t);

        TaskResponseDto result = await _sut.UpdateAsync(taskId, dto, UserId);

        Assert.Equal("New Title", result.Title);
        Assert.Equal(TaskApp.Domain.Enums.TaskStatus.InProgress, result.Status);
    }

    [Fact]
    public async Task UpdateAsync_DifferentUserId_ThrowsDomainException()
    {
        string taskId = "task1";
        TaskItem existing = new TaskItem { Id = taskId, Title = "Task", UserId = OtherUserId, DueDate = DateTime.UtcNow.AddDays(1) };
        _taskRepo.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(existing);
        UpdateTaskDto dto = new UpdateTaskDto { Title = "Hacked", DueDate = DateTime.UtcNow.AddDays(1) };

        await Assert.ThrowsAsync<DomainException>(() => _sut.UpdateAsync(taskId, dto, UserId));
    }

    [Fact]
    public async Task UpdateAsync_AssignsUpdatedAt()
    {
        string taskId = "task1";
        TaskItem existing = new TaskItem { Id = taskId, Title = "T", UserId = UserId, DueDate = DateTime.UtcNow.AddDays(1) };
        UpdateTaskDto dto = new UpdateTaskDto { Title = "Updated", DueDate = DateTime.UtcNow.AddDays(2) };
        TaskItem? captured = null;
        _taskRepo.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(existing);
        _taskRepo.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>()))
            .Callback<TaskItem>(t => captured = t)
            .ReturnsAsync((TaskItem t) => t);

        await _sut.UpdateAsync(taskId, dto, UserId);

        Assert.NotNull(captured);
        Assert.NotEqual(default, captured!.UpdatedAt);
    }

    // --- Delete ---

    [Fact]
    public async Task DeleteAsync_OwnerCalling_Succeeds()
    {
        string taskId = "task1";
        TaskItem existing = new TaskItem { Id = taskId, Title = "T", UserId = UserId };
        _taskRepo.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(existing);
        _taskRepo.Setup(r => r.DeleteAsync(taskId)).Returns(Task.CompletedTask);

        await _sut.DeleteAsync(taskId, UserId);

        _taskRepo.Verify(r => r.DeleteAsync(taskId), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_DifferentUserId_ThrowsDomainException()
    {
        string taskId = "task1";
        TaskItem existing = new TaskItem { Id = taskId, Title = "T", UserId = OtherUserId };
        _taskRepo.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(existing);

        await Assert.ThrowsAsync<DomainException>(() => _sut.DeleteAsync(taskId, UserId));
    }

    // --- GetById ---

    [Fact]
    public async Task GetByIdAsync_DifferentUserId_ThrowsDomainException()
    {
        string taskId = "task1";
        TaskItem existing = new TaskItem { Id = taskId, Title = "T", UserId = OtherUserId };
        _taskRepo.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(existing);

        await Assert.ThrowsAsync<DomainException>(() => _sut.GetByIdAsync(taskId, UserId));
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsDomainException()
    {
        _taskRepo.Setup(r => r.GetByIdAsync("notexist")).ReturnsAsync((TaskItem?)null);

        await Assert.ThrowsAsync<DomainException>(() => _sut.GetByIdAsync("notexist", UserId));
    }
}
