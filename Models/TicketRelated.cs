using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PinusTickets.Models;

[Table("ticket_comments")]
public class TicketComment
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("ticket_id")] public int TicketId { get; set; }
    [Column("author_id")] public int AuthorId { get; set; }
    [Column("body")] public string Body { get; set; } = "";
    [Column("visibility")] public string Visibility { get; set; } = "customer"; // customer | internal
    [Column("created_at")] public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    public Ticket? Ticket { get; set; }
    public User? Author { get; set; }
}

[Table("ticket_attachments")]
public class TicketAttachment
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("ticket_id")] public int TicketId { get; set; }
    [Column("file_name")] public string FileName { get; set; } = "";
    [Column("storage_key")] public string StorageKey { get; set; } = "";
    [Column("mime_type")] public string? MimeType { get; set; }
    [Column("size_bytes")] public long SizeBytes { get; set; }
    [Column("uploaded_by")] public int UploadedBy { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    public Ticket? Ticket { get; set; }
}

[Table("ticket_history")]
public class TicketHistory
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("ticket_id")] public int TicketId { get; set; }
    [Column("actor_id")] public int ActorId { get; set; }
    [Column("action")] public string Action { get; set; } = "";        // status_change, assigned, commented, etc.
    [Column("field_name")] public string? FieldName { get; set; }
    [Column("old_value")] public string? OldValue { get; set; }
    [Column("new_value")] public string? NewValue { get; set; }
    [Column("note")] public string? Note { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    public Ticket? Ticket { get; set; }
    public User? Actor { get; set; }
}

[Table("time_entries")]
public class TimeEntry
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("ticket_id")] public int TicketId { get; set; }
    [Column("user_id")] public int UserId { get; set; }
    [Column("work_date")] public DateTime WorkDate { get; set; }
    [Column("minutes")] public int Minutes { get; set; }
    [Column("billable")] public bool Billable { get; set; } = true;
    [Column("notes")] public string? Notes { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    public Ticket? Ticket { get; set; }
    public User? User { get; set; }
}

[Table("test_results")]
public class TestResult
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("ticket_id")] public int TicketId { get; set; }
    [Column("tester_id")] public int TesterId { get; set; }
    [Column("environment")] public string? Environment { get; set; }
    [Column("result")] public string Result { get; set; } = "Pending"; // Pass, Fail, Blocked
    [Column("evidence_path")] public string? EvidencePath { get; set; }
    [Column("notes")] public string? Notes { get; set; }
    [Column("tested_at")] public DateTime? TestedAt { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    public Ticket? Ticket { get; set; }
    public User? Tester { get; set; }
}
