using Microsoft.EntityFrameworkCore;
using Central.Api.Data.Entities;

namespace Central.Api.Data;

public class CentralDbContext : DbContext
{
    public CentralDbContext(DbContextOptions<CentralDbContext> options) : base(options) { }

    public DbSet<Company> Companies { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Area> Areas { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<SyncRequest> SyncRequests { get; set; }
    public DbSet<SyncManifest> SyncManifests { get; set; }
    public DbSet<SyncAcknowledgement> SyncAcknowledgements { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Name);
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
            entity.HasOne(e => e.Company).WithMany(e => e.Locations).HasForeignKey(e => e.CompanyId);
            entity.HasIndex(e => e.CompanyId);
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasOne(e => e.Location).WithMany(e => e.Groups).HasForeignKey(e => e.LocationId);
            entity.HasIndex(e => e.LocationId);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.Group).WithMany(e => e.Users).HasForeignKey(e => e.GroupId);
            entity.HasIndex(e => e.GroupId);
            entity.HasIndex(e => e.Email);
        });

        modelBuilder.Entity<Area>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.Location).WithMany(e => e.Areas).HasForeignKey(e => e.LocationId);
            entity.HasIndex(e => e.LocationId);
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MacAddress).IsRequired().HasMaxLength(17);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Model).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.Location).WithMany(e => e.Devices).HasForeignKey(e => e.LocationId);
            entity.HasIndex(e => e.MacAddress).IsUnique();
            entity.HasIndex(e => e.LocationId);
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EventData).IsRequired();
            entity.HasOne(e => e.Area).WithMany().HasForeignKey(e => e.AreaId);
            entity.HasIndex(e => e.AreaId);
            entity.HasIndex(e => e.ScheduledTimeUtc);
        });

        modelBuilder.Entity<SyncRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Mac).IsRequired().HasMaxLength(17);
            entity.Property(e => e.ManifestId).IsRequired().HasMaxLength(26);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.HasIndex(e => e.Mac);
            entity.HasIndex(e => e.ManifestId);
            entity.HasIndex(e => e.RequestedAt);
        });

        modelBuilder.Entity<SyncManifest>(entity =>
        {
            entity.HasKey(e => new { e.ManifestId, e.TableName });
            entity.Property(e => e.ManifestId).IsRequired().HasMaxLength(26);
            entity.Property(e => e.TableName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Sha256).IsRequired().HasMaxLength(64);
            entity.Property(e => e.FilterDesc).HasMaxLength(1000);
            entity.HasIndex(e => e.ManifestId);
            entity.HasIndex(e => e.GeneratedAt);
        });

        modelBuilder.Entity<SyncAcknowledgement>(entity =>
        {
            entity.HasKey(e => new { e.ManifestId, e.Mac });
            entity.Property(e => e.ManifestId).IsRequired().HasMaxLength(26);
            entity.Property(e => e.Mac).IsRequired().HasMaxLength(17);
            entity.Property(e => e.Result).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DeviceCountsJson).IsRequired();
            entity.Property(e => e.DeviceHashesJson).IsRequired();
            entity.Property(e => e.ErrorText).HasMaxLength(2000);
            entity.HasIndex(e => e.ManifestId);
            entity.HasIndex(e => e.CompletedAt);
        });
    }
}