using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PinusTickets.Models;

[Table("notifications")]
public class Notification
{
    [Key][Column("id")]          public int      Id          { get; set; }
    [Column("event_type")]       public string   EventType   { get; set; } = "";
    [Column("ticket_id")]        public int?     TicketId    { get; set; }
    [Column("recipient_id")]     public int      RecipientId { get; set; }
    [Column("channel")]          public string   Channel     { get; set; } = "Email";
    [Column("subject")]          public string?  Subject     { get; set; }
    [Column("body")]             public string?  Body        { get; set; }
    [Column("status")]           public string   Status      { get; set; } = "Pending";
    [Column("sent_at")]          public DateTime? SentAt     { get; set; }
    [Column("error_message")]    public string?  ErrorMessage{ get; set; }
    [Column("created_at")]       public DateTime? CreatedAt  { get; set; } = DateTime.UtcNow;

    public Ticket? Ticket { get; set; }
}
