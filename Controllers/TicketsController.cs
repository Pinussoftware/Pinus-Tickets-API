using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PinusTickets.DTOs;
using PinusTickets.Services;

namespace PinusTickets.Controllers;

[ApiController]
[Route("api/v1/tickets")]
[Authorize]
public class TicketsController(TicketService svc) : ControllerBase
{
    private int    CurrentUserId   => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string CurrentRole     => User.FindFirstValue(ClaimTypes.Role) ?? "";
    private int    CurrentOrgId    => int.Parse(User.FindFirstValue("org_id") ?? "0");
    private int?   CurrentCustomerId => int.TryParse(User.FindFirstValue("customer_id"), out var cid) && cid > 0 ? cid : null;
    private bool   IsCustomerRole  => CurrentRole is "CustomerAdmin" or "CustomerUser";

    // GET /api/v1/tickets
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status, [FromQuery] string? priority,
        [FromQuery] int? customerId, [FromQuery] int? assigneeId)
    {
        // Customer roles: scope to their specific customer only
        int? scopedCustomerId = IsCustomerRole ? CurrentCustomerId : customerId;
        return Ok(await svc.GetListAsync(status, priority, scopedCustomerId, assigneeId));
    }

    // GET /api/v1/tickets/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        try
        {
            var ticket = await svc.GetDetailAsync(id);
            // Customer roles: block access to other customers' tickets
            if (IsCustomerRole && ticket.CustomerId != CurrentCustomerId)
                return Forbid();
            return Ok(ticket);
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // POST /api/v1/tickets
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTicketRequest req)
    {
        // Customer roles can only create tickets for their own org
        if (IsCustomerRole && req.CustomerId != CurrentOrgId)
            return Forbid();
        var ticket = await svc.CreateAsync(req, CurrentUserId);
        return CreatedAtAction(nameof(Get), new { id = ticket.Id }, ticket);
    }

    // POST /api/v1/tickets/{id}/transition
    [HttpPost("{id:int}/transition")]
    public async Task<IActionResult> Transition(int id, [FromBody] TransitionRequest req)
    {
        try   { return Ok(await svc.TransitionAsync(id, req, CurrentUserId)); }
        catch (KeyNotFoundException)      { return NotFound(); }
        catch (InvalidOperationException ex) { return UnprocessableEntity(new { message = ex.Message }); }
    }

    // POST /api/v1/tickets/{id}/assign
    [HttpPost("{id:int}/assign")]
    [Authorize(Roles = "Admin,SupportManager,SupportExecutive")]
    public async Task<IActionResult> Assign(int id, [FromBody] AssignRequest req)
    {
        try   { return Ok(await svc.AssignAsync(id, req, CurrentUserId)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    // POST /api/v1/tickets/{id}/comments
    [HttpPost("{id:int}/comments")]
    public async Task<IActionResult> AddComment(int id, [FromBody] AddCommentRequest req)
    {
        // Customer roles: block internal notes
        if (IsCustomerRole && req.Visibility == "internal")
            return Forbid();
        return Ok(await svc.AddCommentAsync(id, req, CurrentUserId));
    }

    // POST /api/v1/tickets/{id}/time-entries
    [HttpPost("{id:int}/time-entries")]
    [Authorize(Roles = "Admin,Developer,SupportExecutive")]
    public async Task<IActionResult> AddTimeEntry(int id, [FromBody] AddTimeEntryRequest req)
        => Ok(await svc.AddTimeEntryAsync(id, req, CurrentUserId));

    // POST /api/v1/tickets/{id}/test-results
    [HttpPost("{id:int}/test-results")]
    [Authorize(Roles = "Admin,QA")]
    public async Task<IActionResult> AddTestResult(int id, [FromBody] AddTestResultRequest req)
        => Ok(await svc.AddTestResultAsync(id, req, CurrentUserId));

    // GET /api/v1/tickets/dashboard
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        // Customer roles: scoped stats for their org only
        int? scopedCustomerId = IsCustomerRole ? CurrentOrgId : null;
        return Ok(await svc.GetStatsAsync(scopedCustomerId));
    }
}
