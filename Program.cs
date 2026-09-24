using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PinusTickets.Data;
using PinusTickets.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── JWT Auth ──────────────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
    });
builder.Services.AddAuthorization();

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<TicketService>();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(opt => opt.AddPolicy("AllowUI", p =>
    p.WithOrigins(
        "http://localhost:4200",
        "https://tickets.pinussoftware.cloud")
     .AllowAnyHeader()
     .AllowAnyMethod()));

builder.Services.AddControllers();

var app = builder.Build();

// ── Migrate + Seed ────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await SeedAsync(db);
}

app.UseCors("AllowUI");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

// ── Seed default org + admin ──────────────────────────────────────────────────
static async Task SeedAsync(AppDbContext db)
{
    if (!db.Organizations.Any())
    {
        db.Organizations.Add(new PinusTickets.Models.Organization
        {
            Name = "Pinus Software Solutions",
            Code = "PINUS",
        });
        await db.SaveChangesAsync();
    }

    if (!db.Users.Any())
    {
        var org = db.Organizations.First();
        db.Users.Add(new PinusTickets.Models.User
        {
            OrganizationId = org.Id,
            Name           = "Admin",
            Email          = "admin@pinussoftware.com",
            PasswordHash   = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Role           = "Admin",
        });
        await db.SaveChangesAsync();
    }
}
