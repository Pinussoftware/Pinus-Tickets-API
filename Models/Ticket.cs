using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PinusTickets.Models;

[Table("tickets")]
public class Ticket
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("ticket_no")] public string TicketNo { get; set; } = "";
    [Column("customer_id")] public int CustomerId { get; set; }
    [Column("application_id")] public int? ApplicationId { get; set; }
    [Column("type")] public string Type { get; set; } = "Bug";            // Bug, Feature, Support, Change
    [Column("category")] public string? Category { get; set; }
    [Column("priority")] public string Priority { get; set; } = "Medium"; // Critical, High, Medium, Low
    [Column("status")] public string Status { get; set; } = "New";        // Workflow states
    [Column("subject")] public string Subject { get; set; } = "";
    [Column("description")] public string Description { get; set; } = "";
    [Column("reproduction_steps")] public string? ReproductionSteps { get; set; }
    [Column("expected_result")] public string? ExpectedResult { get; set; }
    [Column("actual_result")] public string? ActualResult { get; set; }
    [Column("assignee_id")] public int? AssigneeId { get; set; }
    [Column("created_by")] public int CreatedBy { get; set; }
    [Column("sla_due_at")] public DateTime? SlaDueAt { get; set; }
    [Column("resolved_at")] public DateTime? ResolvedAt { get; set; }
    [Column("closed_at")] public DateTime? ClosedAt { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")] public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

    public Customer? Customer { get; set; }
    public Application? Application { get; set; }
    public User? Assignee { get; set; }
    public User? Creator { get; set; }
    public ICollection<TicketComment> Comments { get; set; } = [];
    public ICollection<TicketAttachment> Attachments { get; set; } = [];
    public ICollection<TicketHistory> History { get; set; } = [];
    public ICollection<TimeEntry> TimeEntries { get; set; } = [];
    public ICollection<TestResult> TestResults { get; set; } = [];
}
