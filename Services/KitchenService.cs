using Microsoft.EntityFrameworkCore;
using QigloRestaurant.Web.Models;

namespace QigloRestaurant.Web.Services;

public interface IKitchenService
{
    Task<(bool Succeeded, string Message)> UpdateStatusAsync(int orderId, string newStatus, int actingUserId);
}

public class KitchenService(DB db) : IKitchenService
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedTransitions =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Pending"] = ["Preparing", "Cancelled"],
            ["Preparing"] = ["Ready", "Cancelled"],
            ["Ready"] = ["Served"],
            ["Served"] = [],
            ["Cancelled"] = []
        };

    public async Task<(bool Succeeded, string Message)> UpdateStatusAsync(
        int orderId,
        string newStatus,
        int actingUserId)
    {
        var order = await db.Orders
            .Include(item => item.DiningSession)
                .ThenInclude(item => item.DiningTable)
            .SingleOrDefaultAsync(item => item.OrderId == orderId);
        if (order is null)
        {
            return (false, "Order not found.");
        }

        if (!AllowedTransitions.TryGetValue(order.Status, out var allowed) ||
            !allowed.Contains(newStatus, StringComparer.OrdinalIgnoreCase))
        {
            return (false, $"Cannot change {order.Status} to {newStatus}.");
        }

        order.Status = newStatus;
        if (newStatus.Equals("Served", StringComparison.OrdinalIgnoreCase))
        {
            order.ServedAt = DateTime.Now;
        }

        db.AuditLogs.Add(new AuditLog
        {
            UserId = actingUserId,
            Action = newStatus == "Cancelled" ? "Cancel order" : "Update kitchen status",
            EntityName = nameof(Order),
            EntityId = order.OrderId.ToString(),
            Details = $"Order {order.OrderNumber} for Table {order.DiningSession.DiningTable.TableNumber} changed to {newStatus}."
        });
        await db.SaveChangesAsync();
        return (true, $"Order {order.OrderNumber} for Table {order.DiningSession.DiningTable.TableNumber} updated to {newStatus}.");
    }
}
