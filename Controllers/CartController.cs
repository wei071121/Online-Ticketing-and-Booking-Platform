using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QigloRestaurant.Web.Data;
using QigloRestaurant.Web.Models;
using QigloRestaurant.Web.Services;
using QigloRestaurant.Web.ViewModels;

namespace QigloRestaurant.Web.Controllers;

[Authorize(Roles = "Customer")]
public class CartController(DB db, IOrderService orderService) : Controller
{
    private const string CartSessionKey = "QIGLO.Menu.Cart";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cart = await BuildCartViewModelAsync();
        if (cart.DiningSessionId <= 0)
        {
            TempData["Error"] = "You can use the cart only after restaurant check-in.";
            return RedirectToAction("Index", "Menu");
        }

        return View(cart);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(AddToCartRequest request)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please check the quantity and special instructions.";
            return RedirectToAction("Details", "Menu", new { id = request.MenuItemId });
        }

        var diningSessionId = await ResolveActiveDiningSessionIdAsync();
        if (!diningSessionId.HasValue)
        {
            TempData["Error"] = "You must have an active dining session before using the cart.";
            return RedirectToAction("Details", "Menu", new { id = request.MenuItemId });
        }

        var item = await db.MenuItems
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.MenuItemAddOns)
                .ThenInclude(x => x.AddOn)
            .FirstOrDefaultAsync(x => x.MenuItemId == request.MenuItemId && x.Category.IsActive);

        if (item is null || !item.IsAvailable)
        {
            TempData["Error"] = item is null ? "Menu item not found." : "This menu item is currently sold out.";
            return RedirectToAction("Details", "Menu", new { id = request.MenuItemId });
        }

        var selectedIds = request.SelectedAddOnIds.Distinct().OrderBy(x => x).ToList();
        var allowedIds = item.MenuItemAddOns
            .Where(x => x.AddOn.IsActive)
            .Select(x => x.AddOnId)
            .ToHashSet();

        if (selectedIds.Any(id => !allowedIds.Contains(id)))
        {
            TempData["Error"] = "One or more selected add-ons are invalid or unavailable.";
            return RedirectToAction("Details", "Menu", new { id = request.MenuItemId });
        }

        var cart = ReadCart();
        if (cart.DiningSessionId != diningSessionId.Value)
            cart = new CartSessionModel { DiningSessionId = diningSessionId.Value };

        var instructions = request.SpecialInstructions?.Trim();
        var existing = cart.Lines.FirstOrDefault(x =>
            x.MenuItemId == request.MenuItemId &&
            x.AddOnIds.SequenceEqual(selectedIds) &&
            string.Equals(x.SpecialInstructions ?? string.Empty, instructions ?? string.Empty, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            if (existing.Quantity + request.Quantity > 20)
            {
                TempData["Error"] = "Maximum quantity for the same customized item is 20.";
                return RedirectToAction("Details", "Menu", new { id = request.MenuItemId });
            }
            existing.Quantity += request.Quantity;
        }
        else
        {
            cart.Lines.Add(new CartSessionLine
            {
                LineKey = Guid.NewGuid().ToString("N"),
                MenuItemId = request.MenuItemId,
                Quantity = request.Quantity,
                AddOnIds = selectedIds,
                SpecialInstructions = instructions
            });
        }

        WriteCart(cart);
        TempData["Success"] = "Item added to cart.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Update(UpdateCartQuantityRequest request)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Quantity must be between 1 and 20.";
            return RedirectToAction(nameof(Index));
        }

        var cart = ReadCart();
        var line = cart.Lines.FirstOrDefault(x => x.LineKey == request.LineKey);
        if (line is null)
        {
            TempData["Error"] = "Cart item not found.";
            return RedirectToAction(nameof(Index));
        }

        line.Quantity = request.Quantity;
        WriteCart(cart);
        TempData["Success"] = "Cart updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(string lineKey)
    {
        var cart = ReadCart();
        if (cart.Lines.RemoveAll(x => x.LineKey == lineKey) == 0)
            TempData["Error"] = "Cart item not found.";
        else
        {
            WriteCart(cart);
            TempData["Success"] = "Item removed.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        HttpContext.Session.Remove(CartSessionKey);
        TempData["Success"] = "Cart cleared.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout()
    {
        var activeSessionId = await ResolveActiveDiningSessionIdAsync();
        var cart = ReadCart();
        if (!activeSessionId.HasValue || cart.DiningSessionId != activeSessionId.Value || cart.Lines.Count == 0)
        {
            TempData["Error"] = "Your cart is empty or there is no active dining session.";
            return RedirectToAction(nameof(Index));
        }

        var orderInput = new CreateOrderViewModel
        {
            DiningSessionId = activeSessionId.Value,
            Items = cart.Lines.Select(line => new OrderMenuItemInput
            {
                MenuItemId = line.MenuItemId,
                Quantity = line.Quantity,
                SpecialRequest = line.SpecialInstructions,
                SelectedAddOnIds = line.AddOnIds.ToList()
            }).ToList()
        };

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await orderService.PlaceOrderAsync(orderInput, userId, false);
        if (!result.Succeeded)
        {
            TempData["Error"] = result.Error ?? "Unable to place the order.";
            return RedirectToAction(nameof(Index));
        }

        HttpContext.Session.Remove(CartSessionKey);
        TempData["Success"] = $"Order {result.Order!.OrderNumber} was placed successfully.";
        return RedirectToAction("Details", "Orders", new { id = result.Order.OrderId });
    }

    private async Task<CartViewModel> BuildCartViewModelAsync()
    {
        var activeSessionId = await ResolveActiveDiningSessionIdAsync();
        if (!activeSessionId.HasValue)
        {
            HttpContext.Session.Remove(CartSessionKey);
            return new CartViewModel();
        }

        var cart = ReadCart();
        if (cart.DiningSessionId != activeSessionId.Value)
        {
            cart = new CartSessionModel { DiningSessionId = activeSessionId.Value };
            WriteCart(cart);
        }

        var menuIds = cart.Lines.Select(x => x.MenuItemId).Distinct().ToList();
        var menuItems = await db.MenuItems
            .AsNoTracking()
            .Include(x => x.Photos)
            .Include(x => x.MenuItemAddOns)
                .ThenInclude(x => x.AddOn)
            .Where(x => menuIds.Contains(x.MenuItemId))
            .ToDictionaryAsync(x => x.MenuItemId);

        var vm = new CartViewModel { DiningSessionId = activeSessionId.Value };
        foreach (var line in cart.Lines)
        {
            if (!menuItems.TryGetValue(line.MenuItemId, out var item))
                continue;

            var addOns = item.MenuItemAddOns
                .Where(x => line.AddOnIds.Contains(x.AddOnId))
                .Select(x => x.AddOn)
                .OrderBy(x => x.Name)
                .Select(x => new CartAddOnViewModel
                {
                    AddOnId = x.AddOnId,
                    Name = x.Name,
                    ExtraPrice = x.Price
                })
                .ToList();

            vm.Lines.Add(new CartLineViewModel
            {
                LineKey = line.LineKey,
                MenuItemId = item.MenuItemId,
                MenuItemName = item.Name,
                UnitPrice = item.Price,
                Quantity = line.Quantity,
                SpecialInstructions = line.SpecialInstructions,
                Photo = item.Photos
                    .OrderByDescending(x => x.IsPrimary)
                    .ThenBy(x => x.DisplayOrder)
                    .Select(x => x.FileName)
                    .FirstOrDefault(),
                AddOns = addOns
            });
        }

        return vm;
    }

    private async Task<int?> ResolveActiveDiningSessionIdAsync()
    {
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdText, out var userId))
            return null;

        return await db.DiningSessions
            .AsNoTracking()
            .Where(x => x.CustomerId == userId && x.Status == "Active")
            .OrderByDescending(x => x.StartedAt)
            .Select(x => (int?)x.DiningSessionId)
            .FirstOrDefaultAsync();
    }

    private CartSessionModel ReadCart()
    {
        var json = HttpContext.Session.GetString(CartSessionKey);
        if (string.IsNullOrWhiteSpace(json))
            return new CartSessionModel();

        try
        {
            return JsonSerializer.Deserialize<CartSessionModel>(json, JsonOptions) ?? new CartSessionModel();
        }
        catch
        {
            HttpContext.Session.Remove(CartSessionKey);
            return new CartSessionModel();
        }
    }

    private void WriteCart(CartSessionModel cart)
    {
        var json = JsonSerializer.Serialize(cart, JsonOptions);
        HttpContext.Session.SetString(CartSessionKey, json);
    }

    private sealed class CartSessionModel
    {
        public int DiningSessionId { get; set; }
        public List<CartSessionLine> Lines { get; set; } = [];
    }

    private sealed class CartSessionLine
    {
        public string LineKey { get; set; } = string.Empty;
        public int MenuItemId { get; set; }
        public int Quantity { get; set; }
        public List<int> AddOnIds { get; set; } = [];
        public string? SpecialInstructions { get; set; }
    }
}
