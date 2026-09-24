using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PinusTickets.Data;
using PinusTickets.DTOs;
using PinusTickets.Models;

namespace PinusTickets.Controllers;

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
}

[ApiController]
[Route("api/v1/applications")]
[Authorize]
public class ApplicationsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? customerId)
    {
        var q = db.Applications.AsQueryable();
        if (customerId.HasValue) q = q.Where(a => a.CustomerId == customerId);
        return Ok(await q
            .Select(a => new ApplicationDto(a.Id, a.Name, a.Version,
                                            a.Technology, a.Status, a.CustomerId))
            .ToListAsync());
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
        return Ok(new ApplicationDto(app.Id, app.Name, app.Version,
                                     app.Technology, app.Status, app.CustomerId));
    }
}

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
            Name           = req.Name,
            Email          = req.Email,
            PasswordHash   = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role           = req.Role,
            OrganizationId = req.OrganizationId,
            Phone          = req.Phone,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return Ok(new UserDto(user.Id, user.Name, user.Email, user.Role, user.Status));
    }
}
