using TaskApp.Domain.Entities;

namespace TaskApp.Domain.Interfaces.Repositories;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByIdAsync(string id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
}
