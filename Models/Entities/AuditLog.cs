using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QigloRestaurant.Models.Entities;

public class AuditLog
{
    public int AuditLogId { get; set; }

    public int? UserId { get; set; }

    [Required, StringLength(100)]
    public string Action { get; set; } = string.Empty;

    [StringLength(100)]
    public string? EntityName { get; set; }

    [StringLength(50)]
    public string? EntityId { get; set; }

    [StringLength(1000)]
    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}

