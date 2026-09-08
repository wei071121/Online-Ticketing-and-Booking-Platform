using System.ComponentModel.DataAnnotations;

namespace QigloRestaurant.Web.Models;

// ============================================================================
// Table, reservation and walk-in view models.
// Kept in a focused file so each business area remains easy to locate.
// ============================================================================

// Table management -------------------------------------------------------

public class TableFormViewModel
{
    public int DiningTableId { get; set; }

    [Required, StringLength(20)]
    [RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "Use letters, numbers and hyphens only.")]
    [Display(Name = "Table number")]
    public string TableNumber { get; set; } = string.Empty;

    [Range(1, 30)]
    public int Capacity { get; set; } = 2;

    [Required, StringLength(20)]
    [RegularExpression("^(Available|Occupied|Unavailable)$")]
    public string Status { get; set; } = "Available";

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}

// Availability checking ----------------------------------------------------

public class AvailableTableOption
{
    public int DiningTableId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public int Capacity { get; set; }
}

public class AvailableTimeSlotOption
{
    public string ReservationTime { get; set; } = string.Empty;
    public string DisplayTime { get; set; } = string.Empty;
    public int AvailableTableCount { get; set; }
}

// Reservation ----------------------------------------------------------------

public class CreateReservationViewModel
{
    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Reservation date")]
    public DateTime ReservationDate { get; set; } = DateTime.Today.AddDays(1);

    [Required]
    [Display(Name = "Reservation time")]
    public string ReservationTime { get; set; } = string.Empty;

    [Range(1, 30)]
    public int Pax { get; set; } = 2;

    [Range(30, 240)]
    [Display(Name = "Duration (minutes)")]
    public int DurationMinutes { get; set; } = 120;

    public int? DiningTableId { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class CheckInViewModel
{
    [Required, StringLength(20)]
    [Display(Name = "Booking code")]
    public string BookingCode { get; set; } = string.Empty;
}

public class RejectReservationViewModel
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    [StringLength(250)]
    public string? Reason { get; set; }

    public string? ReturnUrl { get; set; }
}

public class WalkInViewModel
{
    [Required(ErrorMessage = "Please select a table.")]
    [Display(Name = "Table")]
    public int? DiningTableId { get; set; }

    [Range(1, 30)]
    public int Pax { get; set; } = 2;
}
