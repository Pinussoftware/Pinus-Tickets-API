namespace PinusTickets.DTOs;

// ── Auth ─────────────────────────────────────────────────────────────────────
public record LoginRequest(string Email, string Password);
public record LoginResponse(int UserId, string Name, string Email, string Role,
                            string OrganizationName, string Token);

// ── Users ────────────────────────────────────────────────────────────────────
public record UserDto(int Id, string Name, string Email, string Role, string Status, string? Phone, DateTime? CreatedAt);
public record CreateUserRequest(string Name, string Email, string Password,
                                string Role, int OrganizationId, string? Phone);
public record UpdateUserRequest(string? Name, string? Role, string? Phone, string? Status, string? Password, int? CustomerId);

// ── Customers ────────────────────────────────────────────────────────────────
public record CustomerDto(
    int Id, string Name, string AccountCode, string Status, int OrganizationId,
    string? Industry, string? ContactPerson, string? Phone, string? Email, string? Website,
    string? Gstin, string? TaxNo, string? SlaPlan, string? SinceYear,
    string? Address, string? City, string? State, string? Pincode, string? Country,
    string? BankName, string? BranchName, string? AccountName, string? AccountNumber,
    string? AccountType, string? IfscCode, string? SwiftCode, string? MicrCode, string? UpiId,
    string? SupportEmail, string? EscalationContact, string? Timezone,
    string? BusinessHours, int? MaxTicketsPerMonth, string? Notes);

public record CreateCustomerRequest(
    string Name, string AccountCode, int OrganizationId,
    string? Industry, string? ContactPerson, string? Phone, string? Email, string? Website,
    string? Gstin, string? TaxNo, string? SlaPlan, string? Status, string? SinceYear,
    string? Address, string? City, string? State, string? Pincode, string? Country,
    string? BankName, string? BranchName, string? AccountName, string? AccountNumber,
    string? AccountType, string? IfscCode, string? SwiftCode, string? MicrCode, string? UpiId,
    string? SupportEmail, string? EscalationContact, string? Timezone,
    string? BusinessHours, int? MaxTicketsPerMonth, string? Notes);

// ── Applications ─────────────────────────────────────────────────────────────
public record ApplicationDto(int Id, string Name, string? Version, string? Technology,
    string Status, int CustomerId, string? CustomerName,
    string? Description, string? DatabaseTech, string? DeploymentType,
    string? SupportTeam, string? SlaPriority, string? Notes);
public record CreateApplicationRequest(string Name, int CustomerId,
    string? Version, string? Technology, string? Status,
    string? Description, string? DatabaseTech, string? DeploymentType,
    string? SupportTeam, string? SlaPriority, string? Notes);

// ── Contracts ─────────────────────────────────────────────────────────────────
public record ContractDto(int Id, int CustomerId, string CustomerName,
    string ContractNumber, string PlanName, DateTime StartDate, DateTime EndDate,
    string Status, int ResponseHoursCritical, int ResponseHoursHigh,
    int ResponseHoursMedium, int ResponseHoursLow, DateTime? CreatedAt);
public record CreateContractRequest(int CustomerId, string ContractNumber,
    string PlanName, DateTime StartDate, DateTime EndDate, string? Status,
    int ResponseHoursCritical, int ResponseHoursHigh,
    int ResponseHoursMedium, int ResponseHoursLow);

// ── Tickets ──────────────────────────────────────────────────────────────────
public record TicketListItem(int Id, string TicketNo, string Subject,
                             string CustomerName, string? ApplicationName,
                             string Type, string Priority, string Status,
                             string? AssigneeName, DateTime? SlaDueAt,
                             DateTime? UpdatedAt);

public record TicketDetailDto(int Id, string TicketNo, string Subject,
    string Type, string? Category, string Priority, string Status,
    string Description, string? ReproductionSteps,
    string? ExpectedResult, string? ActualResult,
    int CustomerId, string CustomerName,
    int? ApplicationId, string? ApplicationName,
    int? AssigneeId, string? AssigneeName,
    int CreatedBy, string CreatorName,
    DateTime? SlaDueAt, DateTime? ResolvedAt, DateTime? ClosedAt,
    DateTime? CreatedAt, DateTime? UpdatedAt,
    IEnumerable<CommentDto> Comments,
    IEnumerable<HistoryDto> History);

public record CreateTicketRequest(
    string Subject, string Description, string Type, string Priority,
    int CustomerId, int? ApplicationId,
    string? Category, string? ReproductionSteps,
    string? ExpectedResult, string? ActualResult);

public record TransitionRequest(string NewStatus, string? Reason);
public record AssignRequest(int AssigneeId, string? Note);

// ── Comments ─────────────────────────────────────────────────────────────────
public record CommentDto(int Id, string Body, string Visibility,
                         string AuthorName, DateTime? CreatedAt);
public record AddCommentRequest(string Body, string Visibility = "customer");

// ── History ──────────────────────────────────────────────────────────────────
public record HistoryDto(int Id, string Action, string? FieldName,
                         string? OldValue, string? NewValue,
                         string? Note, string ActorName, DateTime? CreatedAt);

// ── Time Entries ─────────────────────────────────────────────────────────────
public record TimeEntryDto(int Id, DateTime WorkDate, int Minutes,
                           bool Billable, string? Notes, string UserName);
public record AddTimeEntryRequest(DateTime WorkDate, int Minutes,
                                  bool Billable = true, string? Notes = null);

// ── Test Results ─────────────────────────────────────────────────────────────
public record TestResultDto(int Id, string Result, string? Environment,
                            string? Notes, string TesterName, DateTime? TestedAt);
public record AddTestResultRequest(string Result, string? Environment,
                                   string? Notes, string? EvidencePath);

public record SetStatusRequest(string Status);

// ── Dashboard ────────────────────────────────────────────────────────────────
public record DashboardStats(int OpenTickets, int UnassignedTickets,
    int CriticalHighTickets, int SlaAtRisk, int OverdueTickets,
    double AvgResolutionHours);
