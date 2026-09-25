using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PinusTickets.Data;
using PinusTickets.Models;

namespace PinusTickets.Controllers;

[ApiController]
[Route("api/v1/tickets/{ticketId:int}/attachments")]
[Authorize]
public class AttachmentsController(AppDbContext db, IConfiguration cfg, ILogger<AttachmentsController> log) : ControllerBase
{
    private readonly string _uploadDir = Path.Combine(
        cfg["UploadPath"] ?? "/opt/pinus-tickets/uploads");

    // ── POST /api/v1/tickets/{id}/attachments ────────────────────────────────
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(int ticketId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });
        if (file.Length > 20 * 1024 * 1024)
            return BadRequest(new { message = "File exceeds 20MB limit" });

        var ticket = await db.Tickets.FindAsync(ticketId);
        if (ticket == null) return NotFound();

        Directory.CreateDirectory(_uploadDir);

        var ext      = Path.GetExtension(file.FileName);
        var key      = $"{ticketId}/{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(_uploadDir, key.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var stream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(stream);

        var userId = int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : 1;

        var att = new TicketAttachment
        {
            TicketId   = ticketId,
            FileName   = file.FileName,
            StorageKey = key,
            MimeType   = file.ContentType,
            SizeBytes  = file.Length,
            UploadedBy = userId,
            CreatedAt  = DateTime.UtcNow
        };
        db.TicketAttachments.Add(att);
        await db.SaveChangesAsync();

        return Ok(new
        {
            att.Id,
            att.FileName,
            att.MimeType,
            att.SizeBytes,
            Url = $"/uploads/{key}"
        });
    }

    // ── GET /api/v1/tickets/{id}/attachments ─────────────────────────────────
    [HttpGet]
    public IActionResult List(int ticketId)
    {
        var list = db.TicketAttachments
            .Where(a => a.TicketId == ticketId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new {
                a.Id, a.FileName, a.MimeType, a.SizeBytes,
                Url = $"/uploads/{a.StorageKey}",
                a.CreatedAt
            }).ToList();
        return Ok(list);
    }

    // ── DELETE /api/v1/tickets/{ticketId}/attachments/{id} ───────────────────
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int ticketId, int id)
    {
        var att = await db.TicketAttachments.FindAsync(id);
        if (att == null || att.TicketId != ticketId) return NotFound();
        try
        {
            var path = Path.Combine(_uploadDir, att.StorageKey.Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
        catch (Exception ex) { log.LogWarning(ex, "Could not delete file {Key}", att.StorageKey); }
        db.TicketAttachments.Remove(att);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
