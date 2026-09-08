using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QigloRestaurant.Models.Entities;
using QigloRestaurant.Web.Data;
using QigloRestaurant.Web.Models;
using QigloRestaurant.Web.ViewModels;

namespace QigloRestaurant.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminMenuItemsController(DB db, IWebHostEnvironment environment) : Controller
{
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    private const long MaxPhotoBytes = 5 * 1024 * 1024;
    private const int MaxPhotosPerItem = 6;

    public async Task<IActionResult> Index(string? search, int? categoryId)
    {
        var query = db.MenuItems
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Photos)
            .Include(x => x.MenuItemAddOns)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(x => x.Name.Contains(search) ||
                                     (x.Description != null && x.Description.Contains(search)));
        }

        if (categoryId.HasValue)
            query = query.Where(x => x.CategoryId == categoryId.Value);

        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        ViewBag.Categories = await GetCategoryOptionsAsync(includeInactive: true);

        return View(await query
            .OrderBy(x => x.Category.Name)
            .ThenBy(x => x.Name)
            .ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View(new MenuItemFormViewModel
        {
            Categories = await GetCategoryOptionsAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MenuItemFormViewModel vm)
    {
        await ValidateMenuItemFormAsync(vm);
        if (!ModelState.IsValid)
        {
            vm.Categories = await GetCategoryOptionsAsync();
            return View(vm);
        }

        var entity = new MenuItem
        {
            Name = vm.Name.Trim(),
            Description = vm.Description.Trim(),
            Price = vm.Price,
            IsAvailable = vm.IsAvailable,
            CategoryId = vm.CategoryId,
            CreatedAt = DateTime.Now
        };

        db.MenuItems.Add(entity);
        await db.SaveChangesAsync();

        if (vm.Photos.Count > 0)
            await SaveUploadedPhotosAsync(entity.MenuItemId, vm.Photos);

        await ApplyAddOnsAsync(entity, vm.AddOns);
        AddAudit("Create menu item", entity.MenuItemId, $"Created {entity.Name} at RM {entity.Price:0.00}.");
        await db.SaveChangesAsync();

        TempData["Success"] = "Menu item created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var entity = await db.MenuItems
            .Include(x => x.Photos)
            .Include(x => x.MenuItemAddOns)
                .ThenInclude(x => x.AddOn)
            .FirstOrDefaultAsync(x => x.MenuItemId == id);

        if (entity is null)
            return NotFound();

        return View(new MenuItemFormViewModel
        {
            MenuItemId = entity.MenuItemId,
            Name = entity.Name,
            Description = entity.Description ?? string.Empty,
            Price = entity.Price,
            IsAvailable = entity.IsAvailable,
            CategoryId = entity.CategoryId,
            Categories = await GetCategoryOptionsAsync(includeInactive: true),
            ExistingPhotos = entity.Photos
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.DisplayOrder)
                .Select(x => new ExistingPhotoViewModel
                {
                    MenuItemPhotoId = x.MenuItemPhotoId,
                    FileName = x.FileName,
                    IsPrimary = x.IsPrimary
                }).ToList(),
            AddOns = entity.MenuItemAddOns
                .Select(x => x.AddOn)
                .OrderBy(x => x.Name)
                .Select(x => new AddOnInputViewModel
                {
                    AddOnId = x.AddOnId,
                    Name = x.Name,
                    ExtraPrice = x.Price,
                    IsAvailable = x.IsActive
                }).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MenuItemFormViewModel vm)
    {
        if (vm.MenuItemId != id)
            return BadRequest();

        await ValidateMenuItemFormAsync(vm, id);

        var entity = await db.MenuItems
            .Include(x => x.Photos)
            .Include(x => x.MenuItemAddOns)
                .ThenInclude(x => x.AddOn)
            .FirstOrDefaultAsync(x => x.MenuItemId == id);

        if (entity is null)
            return NotFound();

        if (entity.Photos.Count + vm.Photos.Count > MaxPhotosPerItem)
            ModelState.AddModelError(nameof(vm.Photos), $"A menu item can have at most {MaxPhotosPerItem} photos.");

        if (!ModelState.IsValid)
        {
            vm.Categories = await GetCategoryOptionsAsync(includeInactive: true);
            vm.ExistingPhotos = entity.Photos
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.DisplayOrder)
                .Select(x => new ExistingPhotoViewModel
                {
                    MenuItemPhotoId = x.MenuItemPhotoId,
                    FileName = x.FileName,
                    IsPrimary = x.IsPrimary
                }).ToList();
            return View(vm);
        }

        entity.Name = vm.Name.Trim();
        entity.Description = vm.Description.Trim();
        entity.Price = vm.Price;
        entity.IsAvailable = vm.IsAvailable;
        entity.CategoryId = vm.CategoryId;
        entity.UpdatedAt = DateTime.Now;

        if (vm.Photos.Count > 0)
            await SaveUploadedPhotosAsync(entity.MenuItemId, vm.Photos);

        await ApplyAddOnsAsync(entity, vm.AddOns);
        AddAudit("Update menu item", entity.MenuItemId, $"Updated {entity.Name}; available={entity.IsAvailable}.");
        await db.SaveChangesAsync();

        TempData["Success"] = "Menu item updated successfully.";
        return RedirectToAction(nameof(Index));
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAvailability(int id)
    {
        var entity = await db.MenuItems.FindAsync(id);
        if (entity is null)
            return NotFound();

        entity.IsAvailable = !entity.IsAvailable;
        entity.UpdatedAt = DateTime.Now;
        AddAudit("Toggle menu availability", entity.MenuItemId, $"Set {entity.Name} available={entity.IsAvailable}.");
        await db.SaveChangesAsync();

        TempData["Success"] = entity.IsAvailable
            ? $"{entity.Name} is now available."
            : $"{entity.Name} is now marked sold out.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await db.MenuItems
            .Include(x => x.Photos)
            .Include(x => x.OrderItems)
            .Include(x => x.MenuItemAddOns)
            .FirstOrDefaultAsync(x => x.MenuItemId == id);

        if (entity is null)
            return NotFound();

        if (entity.OrderItems.Any())
        {
            entity.IsAvailable = false;
            AddAudit("Archive menu item", entity.MenuItemId, $"Marked {entity.Name} sold out because order history exists.");
            await db.SaveChangesAsync();
            TempData["Error"] = "This item appears in order history, so it was marked Sold Out instead of being deleted.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var photo in entity.Photos)
            DeletePhysicalPhoto(photo.FileName);

        db.MenuItemAddOns.RemoveRange(entity.MenuItemAddOns);
        db.MenuItems.Remove(entity);
        AddAudit("Delete menu item", entity.MenuItemId, $"Deleted unused menu item {entity.Name}.");
        await db.SaveChangesAsync();

        TempData["Success"] = "Menu item deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto(int photoId)
    {
        var photo = await db.MenuItemPhotos.FindAsync(photoId);
        if (photo is null)
            return NotFound();

        var menuItemId = photo.MenuItemId;
        var wasPrimary = photo.IsPrimary;
        DeletePhysicalPhoto(photo.FileName);
        db.MenuItemPhotos.Remove(photo);
        AddAudit("Delete menu photo", menuItemId, $"Deleted photo #{photoId}.");
        await db.SaveChangesAsync();

        if (wasPrimary)
        {
            var next = await db.MenuItemPhotos
                .Where(x => x.MenuItemId == menuItemId)
                .OrderBy(x => x.DisplayOrder)
                .FirstOrDefaultAsync();
            if (next is not null)
            {
                next.IsPrimary = true;
                await db.SaveChangesAsync();
            }
        }

        TempData["Success"] = "Photo deleted.";
        return RedirectToAction(nameof(Edit), new { id = menuItemId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPrimaryPhoto(int photoId)
    {
        var selected = await db.MenuItemPhotos.FindAsync(photoId);
        if (selected is null)
            return NotFound();

        var photos = await db.MenuItemPhotos
            .Where(x => x.MenuItemId == selected.MenuItemId)
            .ToListAsync();

        foreach (var photo in photos)
            photo.IsPrimary = photo.MenuItemPhotoId == photoId;

        AddAudit("Set primary menu photo", selected.MenuItemId, $"Photo #{photoId} selected as primary.");
        await db.SaveChangesAsync();
        TempData["Success"] = "Primary photo updated.";
        return RedirectToAction(nameof(Edit), new { id = selected.MenuItemId });
    }

    private async Task ValidateMenuItemFormAsync(MenuItemFormViewModel vm, int? currentId = null)
    {
        vm.Name = vm.Name?.Trim() ?? string.Empty;
        vm.Description = vm.Description?.Trim() ?? string.Empty;

        if (!await db.Categories.AnyAsync(x => x.CategoryId == vm.CategoryId))
            ModelState.AddModelError(nameof(vm.CategoryId), "Selected category does not exist.");

        if (await db.MenuItems.AnyAsync(x => x.MenuItemId != currentId && x.Name == vm.Name))
            ModelState.AddModelError(nameof(vm.Name), "A menu item with this name already exists.");

        if (vm.Photos.Count > MaxPhotosPerItem)
            ModelState.AddModelError(nameof(vm.Photos), $"A menu item can have at most {MaxPhotosPerItem} photos.");

        foreach (var file in vm.Photos.Where(x => x.Length > 0))
        {
            if (!AllowedExtensions.Contains(Path.GetExtension(file.FileName)))
                ModelState.AddModelError(nameof(vm.Photos), $"Invalid file type: {file.FileName}");
            if (file.Length > MaxPhotoBytes)
                ModelState.AddModelError(nameof(vm.Photos), $"{file.FileName} exceeds 5 MB.");
            if (file.Length <= MaxPhotoBytes && !await HasValidImageSignatureAsync(file))
                ModelState.AddModelError(nameof(vm.Photos), $"{file.FileName} is not a valid JPG, PNG or WebP image.");
        }

        var duplicateNames = vm.AddOns
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .GroupBy(x => x.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (duplicateNames.Count > 0)
            ModelState.AddModelError(nameof(vm.AddOns), $"Duplicate add-on name(s): {string.Join(", ", duplicateNames)}");

        foreach (var input in vm.AddOns.Where(x => x.AddOnId.HasValue && !string.IsNullOrWhiteSpace(x.Name)))
        {
            var addOnId = input.AddOnId.GetValueOrDefault();
            var normalizedName = input.Name.Trim();
            if (await db.AddOns.AnyAsync(x => x.AddOnId != addOnId && x.Name == normalizedName))
            {
                ModelState.AddModelError(nameof(vm.AddOns), $"Add-on name '{normalizedName}' already exists.");
            }
        }
    }

    private async Task ApplyAddOnsAsync(MenuItem entity, IEnumerable<AddOnInputViewModel> submittedInput)
    {
        var submitted = submittedInput.Where(x => !string.IsNullOrWhiteSpace(x.Name)).ToList();
        var submittedIds = submitted.Where(x => x.AddOnId.HasValue).Select(x => x.AddOnId!.Value).ToHashSet();

        var currentLinks = await db.MenuItemAddOns
            .Include(x => x.AddOn)
            .Where(x => x.MenuItemId == entity.MenuItemId)
            .ToListAsync();

        foreach (var link in currentLinks.Where(x => !submittedIds.Contains(x.AddOnId)))
            db.MenuItemAddOns.Remove(link);

        foreach (var input in submitted)
        {
            AddOn? addOn = null;

            if (input.AddOnId.HasValue)
            {
                addOn = currentLinks
                    .Where(x => x.AddOnId == input.AddOnId.Value)
                    .Select(x => x.AddOn)
                    .FirstOrDefault();
            }

            if (addOn is null)
            {
                var name = input.Name.Trim();
                addOn = await db.AddOns.FirstOrDefaultAsync(x => x.Name == name);
                if (addOn is null)
                {
                    addOn = new AddOn { Name = name };
                    db.AddOns.Add(addOn);
                }
            }

            addOn.Name = input.Name.Trim();
            addOn.Price = input.ExtraPrice;
            addOn.IsActive = input.IsAvailable;

            if (!currentLinks.Any(x => x.AddOnId == addOn.AddOnId && addOn.AddOnId != 0))
            {
                entity.MenuItemAddOns.Add(new MenuItemAddOn { AddOn = addOn });
            }
        }
    }

    private async Task SaveUploadedPhotosAsync(int menuItemId, IEnumerable<IFormFile> files)
    {
        var folder = Path.Combine(environment.ContentRootPath, "App_Data", "MenuPhotos");
        Directory.CreateDirectory(folder);

        var existingCount = await db.MenuItemPhotos.CountAsync(x => x.MenuItemId == menuItemId);
        var hasPrimary = await db.MenuItemPhotos.AnyAsync(x => x.MenuItemId == menuItemId && x.IsPrimary);
        var displayOrder = existingCount;

        foreach (var file in files.Where(x => x.Length > 0))
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var safeName = $"{Guid.NewGuid():N}{extension}";
            var physicalPath = Path.Combine(folder, safeName);

            await using (var stream = System.IO.File.Create(physicalPath))
                await file.CopyToAsync(stream);

            db.MenuItemPhotos.Add(new MenuItemPhoto
            {
                MenuItemId = menuItemId,
                FileName = $"/menu-photo/{safeName}",
                IsPrimary = !hasPrimary,
                DisplayOrder = displayOrder++
            });
            hasPrimary = true;
        }

        await db.SaveChangesAsync();
    }

    private static async Task<bool> HasValidImageSignatureAsync(IFormFile file)
    {
        if (file.Length < 12) return false;
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
        if (read < header.Length) return false;

        var isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var isPng = header.AsSpan(0, 8).SequenceEqual(
            new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        var isWebP = header.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                     header.AsSpan(8, 4).SequenceEqual("WEBP"u8);
        return isJpeg || isPng || isWebP;
    }

    private void DeletePhysicalPhoto(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return;

        const string prefix = "/menu-photo/";
        if (!fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return;

        var safeName = Path.GetFileName(fileName[prefix.Length..]);
        var path = Path.Combine(environment.ContentRootPath, "App_Data", "MenuPhotos", safeName);
        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);
    }

    private async Task<List<SelectListItem>> GetCategoryOptionsAsync(bool includeInactive = false)
    {
        var query = db.Categories.AsNoTracking().AsQueryable();
        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        return await query
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem
            {
                Value = x.CategoryId.ToString(),
                Text = x.IsActive ? x.Name : $"{x.Name} (Inactive)"
            })
            .ToListAsync();
    }

    private void AddAudit(string action, int menuItemId, string details) => db.AuditLogs.Add(new AuditLog
    {
        UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
        Action = action,
        EntityName = nameof(MenuItem),
        EntityId = menuItemId.ToString(),
        Details = details
    });
}
