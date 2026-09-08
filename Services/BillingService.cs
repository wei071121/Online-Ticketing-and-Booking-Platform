using System.Data;
using Microsoft.EntityFrameworkCore;
using QigloRestaurant.Models.Entities;
using QigloRestaurant.Web.Models;

namespace QigloRestaurant.Web.Services;

public interface IBillingService
{
    Task<PaymentCreateViewModel?> BuildPaymentAsync(int diningSessionId);
    Task<(bool Succeeded, string? Error, Payment? Payment)> CompletePaymentAsync(
        PaymentCreateViewModel input,
        int processedByUserId);
    Task<Payment?> GetReceiptAsync(int paymentId);
}

public class BillingService(DB db) : IBillingService
{
    private const decimal TaxRate = 0.06m;
    private const decimal ServiceChargeRate = 0.10m;

    public async Task<PaymentCreateViewModel?> BuildPaymentAsync(int diningSessionId)
    {
        var session = await LoadSessionAsync(diningSessionId);
        if (session is null)
        {
            return null;
        }

        var lines = session.Orders
            .Where(order => order.Status != "Cancelled")
            .SelectMany(order => order.OrderItems)
            .Select(item =>
            {
                var unitPrice = item.UnitPrice + item.OrderItemAddOns.Sum(addOn => addOn.UnitPrice);
                return new BillingLineViewModel
                {
                    Description = item.OrderItemAddOns.Count == 0
                        ? item.MenuItemName
                        : $"{item.MenuItemName} ({string.Join(", ", item.OrderItemAddOns.Select(addOn => addOn.AddOnName))})",
                    Quantity = item.Quantity,
                    UnitPriceWithAddOns = unitPrice,
                    LineTotal = decimal.Round(unitPrice * item.Quantity, 2)
                };
            })
            .ToList();

        var subtotal = decimal.Round(lines.Sum(line => line.LineTotal), 2);
        var tax = decimal.Round(subtotal * TaxRate, 2, MidpointRounding.AwayFromZero);
        var serviceCharge = decimal.Round(
            subtotal * ServiceChargeRate,
            2,
            MidpointRounding.AwayFromZero);
        var total = subtotal + tax + serviceCharge;

        return new PaymentCreateViewModel
        {
            DiningSessionId = session.DiningSessionId,
            TableNumber = session.DiningTable.TableNumber,
            Lines = lines,
            Subtotal = subtotal,
            TaxAmount = tax,
            ServiceCharge = serviceCharge,
            TotalAmount = total,
            AmountReceived = total
        };
    }

    public async Task<(bool Succeeded, string? Error, Payment? Payment)> CompletePaymentAsync(
        PaymentCreateViewModel input,
        int processedByUserId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var session = await LoadSessionAsync(input.DiningSessionId);
        if (session is null)
        {
            return (false, "Dining session not found.", null);
        }

        if (session.Payment is not null || session.Status == "Completed")
        {
            return (false, "This dining session has already been paid.", null);
        }

        var activeOrders = session.Orders.Where(order => order.Status != "Cancelled").ToList();
        if (activeOrders.Count == 0)
        {
            return (false, "At least one order is required before payment.", null);
        }

        if (activeOrders.Any(order => order.Status != "Served"))
        {
            return (false, "All active orders must be served before payment.", null);
        }

        var calculated = await BuildPaymentAsync(input.DiningSessionId);
        if (calculated is null || calculated.TotalAmount <= 0)
        {
            return (false, "The bill total is invalid.", null);
        }

        if (input.AmountReceived < calculated.TotalAmount)
        {
            return (false, "Amount received is less than the total amount.", null);
        }

        var payment = new Payment
        {
            DiningSessionId = session.DiningSessionId,
            ProcessedByUserId = processedByUserId,
            ReceiptNumber = $"RCPT-{DateTime.Now:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
            Subtotal = calculated.Subtotal,
            TaxAmount = calculated.TaxAmount,
            ServiceCharge = calculated.ServiceCharge,
            TotalAmount = calculated.TotalAmount,
            AmountReceived = input.AmountReceived,
            ChangeAmount = decimal.Round(input.AmountReceived - calculated.TotalAmount, 2),
            PaymentMethod = input.PaymentMethod,
            Status = "Paid",
            PaidAt = DateTime.Now
        };

        session.Status = "Completed";
        session.CompletedAt = DateTime.Now;
        session.DiningTable.Status = "Available";
        if (session.Reservation is not null)
        {
            session.Reservation.Status = "Completed";
        }

        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        db.AuditLogs.Add(new AuditLog
        {
            UserId = processedByUserId,
            Action = "Complete payment",
            EntityName = nameof(Payment),
            EntityId = payment.PaymentId.ToString(),
            Details = $"Paid dining session #{session.DiningSessionId}; receipt {payment.ReceiptNumber}; total RM {payment.TotalAmount:0.00}."
        });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (true, null, await GetReceiptAsync(payment.PaymentId));
    }

    public Task<Payment?> GetReceiptAsync(int paymentId) => db.Payments
        .AsNoTracking()
        .Include(item => item.ProcessedByUser)
        .Include(item => item.DiningSession)
            .ThenInclude(item => item.Customer)
        .Include(item => item.DiningSession)
            .ThenInclude(item => item.DiningTable)
        .Include(item => item.DiningSession)
            .ThenInclude(item => item.Orders)
                .ThenInclude(item => item.OrderItems)
                    .ThenInclude(item => item.OrderItemAddOns)
        .SingleOrDefaultAsync(item => item.PaymentId == paymentId);

    private Task<DiningSession?> LoadSessionAsync(int diningSessionId) => db.DiningSessions
        .Include(item => item.Customer)
        .Include(item => item.DiningTable)
        .Include(item => item.Reservation)
        .Include(item => item.Payment)
        .Include(item => item.Orders)
            .ThenInclude(item => item.OrderItems)
                .ThenInclude(item => item.OrderItemAddOns)
        .SingleOrDefaultAsync(item => item.DiningSessionId == diningSessionId);
}
