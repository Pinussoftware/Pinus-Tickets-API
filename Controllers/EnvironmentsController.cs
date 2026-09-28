using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PinusTickets.Data;

namespace PinusTickets.Controllers;

[ApiController]
[Route("api/v1/environments")]
[Authorize]
public class EnvironmentsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? all)
    {
        var q = db.Environments.AsQueryable();
        if (all != true) q = q.Where(e => e.IsActive);
        return Ok(await q.OrderBy(e => e.Name)
            .Select(e => new { e.Id, e.Name, e.Description, e.Icon, e.IsActive, e.CreatedAt })
            .ToListAsync());
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SupportManager")]
    public async Task<IActionResult> Create([FromBody] EnvRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required" });
        if (await db.Environments.AnyAsync(e => e.Name.ToLower() == req.Name.ToLower()))
            return Conflict(new { message = "Environment already exists" });
        var env = new Models.Environment { Name = req.Name.Trim(), Description = req.Description,
                                           Icon = req.Icon ?? "🌐", IsActive = true };
        db.Environments.Add(env);
        await db.SaveChangesAsync();
        return Ok(env);
    }

    [HttpPatch("{id:int}")]
    [Authorize(Roles = "Admin,SupportManager")]
    public async Task<IActionResult> Update(int id, [FromBody] EnvRequest req)
    {
        var env = await db.Environments.FindAsync(id);
        if (env == null) return NotFound();
        if (!string.IsNullOrWhiteSpace(req.Name)) env.Name = req.Name.Trim();
        if (req.Description != null) env.Description = req.Description;
        if (!string.IsNullOrWhiteSpace(req.Icon)) env.Icon = req.Icon;
        if (req.IsActive.HasValue) env.IsActive = req.IsActive.Value;
        await db.SaveChangesAsync();
        return Ok(env);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var env = await db.Environments.FindAsync(id);
        if (env == null) return NotFound();
        db.Environments.Remove(env);
        await db.SaveChangesAsync();
        return Ok(new { message = "Deleted" });
    }
}

public record EnvRequest(string? Name, string? Description, string? Icon, bool? IsActive);
