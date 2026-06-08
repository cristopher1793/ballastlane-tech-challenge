using Moq;
using TaskApp.Application.DTOs.Auth;
using TaskApp.Application.Interfaces;
using TaskApp.Application.Services;
using TaskApp.Domain.Entities;
using TaskApp.Domain.Enums;
using TaskApp.Domain.Exceptions;
using TaskApp.Domain.Interfaces.Repositories;

namespace TaskApp.Tests.Unit.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepo;
    private readonly Mock<IJwtTokenGenerator> _jwtGenerator;
    private readonly Mock<IPasswordHasher> _passwordHasher;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _userRepo = new Mock<IUserRepository>();
        _jwtGenerator = new Mock<IJwtTokenGenerator>();
        _passwordHasher = new Mock<IPasswordHasher>();
        _sut = new UserService(_userRepo.Object, _jwtGenerator.Object, _passwordHasher.Object);
    }

    // --- Register ---

    [Fact]
    public async Task RegisterAsync_ValidInput_ReturnsUserResponseDto()
    {
        RegisterRequestDto dto = new RegisterRequestDto { FirstName = "Alice", LastName = "Smith", Username = "alice", Email = "alice@example.com", Password = "Password1!" };
        _userRepo.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
        _userRepo.Setup(r => r.GetByUsernameAsync(dto.Username)).ReturnsAsync((User?)null);
        _passwordHasher.Setup(h => h.Hash(dto.Password)).Returns("hashed");
        User created = new User { Id = "id1", FirstName = dto.FirstName, LastName = dto.LastName, Username = dto.Username, Email = dto.Email, PasswordHash = "hashed", Role = UserRole.User, CreatedAt = DateTime.UtcNow };
        _userRepo.Setup(r => r.CreateAsync(It.IsAny<User>())).ReturnsAsync(created);

        UserResponseDto result = await _sut.RegisterAsync(dto);

        Assert.Equal("alice", result.Username);
        Assert.Equal("alice@example.com", result.Email);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsDomainException()
    {
        RegisterRequestDto dto = new RegisterRequestDto { FirstName = "Alice", LastName = "Smith", Username = "alice", Email = "alice@example.com", Password = "Password1!" };
        _userRepo.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(new User());

        await Assert.ThrowsAsync<DomainException>(() => _sut.RegisterAsync(dto));
    }

    [Fact]
    public async Task RegisterAsync_DuplicateUsername_ThrowsDomainException()
    {
        RegisterRequestDto dto = new RegisterRequestDto { FirstName = "Alice", LastName = "Smith", Username = "alice", Email = "alice@example.com", Password = "Password1!" };
        _userRepo.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
        _userRepo.Setup(r => r.GetByUsernameAsync(dto.Username)).ReturnsAsync(new User());

        await Assert.ThrowsAsync<DomainException>(() => _sut.RegisterAsync(dto));
    }

    [Fact]
    public async Task RegisterAsync_WeakPassword_ThrowsDomainException()
    {
        RegisterRequestDto dto = new RegisterRequestDto { Username = "alice", Email = "alice@example.com", Password = "short" };
        _userRepo.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
        _userRepo.Setup(r => r.GetByUsernameAsync(dto.Username)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<DomainException>(() => _sut.RegisterAsync(dto));
    }

    [Fact]
    public async Task RegisterAsync_InvalidEmail_ThrowsDomainException()
    {
        RegisterRequestDto dto = new RegisterRequestDto { Username = "alice", Email = "not-an-email", Password = "Password1!" };
        _userRepo.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
        _userRepo.Setup(r => r.GetByUsernameAsync(dto.Username)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<DomainException>(() => _sut.RegisterAsync(dto));
    }

    // --- Login ---

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsLoginResponseDto()
    {
        LoginRequestDto dto = new LoginRequestDto { Email = "alice@example.com", Password = "Password1!" };
        User user = new User { Id = "id1", Username = "alice", Email = dto.Email, PasswordHash = "hashed", Role = UserRole.User, FailedLoginAttempts = 0 };
        _userRepo.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify(dto.Password, user.PasswordHash)).Returns(true);
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _jwtGenerator.Setup(j => j.GenerateToken(user)).Returns("jwt-token");

        LoginResponseDto result = await _sut.LoginAsync(dto);

        Assert.Equal("jwt-token", result.Token);
        Assert.Equal("alice", result.User.Username);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ResetsFailedAttempts()
    {
        LoginRequestDto dto = new LoginRequestDto { Email = "alice@example.com", Password = "Password1!" };
        User user = new User { Id = "id1", Email = dto.Email, PasswordHash = "hashed", FailedLoginAttempts = 2, Role = UserRole.User };
        _userRepo.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify(dto.Password, user.PasswordHash)).Returns(true);
        User? updated = null;
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .Callback<User>(u => updated = u)
            .ReturnsAsync((User u) => u);
        _jwtGenerator.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("token");

        await _sut.LoginAsync(dto);

        Assert.NotNull(updated);
        Assert.Equal(0, updated!.FailedLoginAttempts);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_IncrementsFailedAttempts()
    {
        LoginRequestDto dto = new LoginRequestDto { Email = "alice@example.com", Password = "wrong" };
        User user = new User { Id = "id1", Email = dto.Email, PasswordHash = "hashed", FailedLoginAttempts = 0, Role = UserRole.User };
        _userRepo.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify(dto.Password, user.PasswordHash)).Returns(false);
        User? updated = null;
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .Callback<User>(u => updated = u)
            .ReturnsAsync((User u) => u);

        await Assert.ThrowsAsync<DomainException>(() => _sut.LoginAsync(dto));

        Assert.NotNull(updated);
        Assert.Equal(1, updated!.FailedLoginAttempts);
    }

    [Fact]
    public async Task LoginAsync_ThreeFailedAttempts_LocksAccount()
    {
        LoginRequestDto dto = new LoginRequestDto { Email = "alice@example.com", Password = "wrong" };
        User user = new User { Id = "id1", Email = dto.Email, PasswordHash = "hashed", FailedLoginAttempts = 2, Role = UserRole.User };
        _userRepo.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify(dto.Password, user.PasswordHash)).Returns(false);
        User? updated = null;
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .Callback<User>(u => updated = u)
            .ReturnsAsync((User u) => u);

        await Assert.ThrowsAsync<DomainException>(() => _sut.LoginAsync(dto));

        Assert.NotNull(updated);
        Assert.Equal(3, updated!.FailedLoginAttempts);
        Assert.True(updated.IsLocked);
        Assert.NotNull(updated.LockedAt);
    }

    [Fact]
    public async Task LoginAsync_LockedAccount_ThrowsDomainException()
    {
        LoginRequestDto dto = new LoginRequestDto { Email = "alice@example.com", Password = "Password1!" };
        User user = new User { Id = "id1", Email = dto.Email, PasswordHash = "hashed", IsLocked = true, Role = UserRole.User };
        _userRepo.Setup(r => r.GetByEmailAsync(dto.Email)).ReturnsAsync(user);

        DomainException ex = await Assert.ThrowsAsync<DomainException>(() => _sut.LoginAsync(dto));
        Assert.Contains("locked", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // --- Unlock ---

    [Fact]
    public async Task UnlockAsync_ResetsLockoutFields()
    {
        User user = new User { Id = "id1", Username = "alice", Email = "a@b.com", IsLocked = true, FailedLoginAttempts = 3, LockedAt = DateTime.UtcNow, Role = UserRole.User };
        _userRepo.Setup(r => r.GetByIdAsync("id1")).ReturnsAsync(user);
        User? updated = null;
        _userRepo.Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .Callback<User>(u => updated = u)
            .ReturnsAsync((User u) => u);

        UserResponseDto result = await _sut.UnlockAsync("id1");

        Assert.NotNull(updated);
        Assert.False(updated!.IsLocked);
        Assert.Equal(0, updated.FailedLoginAttempts);
        Assert.Null(updated.LockedAt);
    }

    [Fact]
    public async Task UnlockAsync_UserNotFound_ThrowsDomainException()
    {
        _userRepo.Setup(r => r.GetByIdAsync("notexist")).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<DomainException>(() => _sut.UnlockAsync("notexist"));
    }
}
