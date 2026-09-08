using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace QigloRestaurant.Models.Entities;

[Index(nameof(MenuItemId), nameof(AddOnId), IsUnique = true)]
public class MenuItemAddOn
{
    public int MenuItemAddOnId { get; set; }

    public int MenuItemId { get; set; }
    public int AddOnId { get; set; }

    [ForeignKey(nameof(MenuItemId))]
    public MenuItem MenuItem { get; set; } = null!;

    [ForeignKey(nameof(AddOnId))]
    public AddOn AddOn { get; set; } = null!;
}

