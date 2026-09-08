using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace QigloRestaurant.Models.Entities;

[Index(nameof(Name), IsUnique = true)]
public class Category
{
    public int CategoryId { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
}

