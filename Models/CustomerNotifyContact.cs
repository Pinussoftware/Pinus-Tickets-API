using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PinusTickets.Models;

[Table("customer_notify_contacts")]
public class CustomerNotifyContact
{
    [Key][Column("id")]              public int     Id             { get; set; }
    [Column("customer_id")]          public int     CustomerId     { get; set; }
    [Column("name")]                 public string  Name           { get; set; } = "";
    [Column("email")]                public string  Email          { get; set; } = "";
    [Column("role_label")]           public string? RoleLabel      { get; set; }
    [Column("group_name")]           public string? GroupName      { get; set; }
    [Column("notify_on_create")]     public bool    NotifyOnCreate { get; set; } = true;
    [Column("notify_on_status")]     public bool    NotifyOnStatus { get; set; } = true;
    [Column("notify_on_assign")]     public bool    NotifyOnAssign { get; set; } = false;
    [Column("notify_on_resolve")]    public bool    NotifyOnResolve{ get; set; } = true;
    [Column("is_active")]            public bool    IsActive       { get; set; } = true;
    [Column("created_at")]           public DateTime? CreatedAt    { get; set; } = DateTime.UtcNow;

    public Customer? Customer { get; set; }
}
