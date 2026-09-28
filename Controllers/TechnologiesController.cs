using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PinusTickets.Data;
using PinusTickets.Models;

namespace PinusTickets.Controllers;

[ApiController]
[Route("api/v1/technologies")]
[Authorize]
public class TechnologiesController(AppDbContext db) : ControllerBase
{
    // GET /api/v1/technologies — all active (for dropdowns)
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? all)
    {
        var q = db.Technologies.AsQueryable();
        if (all != true) q = q.Where(t => t.IsActive);
        return Ok(await q.OrderBy(t => t.Category).ThenBy(t => t.Name)
            .Select(t => new { t.Id, t.Name, t.Category, t.Icon, t.IsActive, t.CreatedAt })
            .ToListAsync());
    }

    // POST /api/v1/technologies — create
    [HttpPost]
    [Authorize(Roles = "Admin,SupportManager")]
    public async Task<IActionResult> Create([FromBody] TechRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required" });
        if (await db.Technologies.AnyAsync(t => t.Name.ToLower() == req.Name.ToLower()))
            return Conflict(new { message = "Technology already exists" });
        var tech = new Technology { Name = req.Name.Trim(), Category = req.Category ?? "Technology",
                                    Icon = req.Icon ?? "⚙️", IsActive = true };
        db.Technologies.Add(tech);
        await db.SaveChangesAsync();
        return Ok(tech);
    }

    // PATCH /api/v1/technologies/{id}
    [HttpPatch("{id:int}")]
    [Authorize(Roles = "Admin,SupportManager")]
    public async Task<IActionResult> Update(int id, [FromBody] TechRequest req)
    {
        var tech = await db.Technologies.FindAsync(id);
        if (tech == null) return NotFound();
        if (!string.IsNullOrWhiteSpace(req.Name)) tech.Name = req.Name.Trim();
        if (!string.IsNullOrWhiteSpace(req.Category)) tech.Category = req.Category;
        if (!string.IsNullOrWhiteSpace(req.Icon)) tech.Icon = req.Icon;
        if (req.IsActive.HasValue) tech.IsActive = req.IsActive.Value;
        await db.SaveChangesAsync();
        return Ok(tech);
    }

    // DELETE /api/v1/technologies/{id}
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var tech = await db.Technologies.FindAsync(id);
        if (tech == null) return NotFound();
        db.Technologies.Remove(tech);
        await db.SaveChangesAsync();
        return Ok(new { message = "Deleted" });
    }
}

public record TechRequest(string? Name, string? Category, string? Icon, bool? IsActive);
