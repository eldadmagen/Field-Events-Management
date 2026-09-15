using FieldEvents.Server.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FieldEvents.Server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<FieldEvent> Events => Set<FieldEvent>();
    public DbSet<EventStatusHistory> EventStatusHistories => Set<EventStatusHistory>();
    public DbSet<User> Users => Set<User>();
    public DbSet<EventComment> EventComments => Set<EventComment>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FieldEvent>(e =>
        {
            e.HasIndex(x => x.OutboxId).IsUnique();
            e.HasMany(x => x.History)
                .WithOne()
                .HasForeignKey(h => h.FieldEventId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Comments)
                .WithOne()
                .HasForeignKey(c => c.FieldEventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>().HasIndex(u => u.UserName).IsUnique();
    }
}
