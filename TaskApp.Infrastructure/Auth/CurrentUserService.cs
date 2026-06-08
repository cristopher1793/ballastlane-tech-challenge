using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TaskApp.Application.Interfaces;
using TaskApp.Domain.Enums;

namespace TaskApp.Infrastructure.Auth;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserId =>
        _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value
        ?? string.Empty;

    public string Username =>
        _httpContextAccessor.HttpContext?.User.FindFirst("username")?.Value ?? string.Empty;

    public UserRole Role
    {
        get
        {
            string? roleValue = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;
            if (Enum.TryParse<UserRole>(roleValue, out UserRole role))
                return role;
            return UserRole.User;
        }
    }
}
