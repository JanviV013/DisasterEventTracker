using DisasterEventTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace DisasterEventTracker.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<SavedEvent> SavedEvents => Set<SavedEvent>();
    public DbSet<WatchListItem> WatchListItems => Set<WatchListItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // One account per email address
        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<SavedEvent>(entity =>
        {
            // Store the enums as readable text ("High", "Closed") instead of numbers
            entity.Property(e => e.Priority).HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

            // A user can save the same earthquake only once
            entity.HasIndex(e => new { e.UserId, e.EarthquakeId }).IsUnique();

            // One user has many saved events; deleting a user deletes their saved events
            entity.HasOne(e => e.User)
                  .WithMany(u => u.SavedEvents)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WatchListItem>(entity =>
        {
            entity.HasOne(w => w.User)
                  .WithMany(u => u.WatchListItems)
                  .HasForeignKey(w => w.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
