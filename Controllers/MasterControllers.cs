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
    [HttpGet]
    public async Task<IActionResult> List()
        => Ok(await db.Customers
            .Select(c => new CustomerDto(c.Id, c.Name, c.AccountCode, c.Status, c.OrganizationId))
            .ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var c = await db.Customers.FindAsync(id);
        return c == null ? NotFound() :
            Ok(new CustomerDto(c.Id, c.Name, c.AccountCode, c.Status, c.OrganizationId));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SupportManager")]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest req)
    {
        var customer = new Customer
        {
            Name = req.Name, AccountCode = req.AccountCode,
            OrganizationId = req.OrganizationId,
        };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = customer.Id },
            new CustomerDto(customer.Id, customer.Name, customer.AccountCode,
                            customer.Status, customer.OrganizationId));
    }

    [HttpPatch("{id:int}")]
    [Authorize(Roles = "Admin,SupportManager")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateCustomerRequest req)
    {
        var c = await db.Customers.FindAsync(id);
        if (c == null) return NotFound();
        c.Name = req.Name; c.AccountCode = req.AccountCode;
        await db.SaveChangesAsync();
        return Ok(new CustomerDto(c.Id, c.Name, c.AccountCode, c.Status, c.OrganizationId));
    }
}

// ── Applications ──────────────────────────────────────────────────────────────
[ApiController]
[Route("api/v1/applications")]
[Authorize]
public class ApplicationsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? customerId, [FromQuery] string? status)
    {
        var q = db.Applications.Include(a => a.Customer).AsQueryable();
        if (customerId.HasValue) q = q.Where(a => a.CustomerId == customerId);
        if (!string.IsNullOrEmpty(status)) q = q.Where(a => a.Status == status);
        return Ok(await q.OrderBy(a => a.Customer!.Name).ThenBy(a => a.Name)
            .Select(a => new
            {
                a.Id, a.Name, a.Version, a.Technology, a.Status, a.CustomerId,
                CustomerName = a.Customer!.Name,
                a.OwnerUserId
            })
            .ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var a = await db.Applications.Include(a => a.Customer).FirstOrDefaultAsync(a => a.Id == id);
        if (a == null) return NotFound();
        return Ok(new { a.Id, a.Name, a.Version, a.Technology, a.Status,
                         a.CustomerId, CustomerName = a.Customer?.Name });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SupportManager")]
    public async Task<IActionResult> Create([FromBody] CreateApplicationRequest req)
    {
        var app = new Application
        {
            Name = req.Name, CustomerId = req.CustomerId,
            Version = req.Version, Technology = req.Technology,
        };
        db.Applications.Add(app);
        await db.SaveChangesAsync();
        var customer = await db.Customers.FindAsync(req.CustomerId);
        return Ok(new { app.Id, app.Name, app.Version, app.Technology,
                         app.Status, app.CustomerId, CustomerName = customer?.Name });
    }

    [HttpPatch("{id:int}")]
    [Authorize(Roles = "Admin,SupportManager")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateApplicationRequest req)
    {
        var app = await db.Applications.FindAsync(id);
        if (app == null) return NotFound();
        app.Name = req.Name; app.Version = req.Version; app.Technology = req.Technology;
        await db.SaveChangesAsync();
        return Ok(new { app.Id, app.Name, app.Version, app.Technology, app.Status, app.CustomerId });
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

// ── Users ─────────────────────────────────────────────────────────────────────
[ApiController]
[Route("api/v1/users")]
[Authorize(Roles = "Admin")]
public class UsersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List()
        => Ok(await db.Users
            .Select(u => new UserDto(u.Id, u.Name, u.Email, u.Role, u.Status))
            .ToListAsync());

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
        return Ok(new UserDto(user.Id, user.Name, user.Email, user.Role, user.Status));
    }
}
