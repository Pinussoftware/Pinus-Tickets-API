using Microsoft.EntityFrameworkCore;
using PinusTickets.Data;
using PinusTickets.DTOs;
using PinusTickets.Models;

namespace PinusTickets.Services;

public class TicketService(AppDbContext db)
{
    // ── Allowed transitions ──────────────────────────────────────────────────
    private static readonly Dictionary<string, string[]> AllowedTransitions = new()
    {
        ["New"]                 = ["Under Review", "Closed"],
        ["Under Review"]        = ["Assigned", "Closed"],
        ["Assigned"]            = ["In Progress", "Under Review"],
        ["In Progress"]         = ["Waiting for Customer", "Ready for QA", "Resolved"],
        ["Waiting for Customer"]= ["In Progress", "Closed"],
        ["Ready for QA"]        = ["Testing"],
        ["Testing"]             = ["Resolved", "In Progress"],
        ["Resolved"]            = ["Closed", "Reopened"],
        ["Closed"]              = ["Reopened"],
        ["Reopened"]            = ["In Progress", "Assigned"],
    };

    // ── Generate ticket number ───────────────────────────────────────────────
    public async Task<string> NextTicketNoAsync()
    {
        var prefix = $"TKT-{DateTime.UtcNow:yyyyMMdd}-";
        var last   = await db.Tickets
            .Where(t => t.TicketNo.StartsWith(prefix))
            .OrderByDescending(t => t.TicketNo)
            .Select(t => t.TicketNo)
            .FirstOrDefaultAsync();
        var seq = last == null ? 1 : int.Parse(last[^4..]) + 1;
        return $"{prefix}{seq:D4}";
    }

