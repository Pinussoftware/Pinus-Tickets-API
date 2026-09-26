using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using PinusTickets.Data;
using PinusTickets.Models;

namespace PinusTickets.Controllers;

[ApiController]
[Route("api/v1/erp")]
[Authorize]
public class ErpSyncController(IConfiguration cfg, AppDbContext db, ILogger<ErpSyncController> log) : ControllerBase
{
    // ── GET /api/v1/erp/clients  — preview ERP clients before syncing ─────────
    [HttpGet("clients")]
    public IActionResult GetErpClients()
    {
        var rows = FetchErpClients();
        if (rows is null) return StatusCode(503, new { message = "ERP database unavailable" });

        // Mark which ones already exist in tickets
        var existingCodes = db.Customers.Select(c => c.AccountCode).ToHashSet();
        var existingNames = db.Customers.Select(c => c.Name.ToLower()).ToHashSet();

        var result = rows.Select(r => new
        {
            r.ClientCode, r.ClientName, r.ContactPerson, r.Phone, r.Email,
            r.City, r.State, r.Gstin, r.TaxNo, r.Address, r.Pincode, r.Country,
            r.BankName, r.BranchName, r.AccountName, r.AccountNumber,
            r.AccountType, r.IfscCode, r.SwiftCode, r.MicrCode,
            AlreadySynced = existingCodes.Contains(r.ClientCode) ||
                            existingNames.Contains(r.ClientName.ToLower())
        }).ToList();

        return Ok(result);
    }

    // ── POST /api/v1/erp/sync  — import ERP clients → tickets customers ───────
    [HttpPost("sync")]
    [Authorize(Roles = "Admin,SupportManager")]
    public async Task<IActionResult> Sync([FromBody] SyncRequest? req)
    {
        var rows = FetchErpClients();
        if (rows is null) return StatusCode(503, new { message = "ERP database unavailable" });

        // Filter to selected codes if provided, else sync all active
        if (req?.ClientCodes?.Length > 0)
            rows = rows.Where(r => req.ClientCodes.Contains(r.ClientCode)).ToList();

        var synced  = new List<object>();
        var updated = new List<object>();
        var skipped = new List<string>();
        int orgId   = 1;

        foreach (var r in rows)
        {
            // Match by client_code or name (case-insensitive)
            var existing = db.Customers.FirstOrDefault(c =>
                c.AccountCode == r.ClientCode ||
                c.Name.ToLower() == r.ClientName.ToLower());

            if (existing != null)
            {
                // Update fields that may have changed in ERP
                existing.Name          = r.ClientName.Trim();
                existing.ContactPerson = r.ContactPerson;
                existing.Phone         = r.Phone?.Trim();
                existing.Email         = r.Email?.Trim();
                existing.Address       = r.Address;
                existing.City          = r.City;
                existing.State         = r.State;
                existing.Pincode       = r.Pincode;
                existing.Country       = r.Country ?? "India";
                existing.Gstin         = r.Gstin;
                existing.TaxNo         = r.TaxNo;
                existing.BankName      = r.BankName;
                existing.BranchName    = r.BranchName;
                existing.AccountName   = r.AccountName;
                existing.AccountNumber = r.AccountNumber;
                existing.AccountType   = r.AccountType;
                existing.IfscCode      = r.IfscCode;
                existing.SwiftCode     = r.SwiftCode;
                existing.MicrCode      = r.MicrCode;
                updated.Add(new { existing.AccountCode, existing.Name });
            }
            else
            {
                var customer = new Customer
                {
                    OrganizationId = orgId,
                    AccountCode    = r.ClientCode,
                    Name           = r.ClientName.Trim(),
                    ContactPerson  = r.ContactPerson,
                    Phone          = r.Phone?.Trim(),
                    Email          = r.Email?.Trim(),
                    Address        = r.Address,
                    City           = r.City,
                    State          = r.State,
                    Pincode        = r.Pincode,
                    Country        = r.Country ?? "India",
                    Gstin          = r.Gstin,
                    TaxNo          = r.TaxNo,
                    BankName       = r.BankName,
                    BranchName     = r.BranchName,
                    AccountName    = r.AccountName,
                    AccountNumber  = r.AccountNumber,
                    AccountType    = r.AccountType,
                    IfscCode       = r.IfscCode,
                    SwiftCode      = r.SwiftCode,
                    MicrCode       = r.MicrCode,
                    Status         = "active",
                    SlaPlan        = "Standard",
                };
                db.Customers.Add(customer);
                synced.Add(new { customer.AccountCode, customer.Name });
            }
        }

        await db.SaveChangesAsync();
        log.LogInformation("[ERP SYNC] Synced:{S} Updated:{U}", synced.Count, updated.Count);

        return Ok(new
        {
            message = $"Sync complete — {synced.Count} new, {updated.Count} updated",
            synced  = synced.Count,
            updated = updated.Count,
            newCustomers    = synced,
            updatedCustomers = updated
        });
    }

