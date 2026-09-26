using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PinusTickets.Data;
using PinusTickets.DTOs;
using PinusTickets.Models;

namespace PinusTickets.Controllers;

// ── Customers ─────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/v1/customers")]
[Authorize]
public class CustomersController(AppDbContext db) : ControllerBase
{
    private static CustomerDto ToDto(Customer c) => new(
        c.Id, c.Name, c.AccountCode, c.Status, c.OrganizationId,
        c.Industry, c.ContactPerson, c.Phone, c.Email, c.Website,
        c.Gstin, c.TaxNo, c.SlaPlan, c.SinceYear,
        c.Address, c.City, c.State, c.Pincode, c.Country,
        c.BankName, c.BranchName, c.AccountName, c.AccountNumber,
        c.AccountType, c.IfscCode, c.SwiftCode, c.MicrCode, c.UpiId,
        c.SupportEmail, c.EscalationContact, c.Timezone,
        c.BusinessHours, c.MaxTicketsPerMonth, c.Notes);

    private static void ApplyRequest(Customer c, CreateCustomerRequest req, bool isNew = false)
    {
        c.Name = req.Name; c.AccountCode = req.AccountCode;
        if (isNew) c.OrganizationId = req.OrganizationId > 0 ? req.OrganizationId : 1;
        // Never overwrite OrganizationId on update — keep existing value
        c.Industry = req.Industry; c.ContactPerson = req.ContactPerson;
        c.Phone = req.Phone; c.Email = req.Email; c.Website = req.Website;
        c.Gstin = req.Gstin; c.TaxNo = req.TaxNo; c.SlaPlan = req.SlaPlan;
        c.Status = req.Status ?? c.Status; c.SinceYear = req.SinceYear;
        c.Address = req.Address; c.City = req.City; c.State = req.State;
        c.Pincode = req.Pincode; c.Country = req.Country;
        c.BankName = req.BankName; c.BranchName = req.BranchName;
        c.AccountName = req.AccountName; c.AccountNumber = req.AccountNumber;
        c.AccountType = req.AccountType; c.IfscCode = req.IfscCode;
        c.SwiftCode = req.SwiftCode; c.MicrCode = req.MicrCode; c.UpiId = req.UpiId;
        c.SupportEmail = req.SupportEmail; c.EscalationContact = req.EscalationContact;
        c.Timezone = req.Timezone; c.BusinessHours = req.BusinessHours;
        c.MaxTicketsPerMonth = req.MaxTicketsPerMonth; c.Notes = req.Notes;
    }

    private bool  IsCustomerRole   => User.IsInRole("CustomerAdmin") || User.IsInRole("CustomerUser");
    private int?  CurrentCustomerId => int.TryParse(User.FindFirst("customer_id")?.Value, out var cid) && cid > 0 ? cid : null;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var q = db.Customers.AsQueryable();
        // Customer roles see only their own customer record
        if (IsCustomerRole && CurrentCustomerId.HasValue)
            q = q.Where(c => c.Id == CurrentCustomerId.Value);
        return Ok(await q.Select(c => ToDto(c)).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var c = await db.Customers.FindAsync(id);
        return c == null ? NotFound() : Ok(ToDto(c));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SupportManager")]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest req)
    {
        var customer = new Customer();
        ApplyRequest(customer, req, isNew: true);
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = customer.Id }, ToDto(customer));
    }

    [HttpPatch("{id:int}")]
    [Authorize(Roles = "Admin,SupportManager")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateCustomerRequest req)
    {
        var c = await db.Customers.FindAsync(id);
        if (c == null) return NotFound();
        try
        {
            ApplyRequest(c, req);
            await db.SaveChangesAsync();
            return Ok(ToDto(c));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.InnerException?.Message ?? ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await db.Customers.FindAsync(id);
        if (c == null) return NotFound();
        try
        {
            // Cascade: remove linked tickets (and their children) + contracts
            var ticketIds = db.Tickets.Where(t => t.CustomerId == id).Select(t => t.Id).ToList();
            if (ticketIds.Count > 0)
            {
                db.Notifications.RemoveRange(db.Notifications.Where(n => n.TicketId != null && ticketIds.Contains(n.TicketId!.Value)));
                db.TicketHistory.RemoveRange(db.TicketHistory.Where(h => ticketIds.Contains(h.TicketId)));
                db.TicketComments.RemoveRange(db.TicketComments.Where(h => ticketIds.Contains(h.TicketId)));
                db.TicketAttachments.RemoveRange(db.TicketAttachments.Where(h => ticketIds.Contains(h.TicketId)));
                db.TimeEntries.RemoveRange(db.TimeEntries.Where(h => ticketIds.Contains(h.TicketId)));
                db.TestResults.RemoveRange(db.TestResults.Where(h => ticketIds.Contains(h.TicketId)));
                db.Tickets.RemoveRange(db.Tickets.Where(t => ticketIds.Contains(t.Id)));
            }
            db.Contracts.RemoveRange(db.Contracts.Where(ct => ct.CustomerId == id));
            db.Customers.Remove(c);
            await db.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.InnerException?.Message ?? ex.Message });
        }
    }
}

