using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PinusTickets.Models;

[Table("role_permissions")]
public class RolePermission
{
    [Key][Column("id")]           public int     Id          { get; set; }
    [Column("role")]              public string  Role        { get; set; } = "";
    [Column("page_key")]          public string  PageKey     { get; set; } = "";
    [Column("page_label")]        public string  PageLabel   { get; set; } = "";
    [Column("page_icon")]         public string  PageIcon    { get; set; } = "📄";
    [Column("page_section")]      public string  PageSection { get; set; } = "General";
    [Column("can_access")]        public bool    CanAccess   { get; set; }
    [Column("can_create")]        public bool    CanCreate   { get; set; }
    [Column("can_edit")]          public bool    CanEdit     { get; set; }
    [Column("can_delete")]        public bool    CanDelete   { get; set; }
    [Column("updated_at")]        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
}
