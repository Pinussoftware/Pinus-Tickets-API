using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PinusTickets.Models;

[Table("customers")]
public class Customer
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("organization_id")] public int OrganizationId { get; set; }
    [Column("account_code")] public string AccountCode { get; set; } = "";
    [Column("name")] public string Name { get; set; } = "";
    [Column("status")] public string Status { get; set; } = "active";
    [Column("primary_contact_id")] public int? PrimaryContactId { get; set; }
    [Column("created_at")] public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    public Organization? Organization { get; set; }
    public ICollection<Application> Applications { get; set; } = [];
    public ICollection<Contract> Contracts { get; set; } = [];
    public ICollection<Ticket> Tickets { get; set; } = [];
}
