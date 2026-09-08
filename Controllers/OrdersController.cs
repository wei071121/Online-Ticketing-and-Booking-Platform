using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QigloRestaurant.Web.Models;
using QigloRestaurant.Web.Services;

namespace QigloRestaurant.Web.Controllers;

[Authorize(Roles = "Customer,Staff,Admin")]
public class OrdersController(
    DB db,
    IOrderService orderService,
    IKitchenService kitchenService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var query = db.Orders
            .AsNoTracking()
            .Include(item => item.DiningSession)
                .ThenInclude(item => item.DiningTable)
            .Include(item => item.OrderItems)
                .ThenInclude(item => item.OrderItemAddOns)
            .AsQueryable();

        if (User.IsInRole("Customer"))
        {
            var userId = CurrentUserId();
            query = query.Where(item => item.DiningSession.CustomerId == userId);
        }

        return View(await query.OrderByDescending(item => item.OrderedAt).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await LoadOrderAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        if (User.IsInRole("Customer") && order.DiningSession.CustomerId != CurrentUserId())
        {
            return Forbid();
        }

        return View(order);
    }

    public async Task<IActionResult> Track(int id)
    {
        var order = await LoadOrderAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        if (User.IsInRole("Customer") && order.DiningSession.CustomerId != CurrentUserId())
        {
            return Forbid();
        }

        return View(order);
    }

    [HttpGet]
    public async Task<IActionResult> StatusJson(int id)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(item => item.DiningSession)
            .SingleOrDefaultAsync(item => item.OrderId == id);

        if (order is null)
        {
            return NotFound();
        }

        if (User.IsInRole("Customer") && order.DiningSession.CustomerId != CurrentUserId())
        {
            return Forbid();
        }

        return Json(new
        {
            order.OrderId,
            order.OrderNumber,
            order.Status,
            updatedAt = order.ServedAt ?? order.OrderedAt
        });
    }

    public async Task<IActionResult> Create(int? diningSessionId)
    {
        var customerId = User.IsInRole("Customer") ? CurrentUserId() : (int?)null;
        return View(await orderService.BuildCreateViewModelAsync(diningSessionId, customerId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrderViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(await RebuildAsync(model));
        }

        var result = await orderService.PlaceOrderAsync(
            model,
            CurrentUserId(),
            User.IsInRole("Staff") || User.IsInRole("Admin"));

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(await RebuildAsync(model));
        }

        TempData["Success"] = $"Order {result.Order!.OrderNumber} was placed successfully.";
        return RedirectToAction(nameof(Details), new { id = result.Order.OrderId });
    }

    [Authorize(Roles = "Staff,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? returnUrl = null)
    {
        var result = await kitchenService.UpdateStatusAsync(id, "Cancelled", CurrentUserId());
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<CreateOrderViewModel> RebuildAsync(CreateOrderViewModel posted)
    {
        var customerId = User.IsInRole("Customer") ? CurrentUserId() : (int?)null;
        var rebuilt = await orderService.BuildCreateViewModelAsync(posted.DiningSessionId, customerId);
        rebuilt.Notes = posted.Notes;
        foreach (var item in rebuilt.Items)
        {
            var postedItem = posted.Items.FirstOrDefault(value => value.MenuItemId == item.MenuItemId);
            if (postedItem is null)
            {
                continue;
            }

            item.Quantity = postedItem.Quantity;
            item.SpecialRequest = postedItem.SpecialRequest;
            item.SelectedAddOnIds = postedItem.SelectedAddOnIds;
        }

        return rebuilt;
    }

    private int CurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private Task<QigloRestaurant.Models.Entities.Order?> LoadOrderAsync(int id) => db.Orders
        .AsNoTracking()
        .Include(item => item.DiningSession)
            .ThenInclude(item => item.DiningTable)
        .Include(item => item.OrderItems)
            .ThenInclude(item => item.OrderItemAddOns)
        .SingleOrDefaultAsync(item => item.OrderId == id);
}
