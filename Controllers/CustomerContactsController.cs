using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PinusTickets.Data;
using PinusTickets.Models;

namespace PinusTickets.Controllers;

[ApiController]
[Route("api/v1/customers/{customerId:int}/contacts")]
[Authorize]
public class CustomerContactsController(AppDbContext db) : ControllerBase
{
    // GET /api/v1/customers/{id}/contacts
    [HttpGet]
    public async Task<IActionResult> List(int customerId)
    {
        var contacts = await db.CustomerNotifyContacts
            .Where(c => c.CustomerId == customerId)
            .OrderBy(c => c.GroupName).ThenBy(c => c.Name)
            .Select(c => new {
                c.Id, c.Name, c.Email, c.RoleLabel, c.GroupName,
                c.NotifyOnCreate, c.NotifyOnStatus, c.NotifyOnAssign, c.NotifyOnResolve,
                c.IsActive, c.CreatedAt
            }).ToListAsync();
        return Ok(contacts);
    }

    // POST /api/v1/customers/{id}/contacts
    [HttpPost]
    [Authorize(Roles = "Admin,SupportManager")]
    public async Task<IActionResult> Create(int customerId, [FromBody] ContactRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Email))
            return BadRequest(new { message = "Name and Email are required" });

        var contact = new CustomerNotifyContact
        {
            CustomerId     = customerId,
            Name           = req.Name.Trim(),
            Email          = req.Email.Trim().ToLower(),
            RoleLabel      = req.RoleLabel,
            GroupName      = req.GroupName,
            NotifyOnCreate = req.NotifyOnCreate ?? true,
            NotifyOnStatus = req.NotifyOnStatus ?? true,
            NotifyOnAssign = req.NotifyOnAssign ?? false,
            NotifyOnResolve= req.NotifyOnResolve ?? true,
            IsActive       = true
        };
        db.CustomerNotifyContacts.Add(contact);
        await db.SaveChangesAsync();
        return Ok(contact);
    }

    // PATCH /api/v1/customers/{id}/contacts/{contactId}
    [HttpPatch("{contactId:int}")]
    [Authorize(Roles = "Admin,SupportManager")]
    public async Task<IActionResult> Update(int customerId, int contactId, [FromBody] ContactRequest req)
    {
        var c = await db.CustomerNotifyContacts
            .FirstOrDefaultAsync(x => x.Id == contactId && x.CustomerId == customerId);
        if (c == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(req.Name))  c.Name      = req.Name.Trim();
        if (!string.IsNullOrWhiteSpace(req.Email))  c.Email     = req.Email.Trim().ToLower();
        if (req.RoleLabel  != null) c.RoleLabel   = req.RoleLabel;
        if (req.GroupName  != null) c.GroupName   = req.GroupName;
        if (req.NotifyOnCreate  != null) c.NotifyOnCreate  = req.NotifyOnCreate.Value;
        if (req.NotifyOnStatus  != null) c.NotifyOnStatus  = req.NotifyOnStatus.Value;
        if (req.NotifyOnAssign  != null) c.NotifyOnAssign  = req.NotifyOnAssign.Value;
        if (req.NotifyOnResolve != null) c.NotifyOnResolve = req.NotifyOnResolve.Value;
        if (req.IsActive   != null) c.IsActive    = req.IsActive.Value;

        await db.SaveChangesAsync();
        return Ok(c);
    }

    // DELETE /api/v1/customers/{id}/contacts/{contactId}
    [HttpDelete("{contactId:int}")]
    [Authorize(Roles = "Admin,SupportManager")]
    public async Task<IActionResult> Delete(int customerId, int contactId)
    {
        var c = await db.CustomerNotifyContacts
            .FirstOrDefaultAsync(x => x.Id == contactId && x.CustomerId == customerId);
        if (c == null) return NotFound();
        db.CustomerNotifyContacts.Remove(c);
        await db.SaveChangesAsync();
        return Ok(new { message = "Deleted" });
    }
}

public record ContactRequest(
    string? Name, string? Email, string? RoleLabel, string? GroupName,
    bool? NotifyOnCreate, bool? NotifyOnStatus, bool? NotifyOnAssign, bool? NotifyOnResolve,
    bool? IsActive);
