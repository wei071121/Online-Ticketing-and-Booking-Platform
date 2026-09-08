using Microsoft.EntityFrameworkCore;
using QigloRestaurant.Models.Entities;
using QigloRestaurant.Web.Models;

namespace QigloRestaurant.Web.Services;

public interface IOrderService
{
    Task<CreateOrderViewModel> BuildCreateViewModelAsync(int? diningSessionId, int? customerId);
    Task<(bool Succeeded, string? Error, Order? Order)> PlaceOrderAsync(
        CreateOrderViewModel input,
        int currentUserId,
        bool isStaff);
}

public class OrderService(DB db) : IOrderService
{
    public async Task<CreateOrderViewModel> BuildCreateViewModelAsync(
        int? diningSessionId,
        int? customerId)
    {
        var sessionsQuery = db.DiningSessions
            .AsNoTracking()
            .Include(item => item.DiningTable)
            .Where(item => item.Status == "Active");

        if (customerId.HasValue)
        {
            sessionsQuery = sessionsQuery.Where(item => item.CustomerId == customerId);
        }

        var sessions = await sessionsQuery
            .OrderBy(item => item.DiningTable.TableNumber)
            .Select(item => new DiningSessionOption
            {
                DiningSessionId = item.DiningSessionId,
                Label = $"Session #{item.DiningSessionId} - Table {item.DiningTable.TableNumber}"
            })
            .ToListAsync();

        var menuItems = await db.MenuItems
            .AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.MenuItemAddOns)
                .ThenInclude(item => item.AddOn)
            .Where(item => item.IsAvailable && item.Category.IsActive)
            .OrderBy(item => item.Category.Name)
            .ThenBy(item => item.Name)
            .ToListAsync();

        return new CreateOrderViewModel
        {
            DiningSessionId = diningSessionId ?? sessions.FirstOrDefault()?.DiningSessionId ?? 0,
            DiningSessions = sessions,
            Items = menuItems.Select(item => new OrderMenuItemInput
            {
                MenuItemId = item.MenuItemId,
                Name = item.Name,
                CategoryName = item.Category.Name,
                Price = item.Price,
                AddOns = item.MenuItemAddOns
                    .Where(link => link.AddOn.IsActive)
                    .Select(link => new AddOnOption
                    {
                        AddOnId = link.AddOnId,
                        Name = link.AddOn.Name,
                        Price = link.AddOn.Price
                    })
                    .ToList()
            }).ToList()
        };
    }

    public async Task<(bool Succeeded, string? Error, Order? Order)> PlaceOrderAsync(
        CreateOrderViewModel input,
        int currentUserId,
        bool isStaff)
    {
        var session = await db.DiningSessions
            .SingleOrDefaultAsync(item => item.DiningSessionId == input.DiningSessionId);

        if (session is null || session.Status != "Active")
        {
            return (false, "The dining session is not active.", null);
        }

        if (!isStaff && session.CustomerId != currentUserId)
        {
            return (false, "You cannot place an order for another customer's session.", null);
        }

        var selectedInputs = input.Items.Where(item => item.Quantity > 0).ToList();
        if (selectedInputs.Count == 0)
        {
            return (false, "Select at least one menu item.", null);
        }

        if (selectedInputs.Any(item => item.Quantity is < 1 or > 20))
        {
            return (false, "Each quantity must be between 1 and 20.", null);
        }

        var menuItemIds = selectedInputs.Select(item => item.MenuItemId).Distinct().ToList();
        var menuItems = await db.MenuItems
            .Include(item => item.MenuItemAddOns)
                .ThenInclude(item => item.AddOn)
            .Where(item => menuItemIds.Contains(item.MenuItemId) && item.IsAvailable)
            .ToDictionaryAsync(item => item.MenuItemId);

        if (menuItems.Count != menuItemIds.Count)
        {
            return (false, "One or more selected menu items are unavailable.", null);
        }

        await using var transaction = await db.Database.BeginTransactionAsync();

        var order = new Order
        {
            DiningSessionId = session.DiningSessionId,
            CreatedByUserId = currentUserId,
            OrderNumber = $"ORD-{DateTime.Now:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
            Status = "Pending",
            Notes = input.Notes?.Trim(),
            OrderedAt = DateTime.Now
        };

        foreach (var selected in selectedInputs)
        {
            var menuItem = menuItems[selected.MenuItemId];
            var allowedAddOns = menuItem.MenuItemAddOns
                .Where(link => link.AddOn.IsActive)
                .ToDictionary(link => link.AddOnId, link => link.AddOn);

            if (selected.SelectedAddOnIds.Any(addOnId => !allowedAddOns.ContainsKey(addOnId)))
            {
                return (false, $"An invalid add-on was selected for {menuItem.Name}.", null);
            }

            var orderItem = new OrderItem
            {
                MenuItemId = menuItem.MenuItemId,
                MenuItemName = menuItem.Name,
                UnitPrice = menuItem.Price,
                Quantity = selected.Quantity,
                SpecialRequest = selected.SpecialRequest?.Trim()
            };

            foreach (var addOnId in selected.SelectedAddOnIds.Distinct())
            {
                var addOn = allowedAddOns[addOnId];
                orderItem.OrderItemAddOns.Add(new OrderItemAddOn
                {
                    AddOnId = addOn.AddOnId,
                    AddOnName = addOn.Name,
                    UnitPrice = addOn.Price
                });
            }

            order.OrderItems.Add(orderItem);
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync();
        db.AuditLogs.Add(new AuditLog
        {
            UserId = currentUserId,
            Action = "Place order",
            EntityName = nameof(Order),
            EntityId = order.OrderId.ToString(),
            Details = $"Placed order {order.OrderNumber} for dining session #{session.DiningSessionId}."
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (true, null, order);
    }
}
