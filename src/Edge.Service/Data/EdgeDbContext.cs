using Microsoft.EntityFrameworkCore;
using Edge.Service.Data.Entities;

namespace Edge.Service.Data;

public class EdgeDbContext : DbContext
{
    public EdgeDbContext(DbContextOptions<EdgeDbContext> options) : base(options) { }

    public DbSet<Company> Companies { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Area> Areas { get; set; }
    public DbSet<Device> Devices { get; set; }
    public DbSet<EdgeSyncLog> EdgeSyncLogs { get; set; }
    public DbSet<EdgeSyncTable> EdgeSyncTables { get; set; }
    public DbSet<EdgeSyncState> EdgeSyncStates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Address).IsRequired().HasMaxLength(500);
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Area>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MacAddress).IsRequired().HasMaxLength(17);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Model).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<EdgeSyncLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ManifestId).IsRequired().HasMaxLength(26);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ErrorText).HasMaxLength(2000);
            entity.HasIndex(e => e.ManifestId);
            entity.HasIndex(e => e.StartedAt);
        });

        modelBuilder.Entity<EdgeSyncTable>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TableName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Sha256).IsRequired().HasMaxLength(64);
            entity.HasOne(e => e.EdgeSyncLog).WithMany(e => e.Tables).HasForeignKey(e => e.EdgeSyncLogId);
        });

        modelBuilder.Entity<EdgeSyncState>(entity =>
        {
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Value).IsRequired();
        });
    }
}