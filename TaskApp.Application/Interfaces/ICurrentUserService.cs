using TaskApp.Domain.Enums;

namespace TaskApp.Application.Interfaces;

public interface ICurrentUserService
{
    string UserId { get; }
    string Username { get; }
    UserRole Role { get; }
}
