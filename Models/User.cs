using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PinusTickets.Models;

[Table("users")]
public class User
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("organization_id")] public int OrganizationId { get; set; }
    [Column("name")] public string Name { get; set; } = "";
    [Column("email")] public string Email { get; set; } = "";
    [Column("password_hash")] public string PasswordHash { get; set; } = "";
    [Column("phone")] public string? Phone { get; set; }
    [Column("customer_id")] public int? CustomerId { get; set; }
    [Column("role")] public string Role { get; set; } = "SupportExecutive";
    [Column("status")] public string Status { get; set; } = "active";
    [Column("last_login_at")] public DateTime? LastLoginAt { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    public Organization? Organization { get; set; }
    public ICollection<Ticket> AssignedTickets { get; set; } = [];
}
