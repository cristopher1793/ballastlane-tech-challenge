using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MongoDB.Driver;
using TaskApp.Application.DTOs.Auth;
using TaskApp.Application.DTOs.Tasks;
using TaskApp.Domain.Entities;
using TaskApp.Infrastructure.Persistence;

namespace TaskApp.Tests.Integration;

public class TasksControllerTests : IClassFixture<ApiTestFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly MongoDbContext _context;
    private readonly JsonSerializerOptions _jsonOptions;

    public TasksControllerTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient();
        _context = new MongoDbContext("mongodb://localhost:27017", "taskapp_test");
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task InitializeAsync()
    {
        await _context.Tasks.DeleteManyAsync(Builders<TaskItem>.Filter.Empty);
        await _context.Users.DeleteManyAsync(Builders<User>.Filter.Empty);
    }

    public async Task DisposeAsync()
    {
        await _context.Tasks.DeleteManyAsync(Builders<TaskItem>.Filter.Empty);
        await _context.Users.DeleteManyAsync(Builders<User>.Filter.Empty);
    }

    [Fact]
    public async Task GetAll_WithoutToken_Returns401()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithToken_ReturnsOnlyOwnTasks()
    {
        string token = await RegisterAndLogin("user1tasks", "u1tasks@example.com");
        await RegisterAndLogin("user2tasks", "u2tasks@example.com");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.PostAsJsonAsync("/api/tasks", new CreateTaskDto { Title = "My Task", DueDate = DateTime.UtcNow.AddDays(3) });
        HttpResponseMessage response = await _client.GetAsync("/api/tasks");
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<TaskResponseDto>? tasks = await response.Content.ReadFromJsonAsync<List<TaskResponseDto>>(_jsonOptions);
        Assert.NotNull(tasks);
        Assert.Single(tasks!);
        Assert.Equal("My Task", tasks[0].Title);
    }

    [Fact]
    public async Task CreateTask_ValidDto_Returns201WithTaskResponseDto()
    {
        string token = await RegisterAndLogin("creator", "creator@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        CreateTaskDto dto = new CreateTaskDto { Title = "New Task", Description = "Desc", DueDate = DateTime.UtcNow.AddDays(5) };
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/tasks", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        TaskResponseDto? result = await response.Content.ReadFromJsonAsync<TaskResponseDto>(_jsonOptions);
        Assert.NotNull(result);
        Assert.Equal("New Task", result!.Title);
        Assert.NotEmpty(result.Id);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task UpdateTask_ValidDto_Returns200WithUpdatedTask()
    {
        string token = await RegisterAndLogin("updater", "updater@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        TaskResponseDto? created = await CreateTask(token, "Original");
        Assert.NotNull(created);

        UpdateTaskDto updateDto = new UpdateTaskDto
        {
            Title = "Updated",
            Description = "New desc",
            Status = TaskApp.Domain.Enums.TaskStatus.InProgress,
            DueDate = DateTime.UtcNow.AddDays(7)
        };
        HttpResponseMessage response = await _client.PutAsJsonAsync($"/api/tasks/{created!.Id}", updateDto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        TaskResponseDto? result = await response.Content.ReadFromJsonAsync<TaskResponseDto>(_jsonOptions);
        Assert.NotNull(result);
        Assert.Equal("Updated", result!.Title);
        Assert.Equal(TaskApp.Domain.Enums.TaskStatus.InProgress, result.Status);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task DeleteTask_ValidId_Returns204()
    {
        string token = await RegisterAndLogin("deleter", "deleter@example.com");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        TaskResponseDto? created = await CreateTask(token, "To Delete");
        Assert.NotNull(created);

        HttpResponseMessage response = await _client.DeleteAsync($"/api/tasks/{created!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task GetById_AnotherUsersTask_ReturnsNotFound()
    {
        string token1 = await RegisterAndLogin("owner", "owner@example.com");
        string token2 = await RegisterAndLogin("attacker", "attacker@example.com");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        TaskResponseDto? created = await CreateTask(token1, "Private Task");
        Assert.NotNull(created);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);
        HttpResponseMessage response = await _client.GetAsync($"/api/tasks/{created!.Id}");
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.True(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Forbidden);
    }

    private async Task<string> RegisterAndLogin(string username, string email)
    {
        RegisterRequestDto reg = new RegisterRequestDto { Username = username, Email = email, Password = "Password1!" };
        await _client.PostAsJsonAsync("/api/auth/register", reg);
        LoginRequestDto login = new LoginRequestDto { Email = email, Password = "Password1!" };
        HttpResponseMessage resp = await _client.PostAsJsonAsync("/api/auth/login", login);
        LoginResponseDto? result = await resp.Content.ReadFromJsonAsync<LoginResponseDto>(_jsonOptions);
        return result?.Token ?? string.Empty;
    }

    private async Task<TaskResponseDto?> CreateTask(string token, string title)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        CreateTaskDto dto = new CreateTaskDto { Title = title, DueDate = DateTime.UtcNow.AddDays(3) };
        HttpResponseMessage resp = await _client.PostAsJsonAsync("/api/tasks", dto);
        return await resp.Content.ReadFromJsonAsync<TaskResponseDto>(_jsonOptions);
    }
}
