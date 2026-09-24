using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PinusTickets.Models;

[Table("applications")]
public class Application
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("customer_id")] public int CustomerId { get; set; }
    [Column("name")] public string Name { get; set; } = "";
    [Column("version")] public string? Version { get; set; }
    [Column("owner_user_id")] public int? OwnerUserId { get; set; }
    [Column("technology")] public string? Technology { get; set; }
    [Column("status")] public string Status { get; set; } = "active";
    [Column("created_at")] public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    public Customer? Customer { get; set; }
    public ICollection<Ticket> Tickets { get; set; } = [];
}
