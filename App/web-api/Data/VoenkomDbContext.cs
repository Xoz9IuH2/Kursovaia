using Microsoft.EntityFrameworkCore;
using web_api.Models;

namespace web_api.Data;

public class VoenkomDbContext : DbContext
{
    public VoenkomDbContext(DbContextOptions<VoenkomDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<PersonalFile> PersonalFiles { get; set; }
    public DbSet<Summon> Summons { get; set; }
    public DbSet<Application> Applications { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<CalendarEvent> CalendarEvents { get; set; }
    public DbSet<Evader> Evaders { get; set; }
    public DbSet<GeoLocation> GeoLocations { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<ApplicationStatusHistory> ApplicationStatusHistories { get; set; }
    public DbSet<TimeSlot> TimeSlots { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Login).IsUnique();
            entity.Property(u => u.Role).IsRequired();
            
            entity.HasOne(u => u.PersonalFile)
                .WithMany()
                .HasForeignKey(u => u.PersonalFileId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PersonalFile>(entity =>
        {
            entity.HasIndex(p => new { p.LastName, p.FirstName, p.BirthDate });
            entity.HasOne(p => p.AssignedEmployee)
                .WithMany()
                .HasForeignKey(p => p.AssignedEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Summon>(entity =>
        {
            entity.HasOne(s => s.PersonalFile)
                .WithMany()
                .HasForeignKey(s => s.PersonalFileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(s => s.CreatedBy)
                .WithMany()
                .HasForeignKey(s => s.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasOne(a => a.PersonalFile)
                .WithMany()
                .HasForeignKey(a => a.PersonalFileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(a => a.ReviewedBy)
                .WithMany()
                .HasForeignKey(a => a.ReviewedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasOne(a => a.PersonalFile)
                .WithMany()
                .HasForeignKey(a => a.PersonalFileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(a => a.AssignedEmployee)
                .WithMany()
                .HasForeignKey(a => a.AssignedEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(n => n.PersonalFile)
                .WithMany()
                .HasForeignKey(n => n.PersonalFileId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CalendarEvent>(entity =>
        {
            entity.HasOne(c => c.CreatedBy)
                .WithMany()
                .HasForeignKey(c => c.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Evader>(entity =>
        {
            entity.HasIndex(e => e.ProtocolNumber).IsUnique();
            entity.HasOne(e => e.PersonalFile)
                .WithMany()
                .HasForeignKey(e => e.PersonalFileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasOne(d => d.PersonalFile)
                .WithMany()
                .HasForeignKey(d => d.PersonalFileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.UploadedBy)
                .WithMany()
                .HasForeignKey(d => d.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ApplicationStatusHistory>(entity =>
        {
            entity.HasOne(h => h.Application)
                .WithMany()
                .HasForeignKey(h => h.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(h => h.ChangedBy)
                .WithMany()
                .HasForeignKey(h => h.ChangedById)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
