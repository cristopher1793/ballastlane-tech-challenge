using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskApp.Application.DTOs.Seed;
using TaskApp.Application.Interfaces;

namespace TaskApp.API.Controllers;

[ApiController]
[Route("api/seed")]
[Authorize]
public class SeedController(IDemoSeedService demoSeedService, ICurrentUserService currentUserService) : ControllerBase
{
    [HttpPost("me")]
    public async Task<ActionResult<DemoSeedResultDto>> SeedMe()
    {
        string userId = currentUserService.UserId
            ?? throw new InvalidOperationException("User ID not found in token.");

        DemoSeedResultDto result = await demoSeedService.SeedForUserAsync(userId);
        return Ok(result);
    }
}
