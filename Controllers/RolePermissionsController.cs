using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PinusTickets.Data;
using PinusTickets.Models;

namespace PinusTickets.Controllers;

[ApiController]
[Route("api/v1/role-permissions")]
[Authorize]
public class RolePermissionsController(AppDbContext db) : ControllerBase
{
    static readonly string[] ValidRoles = ["SupportManager","SupportExecutive","CustomerAdmin","CustomerUser"];

    // GET /api/v1/role-permissions/{role}
    [HttpGet("{role}")]
    [Authorize(Roles = "Admin,SupportManager")]
    public IActionResult Get(string role)
    {
        if (!ValidRoles.Contains(role)) return BadRequest(new { message = "Invalid role" });
        var perms = db.RolePermissions.Where(p => p.Role == role)
            .OrderBy(p => p.PageSection).ThenBy(p => p.PageLabel)
            .Select(p => new {
                p.Role, p.PageKey, p.PageLabel, p.PageIcon, p.PageSection,
                p.CanAccess, p.CanCreate, p.CanEdit, p.CanDelete
            }).ToList();
        return Ok(perms);
    }

    // GET /api/v1/role-permissions — all roles summary
    [HttpGet]
    [Authorize(Roles = "Admin,SupportManager")]
    public IActionResult GetAll()
    {
        var perms = db.RolePermissions
            .OrderBy(p => p.Role).ThenBy(p => p.PageSection).ThenBy(p => p.PageLabel)
            .Select(p => new {
                p.Role, p.PageKey, p.PageLabel, p.PageIcon, p.PageSection,
                p.CanAccess, p.CanCreate, p.CanEdit, p.CanDelete
            }).ToList();
        return Ok(perms);
    }

    // POST /api/v1/role-permissions/{role}  — save/upsert all permissions for a role
    [HttpPost("{role}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Save(string role, [FromBody] List<PermissionRequest> items)
    {
        if (!ValidRoles.Contains(role)) return BadRequest(new { message = "Invalid role" });

        foreach (var item in items)
        {
            var existing = db.RolePermissions.FirstOrDefault(p => p.Role == role && p.PageKey == item.PageKey);
            if (existing != null)
            {
                existing.CanAccess  = item.CanAccess;
                existing.CanCreate  = item.CanCreate;
                existing.CanEdit    = item.CanEdit;
                existing.CanDelete  = item.CanDelete;
                existing.UpdatedAt  = DateTime.UtcNow;
            }
            else
            {
                db.RolePermissions.Add(new RolePermission {
                    Role = role, PageKey = item.PageKey,
                    PageLabel = item.PageLabel ?? item.PageKey,
                    PageIcon = item.PageIcon ?? "📄",
                    PageSection = item.PageSection ?? "General",
                    CanAccess = item.CanAccess, CanCreate = item.CanCreate,
                    CanEdit = item.CanEdit, CanDelete = item.CanDelete,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }
        await db.SaveChangesAsync();
        return Ok(new { message = $"Permissions saved for {role}", count = items.Count });
    }
}

public record PermissionRequest(
    string PageKey, string? PageLabel, string? PageIcon, string? PageSection,
    bool CanAccess, bool CanCreate, bool CanEdit, bool CanDelete);
