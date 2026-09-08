using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QigloRestaurant.Models.Entities;

[Index(nameof(BookingCode), IsUnique = true)]
public class Reservation
{
    public int ReservationId { get; set; }

    public int CustomerId { get; set; }
    public int? DiningTableId { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime StartAt { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime EndAt { get; set; }

    [Range(1, 30)]
    public int Pax { get; set; }

    [Required, StringLength(20)]
    [RegularExpression("^(Pending|Confirmed|Rejected|Cancelled|CheckedIn|Completed)$")]
    public string Status { get; set; } = "Pending";

    [Required, StringLength(20)]
    public string BookingCode { get; set; } = string.Empty;

    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey(nameof(CustomerId))]
    public User Customer { get; set; } = null!;

    [ForeignKey(nameof(DiningTableId))]
    public DiningTable? DiningTable { get; set; }

    [ForeignKey(nameof(ApprovedByUserId))]
    public User? ApprovedByUser { get; set; }

    public DiningSession? DiningSession { get; set; }
}