    // ── Helper: fetch all active clients from ERP MySQL ───────────────────────
    private List<ErpClient>? FetchErpClients()
    {
        var connStr = cfg.GetConnectionString("ErpConnection");
        var result  = new List<ErpClient>();
        try
        {
            using var conn = new MySqlConnection(connStr);
            conn.Open();
            using var cmd = new MySqlCommand(@"
                SELECT client_code, client_name, contact_person, phone, email,
                       address, city, state, pincode, country, gstin, tax_no,
                       bank_name, branch_name, account_name, account_number,
                       account_type, ifsc_code, swift_code, micr_code
                FROM   clients
                WHERE  is_active = 1
                ORDER  BY client_name", conn);

            using var r = cmd.ExecuteReader();
            while (r.Read())
                result.Add(new ErpClient(
                    ClientCode:    r.GetString("client_code"),
                    ClientName:    r.GetString("client_name"),
                    ContactPerson: r.IsDBNull(r.GetOrdinal("contact_person")) ? null : r.GetString("contact_person"),
                    Phone:         r.IsDBNull(r.GetOrdinal("phone"))          ? null : r.GetString("phone"),
                    Email:         r.IsDBNull(r.GetOrdinal("email"))          ? null : r.GetString("email"),
                    Address:       r.IsDBNull(r.GetOrdinal("address"))        ? null : r.GetString("address"),
                    City:          r.IsDBNull(r.GetOrdinal("city"))           ? null : r.GetString("city"),
                    State:         r.IsDBNull(r.GetOrdinal("state"))          ? null : r.GetString("state"),
                    Pincode:       r.IsDBNull(r.GetOrdinal("pincode"))        ? null : r.GetString("pincode"),
                    Country:       r.IsDBNull(r.GetOrdinal("country"))        ? null : r.GetString("country"),
                    Gstin:         r.IsDBNull(r.GetOrdinal("gstin"))          ? null : r.GetString("gstin"),
                    TaxNo:         r.IsDBNull(r.GetOrdinal("tax_no"))         ? null : r.GetString("tax_no"),
                    BankName:      r.IsDBNull(r.GetOrdinal("bank_name"))      ? null : r.GetString("bank_name"),
                    BranchName:    r.IsDBNull(r.GetOrdinal("branch_name"))    ? null : r.GetString("branch_name"),
                    AccountName:   r.IsDBNull(r.GetOrdinal("account_name"))   ? null : r.GetString("account_name"),
                    AccountNumber: r.IsDBNull(r.GetOrdinal("account_number")) ? null : r.GetString("account_number"),
                    AccountType:   r.IsDBNull(r.GetOrdinal("account_type"))   ? null : r.GetString("account_type"),
                    IfscCode:      r.IsDBNull(r.GetOrdinal("ifsc_code"))      ? null : r.GetString("ifsc_code"),
                    SwiftCode:     r.IsDBNull(r.GetOrdinal("swift_code"))     ? null : r.GetString("swift_code"),
                    MicrCode:      r.IsDBNull(r.GetOrdinal("micr_code"))      ? null : r.GetString("micr_code")
                ));
            return result;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to connect to ERP database");
            return null;
        }
    }
}

public record SyncRequest(string[]? ClientCodes);

public record ErpClient(
    string ClientCode, string ClientName, string? ContactPerson,
    string? Phone, string? Email, string? Address, string? City,
    string? State, string? Pincode, string? Country, string? Gstin,
    string? TaxNo, string? BankName, string? BranchName, string? AccountName,
    string? AccountNumber, string? AccountType, string? IfscCode,
    string? SwiftCode, string? MicrCode);
