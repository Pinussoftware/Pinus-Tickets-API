using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using PinusTickets.Data;
using PinusTickets.Models;

namespace PinusTickets.Services;

public class EmailService(IConfiguration cfg, IServiceScopeFactory scopeFactory, ILogger<EmailService> log)
{
    private readonly string _uploadDir = cfg["UploadPath"] ?? "/opt/pinus-tickets/uploads";

    // ── Send one email — with optional file attachments ──────────────────────
    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody,
                                string eventType = "General", int? ticketId = null,
                                IEnumerable<string>? attachmentKeys = null)
    {
        var smtpCfg  = cfg.GetSection("Smtp");
        bool enabled = smtpCfg.GetValue<bool>("Enabled");
        string status    = "Pending";
        string? errorMsg = null;

        if (!enabled)
        {
            log.LogInformation("[EMAIL DISABLED] To:{To} Subject:{Subject}", toEmail, subject);
            status = "Skipped";
        }
        else
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(
                    smtpCfg["FromName"] ?? "Pinus Ticket System",
                    smtpCfg["FromEmail"]!));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = subject;

                // ── Build multipart if there are attachments ──────────────────
                var keys = attachmentKeys?.ToList() ?? [];
                if (keys.Count > 0)
                {
                    var multipart = new Multipart("mixed");
                    multipart.Add(new TextPart("html") { Text = htmlBody });

                    foreach (var key in keys)
                    {
                        var filePath = Path.Combine(_uploadDir,
                            key.Replace('/', Path.DirectorySeparatorChar));

                        if (!File.Exists(filePath))
                        {
                            log.LogWarning("[EMAIL] Attachment not found: {Path}", filePath);
                            continue;
                        }

                        var mimeType  = MimeTypes.GetMimeType(filePath);
                        var contentType = ContentType.Parse(mimeType);
                        var att = new MimePart(contentType.MediaType, contentType.MediaSubtype)
                        {
                            Content            = new MimeContent(File.OpenRead(filePath)),
                            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                            ContentTransferEncoding = ContentEncoding.Base64,
                            FileName = Path.GetFileName(filePath)
                        };
                        multipart.Add(att);
                    }
                    message.Body = multipart;
                }
                else
                {
                    message.Body = new TextPart("html") { Text = htmlBody };
                }

                using var client = new SmtpClient();
                await client.ConnectAsync(
                    smtpCfg["Host"]!,
                    smtpCfg.GetValue<int>("Port"),
                    smtpCfg.GetValue<bool>("UseSsl")
                        ? SecureSocketOptions.SslOnConnect
                        : SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(smtpCfg["Username"]!, smtpCfg["Password"]!);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                status = "Sent";
                log.LogInformation("[EMAIL SENT] To:{To} Subject:{Subject} Attachments:{Count}",
                    toEmail, subject, keys.Count);
            }
            catch (Exception ex)
            {
                status   = "Failed";
                errorMsg = ex.Message;
                log.LogError(ex, "[EMAIL FAILED] To:{To}", toEmail);
            }
        }

        // Always log the notification — fresh scope, never shares DbContext with caller
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Notifications.Add(new Notification
        {
            EventType    = eventType,
            RecipientId  = 0,
            Channel      = "Email",
            Status       = status,
            TicketId     = ticketId,
            Subject      = subject,
            Body         = htmlBody,
            SentAt       = status == "Sent" ? DateTime.UtcNow : null,
            ErrorMessage = errorMsg,
            CreatedAt    = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    // ── HTML Templates ────────────────────────────────────────────────────────
    public string TicketCreatedHtml(string ticketNo, string subject, string priority,
        string customer, string creatorName, string portalUrl) =>
        BaseHtml($"New Ticket Created — {ticketNo}", $@"
  <div class='header'>📋 New Support Ticket Created</div>
  <div class='body'>
    <p>Hello,</p>
    <p>A new support ticket has been raised in the Pinus Ticket System.</p>
    {TicketCard(ticketNo, subject, priority, customer)}
    <p><strong>Raised by:</strong> {creatorName}</p>
    <a href='{portalUrl}' class='btn'>View Ticket</a>
    <p class='footer-note'>Please review and assign this ticket at the earliest.</p>
    <p class='footer-note'>📎 Attachments (if any) are included with this email.</p>
  </div>");

    public string TicketAssignedHtml(string ticketNo, string subject, string priority,
        string customer, string assigneeName, string slaDue, string portalUrl) =>
        BaseHtml($"Ticket Assigned to You — {ticketNo}", $@"
  <div class='header'>🔧 Ticket Assigned to You</div>
  <div class='body'>
    <p>Hello <strong>{assigneeName}</strong>,</p>
    <p>A support ticket has been assigned to you. Please start working on it within the SLA timeline.</p>
    {TicketCard(ticketNo, subject, priority, customer)}
    <p><strong>SLA Due:</strong> {slaDue}</p>
    <a href='{portalUrl}' class='btn'>Open My Workbench</a>
    <p class='footer-note'>📎 Ticket attachments (if any) are included with this email.</p>
  </div>");

    public string TicketStatusChangedHtml(string ticketNo, string subject,
        string oldStatus, string newStatus, string customerName, string portalUrl) =>
        BaseHtml($"Ticket Status Updated — {ticketNo}", $@"
  <div class='header'>🔄 Ticket Status Updated</div>
  <div class='body'>
    <p>Hello <strong>{customerName}</strong>,</p>
    <p>Your support ticket <strong>{ticketNo}</strong> has been updated.</p>
    <div class='status-row'>
      <span class='status old'>{oldStatus}</span>
      <span class='arrow'>→</span>
      <span class='status new'>{newStatus}</span>
    </div>
    <p><strong>Subject:</strong> {subject}</p>
    <a href='{portalUrl}' class='btn'>Track Your Ticket</a>
  </div>");

    public string TicketResolvedHtml(string ticketNo, string subject,
        string customerName, string resolution, string portalUrl) =>
        BaseHtml($"Ticket Resolved — {ticketNo}", $@"
  <div class='header'>✅ Your Ticket Has Been Resolved</div>
  <div class='body'>
    <p>Hello <strong>{customerName}</strong>,</p>
    <p>We are pleased to inform you that your ticket <strong>{ticketNo}</strong> has been resolved.</p>
    <p><strong>Subject:</strong> {subject}</p>
    <p><strong>Resolution:</strong> {resolution}</p>
    <a href='{portalUrl}' class='btn'>Confirm Resolution</a>
    <p class='footer-note'>If the issue persists, you can reopen the ticket from the portal.</p>
  </div>");

    // ── Shared helpers ────────────────────────────────────────────────────────
    private static string TicketCard(string no, string subject, string priority, string customer) => $@"
  <div class='ticket-card'>
    <div class='tc-no'>{no}</div>
    <div class='tc-subject'>{subject}</div>
    <div class='tc-meta'><span class='p-badge p-{priority.ToLower()}'>{priority}</span> &bull; {customer}</div>
  </div>";

    private static string BaseHtml(string title, string content) => $@"
<!DOCTYPE html><html><head><meta charset='utf-8'><title>{title}</title>
<style>
  body  {{ font-family:'Segoe UI',Arial,sans-serif; background:#f1f5f9; margin:0; padding:20px; }}
  .wrap {{ max-width:580px; margin:0 auto; }}
  .logo {{ background:#171a35; padding:20px 28px; border-radius:12px 12px 0 0; }}
  .logo-text {{ color:#fff; font-size:18px; font-weight:700; letter-spacing:1px; }}
  .card {{ background:#fff; border-radius:0 0 12px 12px; padding:28px; box-shadow:0 4px 20px rgba(0,0,0,.08); }}
  .header {{ font-size:20px; font-weight:700; color:#171a35; margin-bottom:18px; padding-bottom:14px; border-bottom:2px solid #f1f5f9; }}
  .body {{ font-size:14px; color:#374151; line-height:1.7; }}
  .ticket-card {{ background:#f8fafc; border:1px solid #e2e8f0; border-radius:10px; padding:16px; margin:16px 0; }}
  .tc-no {{ font-family:monospace; font-size:12px; color:#3b82f6; font-weight:700; margin-bottom:4px; }}
  .tc-subject {{ font-size:15px; font-weight:600; color:#1e293b; margin-bottom:8px; }}
  .tc-meta {{ font-size:12px; color:#64748b; }}
  .p-badge {{ padding:2px 8px; border-radius:20px; font-size:11px; font-weight:700; }}
  .p-critical {{ background:#fee2e2; color:#dc2626; }}
  .p-high    {{ background:#fef3c7; color:#d97706; }}
  .p-medium  {{ background:#e0f2fe; color:#0369a1; }}
  .p-low     {{ background:#f1f5f9; color:#64748b; }}
  .btn {{ display:inline-block; background:#171a35; color:#fff; padding:12px 24px; border-radius:8px; text-decoration:none; font-weight:600; font-size:14px; margin:16px 0; }}
  .status-row {{ display:flex; align-items:center; gap:12px; margin:16px 0; font-size:14px; }}
  .status {{ padding:6px 14px; border-radius:20px; font-weight:600; background:#f1f5f9; color:#374151; }}
  .status.new {{ background:#dcfce7; color:#15803d; }}
  .arrow {{ font-size:20px; color:#94a3b8; }}
  .footer-note {{ font-size:12px; color:#94a3b8; margin-top:16px; }}
  hr {{ border:none; border-top:1px solid #f1f5f9; margin:20px 0; }}
  .footer {{ text-align:center; font-size:11px; color:#94a3b8; padding:16px 0 0; }}
</style></head><body>
<div class='wrap'>
  <div class='logo'><span class='logo-text'>🎫 PINUS TICKET SYSTEM</span></div>
  <div class='card'>
    {content}
    <hr/>
    <div class='footer'>
      Pinus Software Solutions Pvt. Ltd. &bull; ticketing.pinussoftware.cloud<br/>
      This is an automated notification. Please do not reply to this email.
    </div>
  </div>
</div></body></html>";
}
