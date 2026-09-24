using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PinusTickets.Data;
using PinusTickets.DTOs;

namespace PinusTickets.Controllers;

// ── Notification log ──────────────────────────────────────────────────────────
[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController(AppDbContext db) : ControllerBase
{
    // GET /api/v1/notifications  — recent 100 for the log page
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? ticketId, [FromQuery] string? status)
    {
        var q = db.Notifications.AsQueryable();
        if (ticketId.HasValue)           q = q.Where(n => n.TicketId == ticketId);
        if (!string.IsNullOrEmpty(status)) q = q.Where(n => n.Status  == status);
        var result = await q.OrderByDescending(n => n.CreatedAt).Take(100)
            .Select(n => new {
                n.Id, n.EventType, n.TicketId, n.Channel, n.Subject,
                n.Status, n.SentAt, n.ErrorMessage, n.CreatedAt
            }).ToListAsync();
        return Ok(result);
    }

    // GET /api/v1/notifications/stats
    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var total  = await db.Notifications.CountAsync();
        var sent   = await db.Notifications.CountAsync(n => n.Status == "Sent");
        var failed = await db.Notifications.CountAsync(n => n.Status == "Failed");
        var skip   = await db.Notifications.CountAsync(n => n.Status == "Skipped");
        return Ok(new { total, sent, failed, skipped = skip });
    }
}

// ── Ticket Assignment dedicated controller ────────────────────────────────────
[ApiController]
[Route("api/v1/assignments")]
[Authorize(Roles = "Admin,SupportManager,SupportExecutive")]
public class AssignmentsController(AppDbContext db) : ControllerBase
{
    // GET unassigned tickets queue
    [HttpGet("queue")]
    public async Task<IActionResult> Queue()
    {
        var tickets = await db.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Application)
            .Where(t => t.AssigneeId == null && t.Status != "Closed" && t.Status != "Resolved")
            .OrderByDescending(t => t.Priority == "Critical" ? 4
                : t.Priority == "High" ? 3 : t.Priority == "Medium" ? 2 : 1)
            .ThenBy(t => t.CreatedAt)
            .Select(t => new {
                t.Id, t.TicketNo, t.Subject, t.Priority, t.Status, t.Type,
                CustomerName   = t.Customer!.Name,
                ApplicationName= t.Application != null ? t.Application.Name : null,
                t.SlaDueAt, t.CreatedAt
            }).ToListAsync();
        return Ok(tickets);
    }

    // GET engineer workload
    [HttpGet("workload")]
    public async Task<IActionResult> Workload()
    {
        var engineers = await db.Users
            .Where(u => u.Role == "Developer" || u.Role == "SupportExecutive"
                     || u.Role == "SupportManager")
            .Select(u => new {
                u.Id, u.Name, u.Role,
                OpenCount = db.Tickets.Count(t =>
                    t.AssigneeId == u.Id &&
                    t.Status != "Closed" && t.Status != "Resolved"),
                CriticalCount = db.Tickets.Count(t =>
                    t.AssigneeId == u.Id && t.Priority == "Critical" &&
                    t.Status != "Closed" && t.Status != "Resolved"),
                HighCount = db.Tickets.Count(t =>
                    t.AssigneeId == u.Id && t.Priority == "High" &&
                    t.Status != "Closed" && t.Status != "Resolved"),
            }).ToListAsync();
        return Ok(engineers);
    }
}
