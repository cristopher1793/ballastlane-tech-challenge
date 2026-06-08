using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskApp.Application.DTOs.Tasks;
using TaskApp.Application.Interfaces;
using TaskApp.Domain.Enums;
using TaskApp.Domain.Exceptions;

namespace TaskApp.API.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ICurrentUserService _currentUserService;

    public TasksController(ITaskService taskService, ICurrentUserService currentUserService)
    {
        _taskService = taskService;
        _currentUserService = currentUserService;
    }

    private bool IsAdmin => _currentUserService.Role == UserRole.Admin;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TaskResponseDto>>> GetAll()
    {
        IEnumerable<TaskResponseDto> tasks = await _taskService.GetAllAsync(_currentUserService.UserId, IsAdmin);
        return Ok(tasks);
    }

    [HttpGet("labels")]
    public async Task<ActionResult<IEnumerable<string>>> GetLabels()
    {
        IEnumerable<string> labels = await _taskService.GetAllLabelsAsync(_currentUserService.UserId, IsAdmin);
        return Ok(labels);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboard()
    {
        DashboardStatsDto stats = await _taskService.GetDashboardStatsAsync(_currentUserService.UserId, IsAdmin);
        return Ok(stats);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TaskResponseDto>> GetById(string id)
    {
        try
        {
            TaskResponseDto task = await _taskService.GetByIdAsync(id, _currentUserService.UserId, IsAdmin);
            return Ok(task);
        }
        catch (DomainException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<TaskResponseDto>> Create([FromBody] CreateTaskDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            TaskResponseDto created = await _taskService.CreateAsync(dto, _currentUserService.UserId);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TaskResponseDto>> Update(string id, [FromBody] UpdateTaskDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            TaskResponseDto updated = await _taskService.UpdateAsync(id, dto, _currentUserService.UserId, IsAdmin);
            return Ok(updated);
        }
        catch (DomainException ex)
        {
            if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = ex.Message });
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        try
        {
            await _taskService.DeleteAsync(id, _currentUserService.UserId, IsAdmin);
            return NoContent();
        }
        catch (DomainException ex)
        {
            if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = ex.Message });
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
    }
}
