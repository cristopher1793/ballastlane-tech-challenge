using TaskApp.Application.DTOs.Seed;

namespace TaskApp.Application.Interfaces;

public interface IDemoSeedService
{
    Task<DemoSeedResultDto> SeedForUserAsync(string userId);
}
