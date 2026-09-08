using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QigloRestaurant.Models.Entities;

[Index(nameof(CategoryId), nameof(Name), IsUnique = true)]
public class MenuItem
{
    public int MenuItemId { get; set; }

    public int CategoryId { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0.01", "9999.99")]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    public bool IsAvailable { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category Category { get; set; } = null!;

    public ICollection<MenuItemPhoto> Photos { get; set; } = new List<MenuItemPhoto>();
    public ICollection<MenuItemAddOn> MenuItemAddOns { get; set; } = new List<MenuItemAddOn>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

