using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QigloRestaurant.Models.Entities;

[Index(nameof(Email), IsUnique = true)]
public class User
{
    public int UserId { get; set; }

    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string? PhoneNumber { get; set; }

    [Required, StringLength(20)]
    [RegularExpression("^(Customer|Staff|KitchenStaff|Admin)$")]
    public string Role { get; set; } = "Customer";

    public bool IsActive { get; set; } = true;

    [Range(0, 100)]
    public int FailedLoginCount { get; set; }

    public DateTime? LockoutEnd { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [InverseProperty(nameof(Reservation.Customer))]
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<LoginAttempt> LoginAttempts { get; set; } = new List<LoginAttempt>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
