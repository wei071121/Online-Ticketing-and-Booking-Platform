using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QigloRestaurant.Models.Entities;

public class OrderItem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }
    public int MenuItemId { get; set; }

    [Required, StringLength(100)]
    public string MenuItemName { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999.99")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    [Range(1, 20)]
    public int Quantity { get; set; }

    [StringLength(300)]
    public string? SpecialRequest { get; set; }

    [NotMapped]
    public decimal LineSubtotal =>
        Quantity * (UnitPrice + OrderItemAddOns.Sum(item => item.UnitPrice));

    [ForeignKey(nameof(OrderId))]
    public Order Order { get; set; } = null!;

    [ForeignKey(nameof(MenuItemId))]
    public MenuItem MenuItem { get; set; } = null!;

    public ICollection<OrderItemAddOn> OrderItemAddOns { get; set; } = new List<OrderItemAddOn>();
}

