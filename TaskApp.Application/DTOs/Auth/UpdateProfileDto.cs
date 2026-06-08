namespace TaskApp.Application.DTOs.Auth;

public class UpdateProfileDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? CurrentPassword { get; set; }
    public string? NewPassword { get; set; }
}
