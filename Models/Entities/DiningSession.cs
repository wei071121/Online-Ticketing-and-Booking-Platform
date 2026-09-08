using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QigloRestaurant.Models.Entities;

[Index(nameof(ReservationId), IsUnique = true)]
public class DiningSession
{
    public int DiningSessionId { get; set; }

    public int DiningTableId { get; set; }
    public int? ReservationId { get; set; }
    public int? CustomerId { get; set; }

    [Range(1, 30)]
    public int Pax { get; set; }

    [Required, StringLength(20)]
    // Payment has its own status. Keep the dining-session lifecycle separate
    // so every declared state is used by the application flow.
    [RegularExpression("^(Active|Completed|Cancelled)$")]
    public string Status { get; set; } = "Active";

    public DateTime StartedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }

    [ForeignKey(nameof(DiningTableId))]
    public DiningTable DiningTable { get; set; } = null!;

    [ForeignKey(nameof(ReservationId))]
    public Reservation? Reservation { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public User? Customer { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public Payment? Payment { get; set; }
}
