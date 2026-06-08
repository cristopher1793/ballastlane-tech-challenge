using System.Text.RegularExpressions;
using TaskApp.Application.DTOs.Auth;
using TaskApp.Application.Interfaces;
using TaskApp.Domain.Entities;
using TaskApp.Domain.Enums;
using TaskApp.Domain.Exceptions;
using TaskApp.Domain.Interfaces.Repositories;

namespace TaskApp.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private const int MaxFailedAttempts = 3;

    public UserService(
        IUserRepository userRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _passwordHasher = passwordHasher;
    }

    public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync()
    {
        IEnumerable<User> users = await _userRepository.GetAllAsync();
        return users.OrderBy(u => u.Username).Select(MapToDto);
    }

    public async Task<UserResponseDto> RegisterAsync(RegisterRequestDto dto)
    {
        ValidatePassword(dto.Password);
        ValidateEmail(dto.Email);

        User? existingByEmail = await _userRepository.GetByEmailAsync(dto.Email);
        if (existingByEmail is not null)
            throw new DomainException("Email is already in use.");

        User? existingByUsername = await _userRepository.GetByUsernameAsync(dto.Username);
        if (existingByUsername is not null)
            throw new DomainException("Username is already taken.");

        if (string.IsNullOrWhiteSpace(dto.FirstName))
            throw new DomainException("First name is required.");
        if (string.IsNullOrWhiteSpace(dto.LastName))
            throw new DomainException("Last name is required.");

        User user = new User
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            FailedLoginAttempts = 0,
            IsLocked = false
        };

        User created = await _userRepository.CreateAsync(user);
        return MapToDto(created);
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto)
    {
        User? user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user is null)
            throw new DomainException("Invalid credentials.");

        if (user.IsLocked)
            throw new DomainException("Account is locked due to too many failed attempts. Please contact an administrator.");

        bool passwordValid = _passwordHasher.Verify(dto.Password, user.PasswordHash);
        if (!passwordValid)
        {
            user.FailedLoginAttempts += 1;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.IsLocked = true;
                user.LockedAt = DateTime.UtcNow;
            }
            await _userRepository.UpdateAsync(user);
            throw new DomainException("Invalid credentials.");
        }

        user.FailedLoginAttempts = 0;
        await _userRepository.UpdateAsync(user);

        string token = _jwtTokenGenerator.GenerateToken(user);
        return new LoginResponseDto { Token = token, User = MapToDto(user) };
    }

    public async Task<UserResponseDto> GetByIdAsync(string id)
    {
        User? user = await _userRepository.GetByIdAsync(id);
        if (user is null)
            throw new DomainException("User not found.");
        return MapToDto(user);
    }

    public async Task<UserResponseDto> LockAsync(string userId)
    {
        User? user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            throw new DomainException("User not found.");

        user.IsLocked = true;
        user.LockedAt = DateTime.UtcNow;

        User updated = await _userRepository.UpdateAsync(user);
        return MapToDto(updated);
    }

    public async Task<UserResponseDto> UnlockAsync(string userId)
    {
        User? user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            throw new DomainException("User not found.");

        user.IsLocked = false;
        user.FailedLoginAttempts = 0;
        user.LockedAt = null;

        User updated = await _userRepository.UpdateAsync(user);
        return MapToDto(updated);
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
            throw new DomainException("Password must be at least 8 characters.");
    }

    private static void ValidateEmail(string email)
    {
        Regex emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
        if (!emailRegex.IsMatch(email))
            throw new DomainException("Invalid email format.");
    }

    public async Task<UserResponseDto> UpdateProfileAsync(string userId, UpdateProfileDto dto)
    {
        User? user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            throw new DomainException("User not found.");

        if (string.IsNullOrWhiteSpace(dto.FirstName))
            throw new DomainException("First name is required.");
        if (string.IsNullOrWhiteSpace(dto.LastName))
            throw new DomainException("Last name is required.");

        ValidateEmail(dto.Email);

        if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
        {
            User? existingByEmail = await _userRepository.GetByEmailAsync(dto.Email);
            if (existingByEmail is not null)
                throw new DomainException("Email is already in use.");
        }

        if (!string.Equals(user.Username, dto.Username, StringComparison.OrdinalIgnoreCase))
        {
            User? existingByUsername = await _userRepository.GetByUsernameAsync(dto.Username);
            if (existingByUsername is not null)
                throw new DomainException("Username is already taken.");
        }

        if (!string.IsNullOrEmpty(dto.NewPassword))
        {
            if (string.IsNullOrEmpty(dto.CurrentPassword))
                throw new DomainException("Current password is required to set a new password.");
            if (!_passwordHasher.Verify(dto.CurrentPassword, user.PasswordHash))
                throw new DomainException("Current password is incorrect.");
            ValidatePassword(dto.NewPassword);
            user.PasswordHash = _passwordHasher.Hash(dto.NewPassword);
        }

        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.Username = dto.Username;
        user.Email = dto.Email;

        User updated = await _userRepository.UpdateAsync(user);
        return MapToDto(updated);
    }

    private static UserResponseDto MapToDto(User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            IsLocked = user.IsLocked,
            FailedLoginAttempts = user.FailedLoginAttempts
        };
    }
}