    // ── Create ───────────────────────────────────────────────────────────────
    public async Task<TicketDetailDto> CreateAsync(CreateTicketRequest req, int creatorId)
    {
        var ticket = new Ticket
        {
            TicketNo         = await NextTicketNoAsync(),
            CustomerId       = req.CustomerId,
            ApplicationId    = req.ApplicationId,
            Type             = req.Type,
            Category         = req.Category,
            Priority         = req.Priority,
            Status           = "New",
            Subject          = req.Subject,
            Description      = req.Description,
            ReproductionSteps= req.ReproductionSteps,
            ExpectedResult   = req.ExpectedResult,
            ActualResult     = req.ActualResult,
            CreatedBy        = creatorId,
            SlaDueAt         = ComputeSla(req.Priority),
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        await AddHistoryAsync(ticket.Id, creatorId, "created", null, null, "New", null);
        return await GetDetailAsync(ticket.Id);
    }

    // ── Get list ─────────────────────────────────────────────────────────────
    public async Task<IEnumerable<TicketListItem>> GetListAsync(
        string? status, string? priority, int? customerId, int? assigneeId)
    {
        var q = db.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Application)
            .Include(t => t.Assignee)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))   q = q.Where(t => t.Status   == status);
        if (!string.IsNullOrEmpty(priority)) q = q.Where(t => t.Priority == priority);
        if (customerId.HasValue)             q = q.Where(t => t.CustomerId  == customerId);
        if (assigneeId.HasValue)             q = q.Where(t => t.AssigneeId  == assigneeId);

        return await q.OrderByDescending(t => t.UpdatedAt)
            .Select(t => new TicketListItem(
                t.Id, t.TicketNo, t.Subject,
                t.Customer!.Name,
                t.Application != null ? t.Application.Name : null,
                t.Type, t.Priority, t.Status,
                t.Assignee != null ? t.Assignee.Name : null,
                t.SlaDueAt, t.UpdatedAt))
            .ToListAsync();
    }

    // ── Get detail ───────────────────────────────────────────────────────────
    public async Task<TicketDetailDto> GetDetailAsync(int id)
    {
        var t = await db.Tickets
            .Include(t => t.Customer)
            .Include(t => t.Application)
            .Include(t => t.Assignee)
            .Include(t => t.Creator)
            .Include(t => t.Comments).ThenInclude(c => c.Author)
            .Include(t => t.History).ThenInclude(h => h.Actor)
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new KeyNotFoundException($"Ticket {id} not found");

        return MapDetail(t);
    }

    // ── Transition status ────────────────────────────────────────────────────
    public async Task<TicketDetailDto> TransitionAsync(int id, TransitionRequest req, int actorId)
    {
        var ticket = await db.Tickets.FindAsync(id)
            ?? throw new KeyNotFoundException();

        if (!AllowedTransitions.TryGetValue(ticket.Status, out var allowed)
            || !allowed.Contains(req.NewStatus))
            throw new InvalidOperationException(
                $"Transition from '{ticket.Status}' to '{req.NewStatus}' is not allowed.");

        var old = ticket.Status;
        ticket.Status    = req.NewStatus;
        ticket.UpdatedAt = DateTime.UtcNow;
        if (req.NewStatus == "Resolved") ticket.ResolvedAt = DateTime.UtcNow;
        if (req.NewStatus == "Closed")   ticket.ClosedAt   = DateTime.UtcNow;

        await AddHistoryAsync(id, actorId, "status_change", "status", old, req.NewStatus, req.Reason);
        await db.SaveChangesAsync();
        return await GetDetailAsync(id);
    }

    // ── Assign ───────────────────────────────────────────────────────────────
    public async Task<TicketDetailDto> AssignAsync(int id, AssignRequest req, int actorId)
    {
        var ticket = await db.Tickets.FindAsync(id) ?? throw new KeyNotFoundException();
        var oldAssignee = ticket.AssigneeId?.ToString() ?? "unassigned";
        ticket.AssigneeId = req.AssigneeId;
        ticket.UpdatedAt  = DateTime.UtcNow;
        if (ticket.Status == "New" || ticket.Status == "Under Review")
            ticket.Status = "Assigned";

        await AddHistoryAsync(id, actorId, "assigned", "assignee_id",
                              oldAssignee, req.AssigneeId.ToString(), req.Note);
        await db.SaveChangesAsync();
        return await GetDetailAsync(id);
    }

    // ── Add comment ──────────────────────────────────────────────────────────
    public async Task<CommentDto> AddCommentAsync(int ticketId, AddCommentRequest req, int authorId)
    {
        var comment = new TicketComment
        {
            TicketId   = ticketId,
            AuthorId   = authorId,
            Body       = req.Body,
            Visibility = req.Visibility,
        };
        db.TicketComments.Add(comment);
        await AddHistoryAsync(ticketId, authorId, "commented", null, null, null,
                              $"[{req.Visibility}] comment added");
        await db.SaveChangesAsync();

        var author = await db.Users.FindAsync(authorId);
        return new CommentDto(comment.Id, comment.Body, comment.Visibility,
                              author?.Name ?? "", comment.CreatedAt);
    }

    // ── Add time entry ───────────────────────────────────────────────────────
    public async Task<TimeEntryDto> AddTimeEntryAsync(int ticketId, AddTimeEntryRequest req, int userId)
    {
        var entry = new TimeEntry
        {
            TicketId = ticketId, UserId = userId,
            WorkDate = req.WorkDate, Minutes = req.Minutes,
            Billable = req.Billable, Notes   = req.Notes,
        };
        db.TimeEntries.Add(entry);
        await db.SaveChangesAsync();
        var user = await db.Users.FindAsync(userId);
        return new TimeEntryDto(entry.Id, entry.WorkDate, entry.Minutes,
                                entry.Billable, entry.Notes, user?.Name ?? "");
    }

    // ── Add test result ──────────────────────────────────────────────────────
    public async Task<TestResultDto> AddTestResultAsync(int ticketId, AddTestResultRequest req, int testerId)
    {
        var result = new TestResult
        {
            TicketId     = ticketId, TesterId = testerId,
            Result       = req.Result, Environment = req.Environment,
            Notes        = req.Notes, EvidencePath = req.EvidencePath,
            TestedAt     = DateTime.UtcNow,
        };
        db.TestResults.Add(result);

        // Auto-transition on pass/fail
        var ticket = await db.Tickets.FindAsync(ticketId);
        if (ticket != null)
        {
            if (req.Result == "Pass" && ticket.Status == "Testing")
            {
                ticket.Status    = "Resolved";
                ticket.ResolvedAt= DateTime.UtcNow;
                await AddHistoryAsync(ticketId, testerId, "status_change", "status",
                                      "Testing", "Resolved", "QA passed");
            }
            else if (req.Result == "Fail" && ticket.Status == "Testing")
            {
                ticket.Status = "In Progress";
                await AddHistoryAsync(ticketId, testerId, "status_change", "status",
                                      "Testing", "In Progress", $"QA failed: {req.Notes}");
            }
            ticket.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        var tester = await db.Users.FindAsync(testerId);
        return new TestResultDto(result.Id, result.Result, result.Environment,
                                 result.Notes, tester?.Name ?? "", result.TestedAt);
    }

    // ── Dashboard stats ──────────────────────────────────────────────────────
    public async Task<DashboardStats> GetStatsAsync(int? scopedCustomerId = null)
    {
        var now  = DateTime.UtcNow;
        var query = db.Tickets.Where(t => t.Status != "Closed");
        if (scopedCustomerId.HasValue)
            query = query.Where(t => t.CustomerId == scopedCustomerId.Value);
        var open = await query.ToListAsync();

        var resolvedTickets = await db.Tickets
            .Where(t => t.ResolvedAt != null && t.CreatedAt != null)
            .Select(t => new { t.CreatedAt, t.ResolvedAt })
            .ToListAsync();
        var avgHours = resolvedTickets.Any()
            ? resolvedTickets.Average(t =>
                (t.ResolvedAt!.Value - t.CreatedAt!.Value).TotalHours)
            : 0;

        return new DashboardStats(
            OpenTickets:       open.Count,
            UnassignedTickets: open.Count(t => t.AssigneeId == null),
            CriticalHighTickets: open.Count(t => t.Priority is "Critical" or "High"),
            SlaAtRisk:         open.Count(t => t.SlaDueAt.HasValue
                                            && t.SlaDueAt.Value > now
                                            && t.SlaDueAt.Value < now.AddHours(4)),
            OverdueTickets:    open.Count(t => t.SlaDueAt.HasValue && t.SlaDueAt.Value < now),
            AvgResolutionHours: avgHours);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static DateTime ComputeSla(string priority) => priority switch
    {
        "Critical" => DateTime.UtcNow.AddHours(2),
        "High"     => DateTime.UtcNow.AddHours(4),
        "Medium"   => DateTime.UtcNow.AddHours(8),
        _          => DateTime.UtcNow.AddHours(24),
    };

    private async Task AddHistoryAsync(int ticketId, int actorId, string action,
        string? field, string? oldVal, string? newVal, string? note)
    {
        db.TicketHistory.Add(new TicketHistory
        {
            TicketId  = ticketId, ActorId = actorId,
            Action    = action, FieldName = field,
            OldValue  = oldVal, NewValue  = newVal, Note = note,
        });
        await db.SaveChangesAsync();
    }

    private static TicketDetailDto MapDetail(Ticket t) => new(
        t.Id, t.TicketNo, t.Subject, t.Type, t.Category, t.Priority, t.Status,
        t.Description, t.ReproductionSteps, t.ExpectedResult, t.ActualResult,
        t.CustomerId,  t.Customer?.Name  ?? "",
        t.ApplicationId, t.Application?.Name,
        t.AssigneeId, t.Assignee?.Name,
        t.CreatedBy,  t.Creator?.Name    ?? "",
        t.SlaDueAt, t.ResolvedAt, t.ClosedAt, t.CreatedAt, t.UpdatedAt,
        t.Comments.Select(c => new CommentDto(c.Id, c.Body, c.Visibility,
                                              c.Author?.Name ?? "", c.CreatedAt))
                  .OrderBy(c => c.CreatedAt),
        t.History.Select(h => new HistoryDto(h.Id, h.Action, h.FieldName,
                                             h.OldValue, h.NewValue, h.Note,
                                             h.Actor?.Name ?? "", h.CreatedAt))
                 .OrderByDescending(h => h.CreatedAt));
}
