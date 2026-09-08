using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QigloRestaurant.Web.Models;
using QigloRestaurant.Web.Services;

namespace QigloRestaurant.Web.Controllers;

// Reservation and seating: Table Management (add / edit / delete restaurant tables).
[Authorize(Roles = "Staff,Admin")]
public class TablesController(ITableService tableService) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(await tableService.GetAllAsync());
    }

    public IActionResult Create() => View(new TableFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TableFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await tableService.CreateAsync(model, CurrentUserId());
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = $"Table {result.Table!.TableNumber} was added.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var table = await tableService.GetByIdAsync(id);
        if (table is null)
        {
            return NotFound();
        }

        return View(new TableFormViewModel
        {
            DiningTableId = table.DiningTableId,
            TableNumber = table.TableNumber,
            Capacity = table.Capacity,
            Status = table.Status,
            IsActive = table.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TableFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await tableService.UpdateAsync(model, CurrentUserId());
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = "Table updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await tableService.DeleteAsync(id, CurrentUserId());
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private int CurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
