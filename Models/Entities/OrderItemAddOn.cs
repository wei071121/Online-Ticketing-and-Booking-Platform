using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QigloRestaurant.Models.Entities;

[Index(nameof(OrderItemId), nameof(AddOnId), IsUnique = true)]
public class OrderItemAddOn
{
    public int OrderItemAddOnId { get; set; }

    public int OrderItemId { get; set; }
    public int AddOnId { get; set; }

    [Required, StringLength(80)]
    public string AddOnName { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.00", "999.99")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    [ForeignKey(nameof(OrderItemId))]
    public OrderItem OrderItem { get; set; } = null!;

    [ForeignKey(nameof(AddOnId))]
    public AddOn AddOn { get; set; } = null!;
}

