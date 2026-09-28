using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PinusTickets.Models;

[Table("environments")]
public class Environment
{
    [Key][Column("id")]          public int     Id          { get; set; }
    [Column("name")]             public string  Name        { get; set; } = "";
    [Column("description")]      public string? Description { get; set; }
    [Column("icon")]             public string  Icon        { get; set; } = "🌐";
    [Column("is_active")]        public bool    IsActive    { get; set; } = true;
    [Column("created_at")]       public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
}
