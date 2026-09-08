using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QigloRestaurant.Models.Entities;

public class LoginAttempt
{
    public int LoginAttemptId { get; set; }

    public int? UserId { get; set; }

    [Required, EmailAddress, StringLength(150)]
    public string EmailEntered { get; set; } = string.Empty;

    public bool IsSuccessful { get; set; }

    [StringLength(45)]
    public string? IpAddress { get; set; }

    public DateTime AttemptedAt { get; set; } = DateTime.Now;

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}

