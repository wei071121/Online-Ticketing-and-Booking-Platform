using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace QigloRestaurant.Models.Entities;

[Index(nameof(TableNumber), IsUnique = true)]
public class DiningTable
{
    public int DiningTableId { get; set; }

    [Required, StringLength(20)]
    [RegularExpression("^[A-Za-z0-9-]+$")]
    public string TableNumber { get; set; } = string.Empty;

    [Range(1, 30)]
    public int Capacity { get; set; }

    [Required, StringLength(20)]
    [RegularExpression("^(Available|Occupied|Unavailable)$")]
    public string Status { get; set; } = "Available";

    public bool IsActive { get; set; } = true;

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    public ICollection<DiningSession> DiningSessions { get; set; } = new List<DiningSession>();
}
