using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TaskManagerAPI.Data;
using TaskManagerAPI.Models;
using System.Security.Claims;

namespace TaskManagerAPI.Controllers;

[ApiController, Route("api/[controller]"), Authorize]
public class ProjectsController(AppDbContext db) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var projects = await db.Projects
            .Include(p => p.Members).ThenInclude(m => m.User)
            .Where(p => p.OwnerId == UserId || p.Members.Any(m => m.UserId == UserId))
            .Select(p => new {
                p.Id, p.Name, p.Description, p.Status, p.DueDate, p.CreatedAt,
                Owner = p.Owner.Name,
                TaskCount = p.Tasks.Count,
                MemberCount = p.Members.Count
            }).ToListAsync();
        return Ok(projects);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var project = await db.Projects
            .Include(p => p.Tasks).ThenInclude(t => t.Assignee)
            .Include(p => p.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (project is null) return NotFound();
        return Ok(project);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest req)
    {
        var project = new Project {
            Name = req.Name, Description = req.Description,
            DueDate = req.DueDate, OwnerId = UserId
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectRequest req)
    {
        var project = await db.Projects.FindAsync(id);
        if (project is null || project.OwnerId != UserId) return NotFound();
        project.Name = req.Name ?? project.Name;
        project.Description = req.Description ?? project.Description;
        project.Status = req.Status ?? project.Status;
        project.DueDate = req.DueDate ?? project.DueDate;
        await db.SaveChangesAsync();
        return Ok(project);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var project = await db.Projects.FindAsync(id);
        if (project is null || project.OwnerId != UserId) return NotFound();
        db.Projects.Remove(project);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddMember(int id, [FromBody] AddMemberRequest req)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user is null) return NotFound("User not found");
        db.ProjectMembers.Add(new ProjectMember { ProjectId = id, UserId = user.Id, Role = req.Role });
        await db.SaveChangesAsync();
        return Ok();
    }
}

public record CreateProjectRequest(string Name, string Description, DateTime? DueDate);
public record UpdateProjectRequest(string? Name, string? Description, string? Status, DateTime? DueDate);
public record AddMemberRequest(string Email, string Role);
