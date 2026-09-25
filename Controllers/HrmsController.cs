using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using PinusTickets.Data;
using PinusTickets.Models;
using BCrypt.Net;

namespace PinusTickets.Controllers;

[ApiController]
[Route("api/v1/hrms")]
[Authorize]
public class HrmsController(IConfiguration cfg, AppDbContext db, ILogger<HrmsController> log) : ControllerBase
{
    // ── GET /api/v1/hrms/employees  — live query from HRMS MySQL ─────────────
    [HttpGet("employees")]
    public IActionResult GetEmployees()
    {
        var connStr = cfg.GetConnectionString("HrmsConnection");
        var result  = new List<object>();

        try
        {
            using var conn = new MySqlConnection(connStr);
            conn.Open();
            using var cmd = new MySqlCommand(@"
                SELECT e.id, e.employee_code, e.first_name, e.last_name,
                       e.email, e.phone_number, e.employment_status,
                       e.employment_type, e.work_location, e.joining_date,
                       d.department_name, des.designation_name
                FROM   employees e
                LEFT   JOIN departments  d   ON e.department_id  = d.id
                LEFT   JOIN designations des ON e.designation_id = des.Id
                WHERE  e.is_deleted = 0 AND e.is_active = 1
                ORDER  BY e.first_name", conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new
                {
                    Id             = reader.GetInt32("id"),
                    EmployeeCode   = reader.GetString("employee_code"),
                    FirstName      = reader.GetString("first_name"),
                    LastName       = reader.GetString("last_name"),
                    FullName       = $"{reader.GetString("first_name")} {reader.GetString("last_name")}",
                    Email          = reader.GetString("email"),
                    Phone          = reader.IsDBNull(reader.GetOrdinal("phone_number"))  ? null : reader.GetString("phone_number"),
                    Status         = reader.GetString("employment_status"),
                    EmploymentType = reader.GetString("employment_type"),
                    WorkLocation   = reader.IsDBNull(reader.GetOrdinal("work_location"))  ? null : reader.GetString("work_location"),
                    JoiningDate    = reader.IsDBNull(reader.GetOrdinal("joining_date"))   ? (DateTime?)null : reader.GetDateTime("joining_date"),
                    Department     = reader.IsDBNull(reader.GetOrdinal("department_name"))  ? null : reader.GetString("department_name"),
                    Designation    = reader.IsDBNull(reader.GetOrdinal("designation_name")) ? null : reader.GetString("designation_name"),
                });
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to connect to HRMS database");
            return StatusCode(503, new { message = "HRMS database unavailable", detail = ex.Message });
        }
    }

    // ── POST /api/v1/hrms/sync  — import HRMS employees as Ticket users ──────
    [HttpPost("sync")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Sync()
    {
        var connStr = cfg.GetConnectionString("HrmsConnection");
        var synced  = new List<object>();
        var skipped = new List<string>();

        try
        {
            using var conn = new MySqlConnection(connStr);
            conn.Open();
            using var cmd = new MySqlCommand(@"
                SELECT e.employee_code, e.first_name, e.last_name,
                       e.email, e.phone_number, des.designation_name
                FROM   employees e
                LEFT   JOIN designations des ON e.designation_id = des.Id
                WHERE  e.is_deleted = 0 AND e.is_active = 1", conn);

            using var reader = cmd.ExecuteReader();
            var rows = new List<(string code, string first, string last, string email, string? phone, string? designation)>();
            while (reader.Read())
                rows.Add((
                    reader.GetString("employee_code"),
                    reader.GetString("first_name"),
                    reader.GetString("last_name"),
                    reader.GetString("email"),
                    reader.IsDBNull(reader.GetOrdinal("phone_number"))   ? null : reader.GetString("phone_number"),
                    reader.IsDBNull(reader.GetOrdinal("designation_name")) ? null : reader.GetString("designation_name")
                ));

            // Map HRMS designation → Tickets role
            static string MapRole(string? designation) => designation?.ToLower() switch
            {
                string d when d.Contains("manager")   => "SupportManager",
                string d when d.Contains("qa")        => "QA",
                string d when d.Contains("test")      => "QA",
                string d when d.Contains("developer") => "Developer",
                string d when d.Contains("support")   => "SupportExecutive",
                string d when d.Contains("hr")        => "SupportExecutive",
                _                                     => "SupportExecutive"
            };

            foreach (var r in rows)
            {
                var existing = db.Users.FirstOrDefault(u => u.Email == r.email);
                if (existing != null)
                {
                    // Update phone/name if changed
                    existing.Name  = $"{r.first} {r.last}";
                    existing.Phone = r.phone;
                    skipped.Add($"{r.email} (already exists — updated)");
                }
                else
                {
                    var user = new User
                    {
                        Name         = $"{r.first} {r.last}",
                        Email        = r.email,
                        Phone        = r.phone,
                        Role         = MapRole(r.designation),
                        OrganizationId = 1,
                        Status       = "active",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pinus@123"),
                    };
                    db.Users.Add(user);
                    synced.Add(new { user.Name, user.Email, user.Role, Source = "HRMS", HrmsCode = r.code });
                }
            }

            await db.SaveChangesAsync();
            return Ok(new { synced = synced.Count, updated = skipped.Count, details = synced, skipped });
        }
        catch (Exception ex)
        {
            log.LogError(ex, "HRMS sync failed");
            return StatusCode(503, new { message = "Sync failed", detail = ex.Message });
        }
    }
}
