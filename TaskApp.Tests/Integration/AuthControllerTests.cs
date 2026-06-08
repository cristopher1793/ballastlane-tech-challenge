using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MongoDB.Driver;
using TaskApp.Application.DTOs.Auth;
using TaskApp.Domain.Entities;
using TaskApp.Infrastructure.Persistence;

namespace TaskApp.Tests.Integration;

public class AuthControllerTests : IClassFixture<ApiTestFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly MongoDbContext _context;

    public AuthControllerTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient();
        _context = new MongoDbContext("mongodb://localhost:27017", "taskapp_test");
    }

    public async Task InitializeAsync()
    {
        await _context.Users.DeleteManyAsync(Builders<User>.Filter.Empty);
        await _context.Tasks.DeleteManyAsync(Builders<TaskItem>.Filter.Empty);
    }

    public async Task DisposeAsync()
    {
        await _context.Users.DeleteManyAsync(Builders<User>.Filter.Empty);
        await _context.Tasks.DeleteManyAsync(Builders<TaskItem>.Filter.Empty);
    }

    [Fact]
    public async Task Register_ValidUser_Returns201WithUserResponseDto()
    {
        RegisterRequestDto dto = new RegisterRequestDto { Username = "testuser", Email = "test@example.com", Password = "Password1!" };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/register", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        UserResponseDto? result = await response.Content.ReadFromJsonAsync<UserResponseDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.Equal("testuser", result!.Username);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsLoginResponseDtoWithToken()
    {
        await RegisterUser("loginuser", "login@example.com", "Password1!");

        LoginRequestDto dto = new LoginRequestDto { Email = "login@example.com", Password = "Password1!" };
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/login", dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        LoginResponseDto? result = await response.Content.ReadFromJsonAsync<LoginResponseDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.NotEmpty(result!.Token);
        Assert.Equal("loginuser", result.User.Username);
    }

    [Fact]
    public async Task Login_WrongPasswordThreeTimes_LocksAccount()
    {
        await RegisterUser("lockme", "lockme@example.com", "Password1!");
        LoginRequestDto badDto = new LoginRequestDto { Email = "lockme@example.com", Password = "WrongPass!" };

        for (int i = 0; i < 3; i++)
        {
            await _client.PostAsJsonAsync("/api/auth/login", badDto);
        }

        LoginRequestDto goodDto = new LoginRequestDto { Email = "lockme@example.com", Password = "Password1!" };
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/login", goodDto);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_LockedAccount_Returns403WithMessage()
    {
        await RegisterUser("locked2", "locked2@example.com", "Password1!");
        LoginRequestDto badDto = new LoginRequestDto { Email = "locked2@example.com", Password = "WrongPass!" };
        for (int i = 0; i < 3; i++)
            await _client.PostAsJsonAsync("/api/auth/login", badDto);

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequestDto { Email = "locked2@example.com", Password = "Password1!" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        string body = await response.Content.ReadAsStringAsync();
        Assert.Contains("locked", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetMe_WithToken_ReturnsUserResponseDto()
    {
        await RegisterUser("meuser", "me@example.com", "Password1!");
        string token = await LoginAndGetToken("me@example.com", "Password1!");

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        HttpResponseMessage response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        UserResponseDto? result = await response.Content.ReadFromJsonAsync<UserResponseDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.Equal("meuser", result!.Username);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Unlock_AdminRole_Returns200AndResetsLockout()
    {
        await RegisterAdminUser();
        await RegisterUser("victim", "victim@example.com", "Password1!");
        string adminToken = await LoginAndGetToken("admin@taskapp.com", "Admin1234!");

        LoginRequestDto badDto = new LoginRequestDto { Email = "victim@example.com", Password = "WrongPass!" };
        for (int i = 0; i < 3; i++)
            await _client.PostAsJsonAsync("/api/auth/login", badDto);

        User? victim = await _context.Users
            .Find(Builders<User>.Filter.Eq(u => u.Email, "victim@example.com"))
            .FirstOrDefaultAsync();
        Assert.NotNull(victim);
        Assert.True(victim!.IsLocked);

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminToken);
        HttpResponseMessage response = await _client.PostAsync($"/api/auth/unlock/{victim.Id}", null);
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        UserResponseDto? result = await response.Content.ReadFromJsonAsync<UserResponseDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.False(result!.IsLocked);
        Assert.Equal(0, result.FailedLoginAttempts);
    }

    [Fact]
    public async Task Unlock_NonAdminRole_Returns403()
    {
        await RegisterUser("nonAdmin", "nonadmin@example.com", "Password1!");
        await RegisterUser("target", "target@example.com", "Password1!");
        string userToken = await LoginAndGetToken("nonadmin@example.com", "Password1!");

        User? target = await _context.Users
            .Find(Builders<User>.Filter.Eq(u => u.Email, "target@example.com"))
            .FirstOrDefaultAsync();
        Assert.NotNull(target);

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);
        HttpResponseMessage response = await _client.PostAsync($"/api/auth/unlock/{target!.Id}", null);
        _client.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task RegisterUser(string username, string email, string password)
    {
        RegisterRequestDto dto = new RegisterRequestDto { Username = username, Email = email, Password = password };
        await _client.PostAsJsonAsync("/api/auth/register", dto);
    }

    private async Task RegisterAdminUser()
    {
        bool exists = await _context.Users
            .Find(Builders<User>.Filter.Eq(u => u.Email, "admin@taskapp.com"))
            .AnyAsync();
        if (!exists)
        {
            TaskApp.Infrastructure.Auth.PasswordHasher hasher = new TaskApp.Infrastructure.Auth.PasswordHasher();
            User admin = new User
            {
                Username = "admin",
                Email = "admin@taskapp.com",
                PasswordHash = hasher.Hash("Admin1234!"),
                Role = TaskApp.Domain.Enums.UserRole.Admin,
                CreatedAt = DateTime.UtcNow,
                FailedLoginAttempts = 0,
                IsLocked = false
            };
            await _context.Users.InsertOneAsync(admin);
        }
    }

    private async Task<string> LoginAndGetToken(string email, string password)
    {
        LoginRequestDto dto = new LoginRequestDto { Email = email, Password = password };
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/auth/login", dto);
        LoginResponseDto? result = await response.Content.ReadFromJsonAsync<LoginResponseDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return result?.Token ?? string.Empty;
    }
}
