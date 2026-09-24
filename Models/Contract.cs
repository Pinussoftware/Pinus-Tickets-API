using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PinusTickets.Models;

[Table("contracts")]
public class Contract
{
    [Key][Column("id")] public int Id { get; set; }
    [Column("customer_id")] public int CustomerId { get; set; }
    [Column("contract_number")] public string ContractNumber { get; set; } = "";
    [Column("plan_name")] public string PlanName { get; set; } = "Standard";
    [Column("start_date")] public DateTime StartDate { get; set; }
    [Column("end_date")] public DateTime EndDate { get; set; }
    [Column("status")] public string Status { get; set; } = "active";
    [Column("response_hours_critical")] public int ResponseHoursCritical { get; set; } = 2;
    [Column("response_hours_high")] public int ResponseHoursHigh { get; set; } = 4;
    [Column("response_hours_medium")] public int ResponseHoursMedium { get; set; } = 8;
    [Column("response_hours_low")] public int ResponseHoursLow { get; set; } = 24;
    [Column("created_at")] public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

    public Customer? Customer { get; set; }
}
