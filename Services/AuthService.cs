using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PinusTickets.Data;
using PinusTickets.DTOs;
using PinusTickets.Models;

namespace PinusTickets.Services;

public class AuthService(AppDbContext db, IConfiguration cfg)
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest req)
    {
        var user = await db.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Email == req.Email && u.Status == "active");

        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return null;

        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var token = GenerateToken(user);
        return new LoginResponse(user.Id, user.Name, user.Email, user.Role,
                                 user.Organization?.Name ?? "", token);
    }

    private string GenerateToken(User user)
    {
        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Key"]!));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(12);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier,     user.Id.ToString()),
            new Claim(ClaimTypes.Email,              user.Email),
            new Claim(ClaimTypes.Name,               user.Name),
            new Claim(ClaimTypes.Role,               user.Role),
            new Claim("org_id",                      user.OrganizationId.ToString()),
            new Claim("customer_id",                 user.CustomerId?.ToString() ?? ""),
        };

        var token = new JwtSecurityToken(
            issuer:   cfg["Jwt:Issuer"],
            audience: cfg["Jwt:Audience"],
            claims:   claims,
            expires:  expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
