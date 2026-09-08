using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QigloRestaurant.Models.Entities;

public class MenuItemPhoto
{
    public int MenuItemPhotoId { get; set; }

    public int MenuItemId { get; set; }

    [Required, StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [StringLength(150)]
    public string? AltText { get; set; }

    [Range(0, 20)]
    public int DisplayOrder { get; set; }

    public bool IsPrimary { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.Now;

    [ForeignKey(nameof(MenuItemId))]
    public MenuItem MenuItem { get; set; } = null!;
}

