using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TaskManagerAPI.Data;
using TaskManagerAPI.Models;
using System.Security.Claims;

namespace TaskManagerAPI.Controllers;

[ApiController, Route("api/projects/{projectId}/tasks"), Authorize]
public class TasksController(AppDbContext db) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll(int projectId, [FromQuery] string? status, [FromQuery] string? priority)
    {
        var query = db.Tasks.Include(t => t.Assignee)
            .Where(t => t.ProjectId == projectId);
        if (!string.IsNullOrEmpty(status)) query = query.Where(t => t.Status == status);
        if (!string.IsNullOrEmpty(priority)) query = query.Where(t => t.Priority == priority);
        return Ok(await query.OrderBy(t => t.DueDate).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create(int projectId, [FromBody] CreateTaskRequest req)
    {
        var task = new TaskItem {
            Title = req.Title, Description = req.Description, Priority = req.Priority,
            DueDate = req.DueDate, ProjectId = projectId, AssigneeId = req.AssigneeId
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { projectId, id = task.Id }, task);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int projectId, int id)
    {
        var task = await db.Tasks.Include(t => t.Comments).ThenInclude(c => c.Author)
            .Include(t => t.Assignee)
            .FirstOrDefaultAsync(t => t.Id == id && t.ProjectId == projectId);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int projectId, int id, [FromBody] UpdateTaskRequest req)
    {
        var task = await db.Tasks.FindAsync(id);
        if (task is null || task.ProjectId != projectId) return NotFound();
        if (req.Title is not null) task.Title = req.Title;
        if (req.Status is not null) task.Status = req.Status;
        if (req.Priority is not null) task.Priority = req.Priority;
        if (req.AssigneeId.HasValue) task.AssigneeId = req.AssigneeId;
        await db.SaveChangesAsync();
        return Ok(task);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int projectId, int id)
    {
        var task = await db.Tasks.FindAsync(id);
        if (task is null || task.ProjectId != projectId) return NotFound();
        db.Tasks.Remove(task);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/comments")]
    public async Task<IActionResult> AddComment(int projectId, int id, [FromBody] AddCommentRequest req)
    {
        var comment = new Comment { Content = req.Content, TaskId = id, AuthorId = UserId };
        db.Comments.Add(comment);
        await db.SaveChangesAsync();
        return Ok(comment);
    }
}

public record CreateTaskRequest(string Title, string Description, string Priority, DateTime? DueDate, int? AssigneeId);
public record UpdateTaskRequest(string? Title, string? Status, string? Priority, int? AssigneeId);
public record AddCommentRequest(string Content);
