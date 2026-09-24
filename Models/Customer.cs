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
    [Column("status")]                 public string   Status          { get; set; } = "active";
    [Column("primary_contact_id")]     public int?     PrimaryContactId{ get; set; }
    [Column("created_at")]             public DateTime? CreatedAt      { get; set; } = DateTime.UtcNow;

    // General
    [Column("industry")]               public string?  Industry        { get; set; }
    [Column("contact_person")]         public string?  ContactPerson   { get; set; }
    [Column("phone")]                  public string?  Phone           { get; set; }
    [Column("email")]                  public string?  Email           { get; set; }
    [Column("website")]                public string?  Website         { get; set; }
    [Column("gstin")]                  public string?  Gstin           { get; set; }
    [Column("tax_no")]                 public string?  TaxNo           { get; set; }
    [Column("sla_plan")]               public string?  SlaPlan         { get; set; } = "Standard";
    [Column("since_year")]             public string?  SinceYear       { get; set; }

    // Address
    [Column("address")]                public string?  Address         { get; set; }
    [Column("city")]                   public string?  City            { get; set; }
    [Column("state")]                  public string?  State           { get; set; }
    [Column("pincode")]                public string?  Pincode         { get; set; }
    [Column("country")]                public string?  Country         { get; set; } = "India";

    // Bank details
    [Column("bank_name")]              public string?  BankName        { get; set; }
    [Column("branch_name")]            public string?  BranchName      { get; set; }
    [Column("account_name")]           public string?  AccountName     { get; set; }
    [Column("account_number")]         public string?  AccountNumber   { get; set; }
    [Column("account_type")]           public string?  AccountType     { get; set; }
    [Column("ifsc_code")]              public string?  IfscCode        { get; set; }
    [Column("swift_code")]             public string?  SwiftCode       { get; set; }
    [Column("micr_code")]              public string?  MicrCode        { get; set; }
    [Column("upi_id")]                 public string?  UpiId           { get; set; }

    // Support scope
    [Column("support_email")]          public string?  SupportEmail    { get; set; }
    [Column("escalation_contact")]     public string?  EscalationContact{ get; set; }
    [Column("timezone")]               public string?  Timezone        { get; set; }
    [Column("business_hours")]         public string?  BusinessHours   { get; set; }
    [Column("max_tickets_per_month")]  public int?     MaxTicketsPerMonth{ get; set; }
    [Column("notes")]                  public string?  Notes           { get; set; }

    public Organization? Organization { get; set; }
    public ICollection<Application> Applications { get; set; } = [];
    public ICollection<Contract> Contracts { get; set; } = [];
    public ICollection<Ticket> Tickets { get; set; } = [];
}
