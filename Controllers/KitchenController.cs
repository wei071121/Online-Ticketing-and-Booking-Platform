using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QigloRestaurant.Web.Models;
using QigloRestaurant.Web.Services;

namespace QigloRestaurant.Web.Controllers;

[Authorize(Roles = "KitchenStaff")]
public class KitchenController(
    DB db,
    IKitchenService kitchenService) : Controller
{
    public IActionResult Index() => View();

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> BoardPartial()
    {
        var orders = await db.Orders
            .AsNoTracking()
            .Include(item => item.DiningSession)
                .ThenInclude(item => item.DiningTable)
            .Include(item => item.OrderItems)
                .ThenInclude(item => item.OrderItemAddOns)
            .Where(item => item.Status != "Served" && item.Status != "Cancelled")
            .OrderBy(item => item.OrderedAt)
            .ToListAsync();

        return PartialView("_KitchenBoard", orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int orderId, string newStatus)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await kitchenService.UpdateStatusAsync(orderId, newStatus, userId);
        return result.Succeeded
            ? Json(new { succeeded = true, message = result.Message })
            : BadRequest(new { succeeded = false, message = result.Message });
    }
}
