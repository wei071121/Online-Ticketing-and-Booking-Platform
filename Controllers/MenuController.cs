using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QigloRestaurant.Web.Data;
using QigloRestaurant.Web.Models;
using QigloRestaurant.Web.ViewModels;

namespace QigloRestaurant.Web.Controllers;

public class MenuController(DB db) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index(string? search, int? categoryId)
    {
        var query = db.MenuItems
            .AsNoTracking()
            .Where(x => x.Category.IsActive)
            .Include(x => x.Category)
            .Include(x => x.Photos)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(x =>
                x.Name.Contains(search) ||
                (x.Description != null && x.Description.Contains(search)) ||
                x.Category.Name.Contains(search));
        }

        if (categoryId.HasValue)
            query = query.Where(x => x.CategoryId == categoryId.Value);

        var items = await query
            .OrderBy(x => x.Category.Name)
            .ThenBy(x => x.Name)
            .Select(x => new MenuCardViewModel
            {
                MenuItemId = x.MenuItemId,
                Name = x.Name,
                Description = x.Description ?? string.Empty,
                CategoryName = x.Category.Name,
                Price = x.Price,
                IsAvailable = x.IsAvailable,
                PrimaryPhoto = x.Photos
                    .OrderByDescending(p => p.IsPrimary)
                    .ThenBy(p => p.DisplayOrder)
                    .Select(p => p.FileName)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var popularRows = await db.OrderItems
            .AsNoTracking()
            .GroupBy(x => x.MenuItemId)
            .Select(g => new { MenuItemId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .OrderByDescending(x => x.Quantity)
            .Take(4)
            .ToListAsync();

        var popularQuantities = popularRows.ToDictionary(x => x.MenuItemId, x => x.Quantity);
        var popularIds = popularRows.Select(x => x.MenuItemId).ToList();
        var popularEntities = await db.MenuItems
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Photos)
            .Where(x => popularIds.Contains(x.MenuItemId) && x.IsAvailable && x.Category.IsActive)
            .ToListAsync();

        var popular = popularEntities
            .OrderByDescending(x => popularQuantities[x.MenuItemId])
            .Select(x => new MenuCardViewModel
            {
                MenuItemId = x.MenuItemId,
                Name = x.Name,
                Description = x.Description ?? string.Empty,
                CategoryName = x.Category.Name,
                Price = x.Price,
                IsAvailable = x.IsAvailable,
                PopularQuantity = popularQuantities[x.MenuItemId],
                PrimaryPhoto = x.Photos
                    .OrderByDescending(p => p.IsPrimary)
                    .ThenBy(p => p.DisplayOrder)
                    .Select(p => p.FileName)
                    .FirstOrDefault()
            })
            .ToList();

        // A fresh database only has a small amount of order history. Keep the
        // section visually useful by filling the remaining slots with available items.
        if (popular.Count < 4 && string.IsNullOrWhiteSpace(search) && !categoryId.HasValue)
        {
            var usedIds = popular.Select(x => x.MenuItemId).ToHashSet();
            popular.AddRange(items
                .Where(x => x.IsAvailable && !usedIds.Contains(x.MenuItemId))
                .Take(4 - popular.Count)
                .Select(x => new MenuCardViewModel
                {
                    MenuItemId = x.MenuItemId,
                    Name = x.Name,
                    Description = x.Description,
                    CategoryName = x.CategoryName,
                    Price = x.Price,
                    IsAvailable = x.IsAvailable,
                    PrimaryPhoto = x.PrimaryPhoto,
                    IsRecommended = true
                }));
        }

        var categories = await db.Categories
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new CategoryFilterViewModel
            {
                CategoryId = x.CategoryId,
                Name = x.Name
            })
            .ToListAsync();

        var vm = new MenuCatalogViewModel
        {
            Search = search,
            CategoryId = categoryId,
            Items = items,
            PopularItems = popular,
            Categories = categories,
            TotalMenuItems = await db.MenuItems.AsNoTracking().CountAsync(x => x.Category.IsActive),
            AvailableMenuItems = await db.MenuItems.AsNoTracking().CountAsync(x => x.Category.IsActive && x.IsAvailable)
        };

        var isAjax = string.Equals(
            Request.Headers["X-Requested-With"].ToString(),
            "XMLHttpRequest",
            StringComparison.OrdinalIgnoreCase);

        return isAjax ? PartialView("_MenuResults", vm) : View(vm);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var item = await db.MenuItems
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Photos)
            .Include(x => x.MenuItemAddOns)
                .ThenInclude(x => x.AddOn)
            .FirstOrDefaultAsync(x => x.MenuItemId == id && x.Category.IsActive);

        if (item is null)
            return NotFound();

        var vm = new MenuDetailsViewModel
        {
            MenuItemId = item.MenuItemId,
            Name = item.Name,
            Description = item.Description ?? string.Empty,
            CategoryName = item.Category.Name,
            Price = item.Price,
            IsAvailable = item.IsAvailable,
            Photos = item.Photos
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.DisplayOrder)
                .Select(x => new MenuPhotoViewModel
                {
                    FileName = x.FileName,
                    IsPrimary = x.IsPrimary
                })
                .ToList(),
            AddOns = item.MenuItemAddOns
                .Where(x => x.AddOn.IsActive)
                .Select(x => x.AddOn)
                .OrderBy(x => x.Price)
                .ThenBy(x => x.Name)
                .Select(x => new AddOnChoiceViewModel
                {
                    AddOnId = x.AddOnId,
                    Name = x.Name,
                    ExtraPrice = x.Price
                })
                .ToList()
        };

        return View(vm);
    }
}
