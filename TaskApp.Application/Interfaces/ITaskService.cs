using TaskApp.Application.DTOs.Tasks;

namespace TaskApp.Application.Interfaces;

public interface ITaskService
{
    Task<IEnumerable<TaskResponseDto>> GetAllAsync(string userId, bool isAdmin = false);
    Task<IEnumerable<string>> GetAllLabelsAsync(string userId, bool isAdmin = false);
    Task<DashboardStatsDto> GetDashboardStatsAsync(string userId, bool isAdmin = false);
    Task<TaskResponseDto> GetByIdAsync(string id, string userId, bool isAdmin = false);
    Task<TaskResponseDto> CreateAsync(CreateTaskDto dto, string userId);
    Task<TaskResponseDto> UpdateAsync(string id, UpdateTaskDto dto, string userId, bool isAdmin = false);
    Task DeleteAsync(string id, string userId, bool isAdmin = false);
}
