using Microsoft.EntityFrameworkCore;
using PinusTickets.Models;

namespace PinusTickets.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User>         Users         => Set<User>();
    public DbSet<Customer>     Customers     => Set<Customer>();
    public DbSet<Application>  Applications  => Set<Application>();
    public DbSet<Contract>     Contracts     => Set<Contract>();
    public DbSet<Ticket>       Tickets       => Set<Ticket>();
    public DbSet<TicketComment>    TicketComments    => Set<TicketComment>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<TicketHistory>    TicketHistory     => Set<TicketHistory>();
    public DbSet<TimeEntry>        TimeEntries       => Set<TimeEntry>();
    public DbSet<TestResult>       TestResults       => Set<TestResult>();
    public DbSet<Notification>     Notifications     => Set<Notification>();
    public DbSet<RolePermission>   RolePermissions   => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Ticket → Assignee (nullable FK, no cascade delete)
        b.Entity<Ticket>()
            .HasOne(t => t.Assignee)
            .WithMany(u => u.AssignedTickets)
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);

        // Ticket → Creator
        b.Entity<Ticket>()
            .HasOne(t => t.Creator)
            .WithMany()
            .HasForeignKey(t => t.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // TicketComment → Author
        b.Entity<TicketComment>()
            .HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // TicketHistory → Actor
        b.Entity<TicketHistory>()
            .HasOne(h => h.Actor)
            .WithMany()
            .HasForeignKey(h => h.ActorId)
            .OnDelete(DeleteBehavior.Restrict);

        // TimeEntry → User
        b.Entity<TimeEntry>()
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // TestResult → Tester
        b.Entity<TestResult>()
            .HasOne(r => r.Tester)
            .WithMany()
            .HasForeignKey(r => r.TesterId)
            .OnDelete(DeleteBehavior.Restrict);

        // Customer → PrimaryContact: ignore cycle (manual FK, no nav)
        b.Entity<Customer>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.PrimaryContactId)
            .OnDelete(DeleteBehavior.SetNull);

        // Unique email
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
        // Unique ticket number
        b.Entity<Ticket>().HasIndex(t => t.TicketNo).IsUnique();
    }
}
