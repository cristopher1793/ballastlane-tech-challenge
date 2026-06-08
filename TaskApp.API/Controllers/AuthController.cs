using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskApp.Application.DTOs.Auth;
using TaskApp.Application.Interfaces;
using TaskApp.Domain.Enums;
using TaskApp.Domain.Exceptions;

namespace TaskApp.API.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth-policy")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(IUserService userService, ICurrentUserService currentUserService)
    {
        _userService = userService;
        _currentUserService = currentUserService;
    }

    [HttpGet("ping")]
    [AllowAnonymous]
    public IActionResult Ping()
    {
        return Ok(new { status = "ok" });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<UserResponseDto>> Register([FromBody] RegisterRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            UserResponseDto result = await _userService.RegisterAsync(dto);
            return CreatedAtAction(nameof(Me), result);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            LoginResponseDto result = await _userService.LoginAsync(dto);
            return Ok(result);
        }
        catch (DomainException ex)
        {
            if (ex.Message.Contains("locked", StringComparison.OrdinalIgnoreCase))
                return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
            return Unauthorized(new { error = ex.Message });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserResponseDto>> Me()
    {
        try
        {
            UserResponseDto result = await _userService.GetByIdAsync(_currentUserService.UserId);
            return Ok(result);
        }
        catch (DomainException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<UserResponseDto>> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            UserResponseDto result = await _userService.UpdateProfileAsync(_currentUserService.UserId, dto);
            return Ok(result);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetAllUsers()
    {
        IEnumerable<UserResponseDto> users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpPost("lock/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserResponseDto>> Lock(string userId)
    {
        try
        {
            UserResponseDto result = await _userService.LockAsync(userId);
            return Ok(result);
        }
        catch (DomainException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("unlock/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<UserResponseDto>> Unlock(string userId)
    {
        try
        {
            UserResponseDto result = await _userService.UnlockAsync(userId);
            return Ok(result);
        }
        catch (DomainException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
