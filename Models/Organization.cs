using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PinusTickets.Models;

[Table("organizations")]
public class Organization
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("name")] public string Name { get; set; } = "";
    [Column("code")] public string Code { get; set; } = "";
    [Column("status")] public string Status { get; set; } = "active";
    [Column("timezone")] public string Timezone { get; set; } = "UTC";
    [Column("created_at")] public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<User> Users { get; set; } = [];
    public ICollection<Customer> Customers { get; set; } = [];
}
