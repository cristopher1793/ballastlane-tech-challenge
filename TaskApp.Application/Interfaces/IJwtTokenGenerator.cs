using TaskApp.Domain.Entities;

namespace TaskApp.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
