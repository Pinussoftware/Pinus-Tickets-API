using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PinusTickets.Models;

[Table("ticket_sequences")]
public class TicketSequence
{
    [Key][Column("type")]    public string Type   { get; set; } = "";
    [Column("last_no")]      public int    LastNo { get; set; } = 0;
}
