using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QigloRestaurant.Models.Entities;

[Index(nameof(TokenHash), IsUnique = true)]
public class PasswordResetToken
{
    public int PasswordResetTokenId { get; set; }

    public int UserId { get; set; }

    [Required, StringLength(255)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}

