using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QigloRestaurant.Models.Entities;

[Index(nameof(OrderNumber), IsUnique = true)]
public class Order
{
    public int OrderId { get; set; }

    public int DiningSessionId { get; set; }
    public int? CreatedByUserId { get; set; }

    [Required, StringLength(25)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required, StringLength(20)]
    [RegularExpression("^(Pending|Preparing|Ready|Served|Cancelled)$")]
    public string Status { get; set; } = "Pending";

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime OrderedAt { get; set; } = DateTime.Now;
    public DateTime? ServedAt { get; set; }

    [ForeignKey(nameof(DiningSessionId))]
    public DiningSession DiningSession { get; set; } = null!;

    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