// ── Applications ──────────────────────────────────────────────────────────────
[ApiController]
[Route("api/v1/applications")]
[Authorize]
public class ApplicationsController(AppDbContext db) : ControllerBase
{
    private static object ToDto(Application a, string? customerName = null) => new {
        a.Id, a.Name, a.Version, a.Technology, a.Status, a.CustomerId,
        CustomerName = customerName ?? a.Customer?.Name,
        a.Description, a.DatabaseTech, a.DeploymentType,
        a.SupportTeam, a.SlaPriority, a.Notes, a.OwnerUserId
    };

    private static void Apply(Application a, CreateApplicationRequest req)
    {
        a.Name = req.Name; a.CustomerId = req.CustomerId;
        a.Version = req.Version; a.Technology = req.Technology;
        a.Status = req.Status ?? a.Status;
        a.Description = req.Description; a.DatabaseTech = req.DatabaseTech;
        a.DeploymentType = req.DeploymentType; a.SupportTeam = req.SupportTeam;
        a.SlaPriority = req.SlaPriority; a.Notes = req.Notes;
    }

    private bool   IsCustomerRole  => User.IsInRole("CustomerAdmin") || User.IsInRole("CustomerUser");
    private int?   CurrentCustomerId => int.TryParse(User.FindFirst("customer_id")?.Value, out var cid) && cid > 0 ? cid : null;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? customerId, [FromQuery] string? status)
    {
        var q = db.Applications.Include(a => a.Customer).AsQueryable();
        // Customer roles: force-scope to their own customer
        var scopedId = IsCustomerRole ? CurrentCustomerId : customerId;
        if (scopedId.HasValue) q = q.Where(a => a.CustomerId == scopedId);
        if (!string.IsNullOrEmpty(status)) q = q.Where(a => a.Status == status);
        return Ok(await q.OrderBy(a => a.Customer!.Name).ThenBy(a => a.Name)
            .Select(a => ToDto(a, a.Customer!.Name)).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var a = await db.Applications.Include(a => a.Customer).FirstOrDefaultAsync(a => a.Id == id);
        return a == null ? NotFound() : Ok(ToDto(a));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SupportManager")]
    public async Task<IActionResult> Create([FromBody] CreateApplicationRequest req)
    {
        var app = new Application(); Apply(app, req);
        db.Applications.Add(app);
        await db.SaveChangesAsync();
        var customer = await db.Customers.FindAsync(req.CustomerId);
        return Ok(ToDto(app, customer?.Name));
    }

    [HttpPatch("{id:int}")]
    [Authorize(Roles = "Admin,SupportManager")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateApplicationRequest req)
    {
        var app = await db.Applications.Include(a => a.Customer).FirstOrDefaultAsync(a => a.Id == id);
        if (app == null) return NotFound();
        Apply(app, req);
        await db.SaveChangesAsync();
        return Ok(ToDto(app));
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin,SupportManager")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] SetStatusRequest req)
    {
        var app = await db.Applications.FindAsync(id);
        if (app == null) return NotFound();
        app.Status = req.Status;
        await db.SaveChangesAsync();
        return Ok(new { app.Id, app.Status });
    }
}

// ── Contracts ─────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/v1/contracts")]
[Authorize]
public class ContractsController(AppDbContext db) : ControllerBase
{
    private static ContractDto ToDto(Contract c) => new(
        c.Id, c.CustomerId, c.Customer?.Name ?? "",
        c.ContractNumber, c.PlanName, c.StartDate, c.EndDate,
        c.Status, c.ResponseHoursCritical, c.ResponseHoursHigh,
        c.ResponseHoursMedium, c.ResponseHoursLow, c.CreatedAt);

    private bool   IsCustomerRole  => User.IsInRole("CustomerAdmin") || User.IsInRole("CustomerUser");
    private int?   CurrentCustomerId => int.TryParse(User.FindFirst("customer_id")?.Value, out var cid) && cid > 0 ? cid : null;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? customerId)
    {
        var q = db.Contracts.Include(c => c.Customer).AsQueryable();
        var scopedId = IsCustomerRole ? CurrentCustomerId : customerId;
        if (scopedId.HasValue) q = q.Where(c => c.CustomerId == scopedId);
        return Ok(await q.OrderByDescending(c => c.CreatedAt).Select(c => ToDto(c)).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var c = await db.Contracts.Include(c => c.Customer).FirstOrDefaultAsync(c => c.Id == id);
        return c == null ? NotFound() : Ok(ToDto(c));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SupportManager")]
    public async Task<IActionResult> Create([FromBody] CreateContractRequest req)
    {
        var c = new Contract
        {
            CustomerId = req.CustomerId, ContractNumber = req.ContractNumber,
            PlanName = req.PlanName,
            StartDate = DateTime.SpecifyKind(req.StartDate, DateTimeKind.Utc),
            EndDate   = DateTime.SpecifyKind(req.EndDate,   DateTimeKind.Utc),
            Status = req.Status ?? "active",
            ResponseHoursCritical = req.ResponseHoursCritical,
            ResponseHoursHigh = req.ResponseHoursHigh,
            ResponseHoursMedium = req.ResponseHoursMedium,
            ResponseHoursLow = req.ResponseHoursLow,
        };
        db.Contracts.Add(c);
        await db.SaveChangesAsync();
        await db.Entry(c).Reference(x => x.Customer).LoadAsync();
        return Ok(ToDto(c));
    }

    [HttpPatch("{id:int}")]
    [Authorize(Roles = "Admin,SupportManager")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateContractRequest req)
    {
        var c = await db.Contracts.Include(x => x.Customer).FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();
        c.ContractNumber = req.ContractNumber; c.PlanName = req.PlanName;
        c.StartDate = DateTime.SpecifyKind(req.StartDate, DateTimeKind.Utc);
        c.EndDate   = DateTime.SpecifyKind(req.EndDate,   DateTimeKind.Utc);
        c.Status = req.Status ?? c.Status;
        c.ResponseHoursCritical = req.ResponseHoursCritical;
        c.ResponseHoursHigh = req.ResponseHoursHigh;
        c.ResponseHoursMedium = req.ResponseHoursMedium;
        c.ResponseHoursLow = req.ResponseHoursLow;
        await db.SaveChangesAsync();
        return Ok(ToDto(c));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,SupportManager")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await db.Contracts.FindAsync(id);
        if (c == null) return NotFound();
        db.Contracts.Remove(c);
        await db.SaveChangesAsync();
        return NoContent();
    }
}

// ── Users ─────────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = "Admin")]
public class UsersController(AppDbContext db) : ControllerBase
{
    private static object ToDto(User u) => new {
        u.Id, u.Name, u.Email, u.Role, u.Status, u.Phone, u.CreatedAt, u.CustomerId
    };

    [HttpGet]
    public async Task<IActionResult> List()
        => Ok(await db.Users.Include(u => u.Customer).Select(u => new {
            u.Id, u.Name, u.Email, u.Role, u.Status, u.Phone, u.CreatedAt, u.CustomerId,
            CustomerName = u.Customer != null ? u.Customer.Name : null
        }).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
    {
        if (await db.Users.AnyAsync(u => u.Email == req.Email))
            return Conflict(new { message = "Email already exists" });
        var user = new User
        {
            Name = req.Name, Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = req.Role, OrganizationId = req.OrganizationId, Phone = req.Phone,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return Ok(ToDto(user));
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest req)
    {
        var u = await db.Users.FindAsync(id);
        if (u == null) return NotFound();
        u.Name  = req.Name  ?? u.Name;
        u.Role  = req.Role  ?? u.Role;
        u.Phone = req.Phone ?? u.Phone;
        u.Status= req.Status?? u.Status;
        if (req.CustomerId.HasValue) u.CustomerId = req.CustomerId == 0 ? null : req.CustomerId;
        if (!string.IsNullOrEmpty(req.Password))
            u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);
        await db.SaveChangesAsync();
        return Ok(ToDto(u));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var u = await db.Users.FindAsync(id);
        if (u == null) return NotFound();
        u.Status = "inactive";
        await db.SaveChangesAsync();
        return Ok(ToDto(u));
    }
}
