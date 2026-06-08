using TaskApp.Application.DTOs.Auth;

namespace TaskApp.Application.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
    Task<UserResponseDto> RegisterAsync(RegisterRequestDto dto);
    Task<LoginResponseDto> LoginAsync(LoginRequestDto dto);
    Task<UserResponseDto> GetByIdAsync(string id);
    Task<UserResponseDto> LockAsync(string userId);
    Task<UserResponseDto> UnlockAsync(string userId);
    Task<UserResponseDto> UpdateProfileAsync(string userId, UpdateProfileDto dto);
}
