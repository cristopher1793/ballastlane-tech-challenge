using TaskApp.Domain.Entities;

namespace TaskApp.Domain.Interfaces.Repositories;

public interface ITaskRepository
{
    Task<IEnumerable<TaskItem>> GetAllAsync();
    Task<IEnumerable<TaskItem>> GetAllByUserIdAsync(string userId);
    Task<IEnumerable<string>> GetAllLabelsAsync();
    Task<IEnumerable<string>> GetAllLabelsByUserIdAsync(string userId);
    Task<TaskItem?> GetByIdAsync(string id);
    Task<TaskItem> CreateAsync(TaskItem task);
    Task<TaskItem> UpdateAsync(TaskItem task);
    Task DeleteAsync(string id);
}
