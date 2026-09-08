using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using QigloRestaurant.Web.Validation;

namespace QigloRestaurant.Web.ViewModels;

public class AddToCartRequest
{
    [Required]
    public int MenuItemId { get; set; }

    [Range(1, 20)]
    public int Quantity { get; set; } = 1;

    public List<int> SelectedAddOnIds { get; set; } = [];

    [StringLength(300)]
    public string? SpecialInstructions { get; set; }
}

public class UpdateCartQuantityRequest
{
    [Required]
    public string LineKey { get; set; } = string.Empty;

    [Range(1, 20)]
    public int Quantity { get; set; }
}

public class CartViewModel
{
    public int DiningSessionId { get; set; }
    public List<CartLineViewModel> Lines { get; set; } = [];
    public decimal GrandTotal => Lines.Sum(x => x.LineTotal);
    public int TotalQuantity => Lines.Sum(x => x.Quantity);
}

public class CartLineViewModel
{
    public string LineKey { get; set; } = string.Empty;
    public int MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public string? Photo { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? SpecialInstructions { get; set; }
    public List<CartAddOnViewModel> AddOns { get; set; } = [];
    public decimal AddOnUnitTotal => AddOns.Sum(x => x.ExtraPrice);
    public decimal LineTotal => (UnitPrice + AddOnUnitTotal) * Quantity;
}

public class CartAddOnViewModel
{
    public int AddOnId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal ExtraPrice { get; set; }
}

public class MenuCatalogViewModel
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public List<CategoryFilterViewModel> Categories { get; set; } = [];
    public List<MenuCardViewModel> Items { get; set; } = [];
    public List<MenuCardViewModel> PopularItems { get; set; } = [];
    public int TotalMenuItems { get; set; }
    public int AvailableMenuItems { get; set; }
}

public class CategoryFilterViewModel
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class MenuCardViewModel
{
    public int MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
    public string? PrimaryPhoto { get; set; }
    public int PopularQuantity { get; set; }
    public bool IsRecommended { get; set; }
}

public class MenuDetailsViewModel
{
    public int MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; }
    public List<MenuPhotoViewModel> Photos { get; set; } = [];
    public List<AddOnChoiceViewModel> AddOns { get; set; } = [];
}

public class MenuPhotoViewModel
{
    public string FileName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

public class AddOnChoiceViewModel
{
    public int AddOnId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal ExtraPrice { get; set; }
}

public class MenuItemFormViewModel
{
    public int? MenuItemId { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    [Display(Name = "Food Name")]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(1000, MinimumLength = 5)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(typeof(decimal), "0.01", "9999.99")]
    public decimal Price { get; set; }

    [Display(Name = "Available")]
    public bool IsAvailable { get; set; } = true;

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a category.")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [AllowedFileExtensions(".jpg", ".jpeg", ".png", ".webp")]
    [MaxFileSize(5 * 1024 * 1024)]
    [Display(Name = "Food Photos")]
    public List<IFormFile> Photos { get; set; } = [];

    public IEnumerable<SelectListItem> Categories { get; set; } = [];
    public List<ExistingPhotoViewModel> ExistingPhotos { get; set; } = [];
    public List<AddOnInputViewModel> AddOns { get; set; } = [];
}

public class ExistingPhotoViewModel
{
    public int MenuItemPhotoId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

public class AddOnInputViewModel
{
    public int? AddOnId { get; set; }

    [Required, StringLength(80, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.00", "999.99")]
    [Display(Name = "Extra Price")]
    public decimal ExtraPrice { get; set; }

    public bool IsAvailable { get; set; } = true;
}
