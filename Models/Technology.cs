using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PinusTickets.Models;

[Table("technologies")]
public class Technology
{
    [Key][Column("id")]          public int    Id        { get; set; }
    [Column("name")]             public string Name      { get; set; } = "";
    [Column("category")]         public string Category  { get; set; } = "Technology"; // Technology | Database
    [Column("icon")]             public string Icon      { get; set; } = "⚙️";
    [Column("is_active")]        public bool   IsActive  { get; set; } = true;
    [Column("created_at")]       public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
}
