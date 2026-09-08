using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QigloRestaurant.Models.Entities;

[Index(nameof(Name), IsUnique = true)]
public class AddOn
{
    public int AddOnId { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.00", "999.99")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<MenuItemAddOn> MenuItemAddOns { get; set; } = new List<MenuItemAddOn>();
    public ICollection<OrderItemAddOn> OrderItemAddOns { get; set; } = new List<OrderItemAddOn>();
}

