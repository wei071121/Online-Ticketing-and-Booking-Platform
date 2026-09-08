using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QigloRestaurant.Models.Entities;
using QigloRestaurant.Web.Data;
using QigloRestaurant.Web.Models;

namespace QigloRestaurant.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminCategoriesController(DB db) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(await db.Categories
            .AsNoTracking()
            .Include(x => x.MenuItems)
            .OrderBy(x => x.Name)
            .ToListAsync());
    }

    [HttpGet]
    public IActionResult Create() => View(new Category());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category model)
    {
        model.Name = model.Name?.Trim() ?? string.Empty;

        if (await db.Categories.AnyAsync(x => x.Name == model.Name))
            ModelState.AddModelError(nameof(model.Name), "This category name already exists.");

        if (!ModelState.IsValid)
            return View(model);

        db.Categories.Add(model);
        await db.SaveChangesAsync();
        AddAudit("Create category", model, $"Created category {model.Name}.");
        await db.SaveChangesAsync();
        TempData["Success"] = "Category created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await db.Categories.FindAsync(id);
        return category is null ? NotFound() : View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Category model)
    {
        if (id != model.CategoryId)
            return BadRequest();

        model.Name = model.Name?.Trim() ?? string.Empty;
        if (await db.Categories.AnyAsync(x => x.CategoryId != id && x.Name == model.Name))
            ModelState.AddModelError(nameof(model.Name), "This category name already exists.");

        if (!ModelState.IsValid)
            return View(model);

        var entity = await db.Categories.FindAsync(id);
        if (entity is null)
            return NotFound();

        entity.Name = model.Name;
        entity.Description = model.Description?.Trim();
        entity.IsActive = model.IsActive;
        AddAudit("Update category", entity, $"Updated category {entity.Name}; active={entity.IsActive}.");
        await db.SaveChangesAsync();

        TempData["Success"] = "Category updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await db.Categories
            .Include(x => x.MenuItems)
            .FirstOrDefaultAsync(x => x.CategoryId == id);

        if (category is null)
            return NotFound();

        if (category.MenuItems.Any())
        {
            TempData["Error"] = "This category cannot be deleted because it still contains menu items.";
            return RedirectToAction(nameof(Index));
        }

        db.Categories.Remove(category);
        AddAudit("Delete category", category, $"Deleted empty category {category.Name}.");
        await db.SaveChangesAsync();
        TempData["Success"] = "Category deleted.";
        return RedirectToAction(nameof(Index));
    }

    private void AddAudit(string action, Category category, string details) => db.AuditLogs.Add(new AuditLog
    {
        UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
        Action = action,
        EntityName = nameof(Category),
        EntityId = category.CategoryId.ToString(),
        Details = details
    });
}
