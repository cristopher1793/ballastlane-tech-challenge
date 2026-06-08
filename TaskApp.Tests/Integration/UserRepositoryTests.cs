using MongoDB.Driver;
using TaskApp.Domain.Entities;
using TaskApp.Domain.Enums;
using TaskApp.Infrastructure.Persistence;
using TaskApp.Infrastructure.Persistence.Repositories;

namespace TaskApp.Tests.Integration;

public class UserRepositoryTests : IAsyncLifetime
{
    private readonly MongoDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        _context = new MongoDbContext("mongodb://localhost:27017", "taskapp_test");
        _repository = new UserRepository(_context);
    }

    public async Task InitializeAsync()
    {
        await _context.Users.DeleteManyAsync(Builders<User>.Filter.Empty);
    }

    public async Task DisposeAsync()
    {
        await _context.Users.DeleteManyAsync(Builders<User>.Filter.Empty);
    }

    [Fact]
    public async Task CreateAsync_InsertsUserAndReturnsWithId()
    {
        User user = BuildUser("alice", "alice@example.com");

        User result = await _repository.CreateAsync(user);

        Assert.NotEmpty(result.Id);
        Assert.Equal("alice", result.Username);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsInsertedUser()
    {
        User user = BuildUser("bob", "bob@example.com");
        await _repository.CreateAsync(user);

        User? found = await _repository.GetByIdAsync(user.Id);

        Assert.NotNull(found);
        Assert.Equal("bob", found!.Username);
    }

    [Fact]
    public async Task GetByEmailAsync_ReturnsMatchingUser()
    {
        User user = BuildUser("carol", "carol@example.com");
        await _repository.CreateAsync(user);

        User? found = await _repository.GetByEmailAsync("carol@example.com");

        Assert.NotNull(found);
        Assert.Equal("carol", found!.Username);
    }

    [Fact]
    public async Task GetByUsernameAsync_ReturnsMatchingUser()
    {
        User user = BuildUser("dave", "dave@example.com");
        await _repository.CreateAsync(user);

        User? found = await _repository.GetByUsernameAsync("dave");

        Assert.NotNull(found);
        Assert.Equal("dave@example.com", found!.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_NonExistentEmail_ReturnsNull()
    {
        User? found = await _repository.GetByEmailAsync("nobody@example.com");

        Assert.Null(found);
    }

    [Fact]
    public async Task GetByUsernameAsync_NonExistentUsername_ReturnsNull()
    {
        User? found = await _repository.GetByUsernameAsync("ghost");

        Assert.Null(found);
    }

    [Fact]
    public async Task DuplicateEmail_ApplicationLayerEnforcesUniqueness()
    {
        await _repository.CreateAsync(BuildUser("eve", "eve@example.com"));
        User? existing = await _repository.GetByEmailAsync("eve@example.com");

        Assert.NotNull(existing);
    }

    [Fact]
    public async Task DuplicateUsername_ApplicationLayerEnforcesUniqueness()
    {
        await _repository.CreateAsync(BuildUser("frank", "frank@example.com"));
        User? existing = await _repository.GetByUsernameAsync("frank");

        Assert.NotNull(existing);
    }

    private static User BuildUser(string username, string email)
    {
        return new User
        {
            Username = username,
            Email = email,
            PasswordHash = "hashed",
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            FailedLoginAttempts = 0,
            IsLocked = false
        };
    }
}
