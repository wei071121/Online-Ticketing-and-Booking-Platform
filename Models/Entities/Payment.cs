using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QigloRestaurant.Models.Entities;

[Index(nameof(DiningSessionId), IsUnique = true)]
[Index(nameof(ReceiptNumber), IsUnique = true)]
public class Payment
{
    public int PaymentId { get; set; }

    public int DiningSessionId { get; set; }
    public int? ProcessedByUserId { get; set; }

    [Required, StringLength(30)]
    public string ReceiptNumber { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal ServiceCharge { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal AmountReceived { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal ChangeAmount { get; set; }

    [Required, StringLength(20)]
    [RegularExpression("^(Cash|Card|EWallet)$")]
    public string PaymentMethod { get; set; } = "Cash";

    [Required, StringLength(20)]
    [RegularExpression("^(Pending|Paid|Refunded|Cancelled)$")]
    public string Status { get; set; } = "Pending";

    public DateTime? PaidAt { get; set; }

    [ForeignKey(nameof(DiningSessionId))]
    public DiningSession DiningSession { get; set; } = null!;

    [ForeignKey(nameof(ProcessedByUserId))]
    public User? ProcessedByUser { get; set; }
}

