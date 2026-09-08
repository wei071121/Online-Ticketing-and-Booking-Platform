using Microsoft.EntityFrameworkCore;
using QigloRestaurant.Models.Entities;

namespace QigloRestaurant.Web.Models;

// Lecturer practical pattern: one clearly named DB context under Models.
public class DB(DbContextOptions<DB> options) : DbContext(options)
{
    // DB Sets ----------------------------------------------------------------
    public DbSet<User> Users => Set<User>();
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<DiningTable> DiningTables => Set<DiningTable>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<DiningSession> DiningSessions => Set<DiningSession>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MenuItemPhoto> MenuItemPhotos => Set<MenuItemPhoto>();
    public DbSet<AddOn> AddOns => Set<AddOn>();
    public DbSet<MenuItemAddOn> MenuItemAddOns => Set<MenuItemAddOn>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemAddOn> OrderItemAddOns => Set<OrderItemAddOn>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // The practicals use Data Annotations for entity design. These delete
        // rules are the necessary exception to avoid SQL Server multiple-cascade
        // paths and to protect completed order/payment history.
        modelBuilder.Entity<Reservation>()
            .HasOne(item => item.Customer)
            .WithMany(user => user.Reservations)
            .HasForeignKey(item => item.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Reservation>()
            .HasOne(item => item.ApprovedByUser)
            .WithMany()
            .HasForeignKey(item => item.ApprovedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<DiningSession>()
            .HasOne(item => item.Reservation)
            .WithOne(item => item.DiningSession)
            .HasForeignKey<DiningSession>(item => item.ReservationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DiningSession>()
            .HasOne(item => item.Customer)
            .WithMany()
            .HasForeignKey(item => item.CustomerId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<DiningSession>()
            .HasOne(item => item.DiningTable)
            .WithMany(item => item.DiningSessions)
            .HasForeignKey(item => item.DiningTableId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(item => item.DiningSession)
            .WithMany(item => item.Orders)
            .HasForeignKey(item => item.DiningSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(item => item.CreatedByUser)
            .WithMany()
            .HasForeignKey(item => item.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<OrderItem>()
            .HasOne(item => item.MenuItem)
            .WithMany(item => item.OrderItems)
            .HasForeignKey(item => item.MenuItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MenuItemAddOn>()
            .HasOne(item => item.AddOn)
            .WithMany(item => item.MenuItemAddOns)
            .HasForeignKey(item => item.AddOnId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderItemAddOn>()
            .HasOne(item => item.AddOn)
            .WithMany(item => item.OrderItemAddOns)
            .HasForeignKey(item => item.AddOnId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(item => item.DiningSession)
            .WithOne(item => item.Payment)
            .HasForeignKey<Payment>(item => item.DiningSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Payment>()
            .HasOne(item => item.ProcessedByUser)
            .WithMany()
            .HasForeignKey(item => item.ProcessedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LoginAttempt>()
            .HasOne(item => item.User)
            .WithMany(item => item.LoginAttempts)
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AuditLog>()
            .HasOne(item => item.User)
            .WithMany(item => item.AuditLogs)
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
