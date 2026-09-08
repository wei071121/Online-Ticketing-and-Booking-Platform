using Microsoft.EntityFrameworkCore;
using QigloRestaurant.Models.Entities;

namespace QigloRestaurant.Web.Services;

// Reservation and seating: shared availability-checking logic used by both the customer
// reservation form (AJAX) and staff walk-in seating, and by the service
// layer itself when approving a reservation. This is what prevents double
// booking - a table is only "available" if no active Pending/Confirmed/
// CheckedIn reservation overlaps the requested time window. An abandoned
// pending request stops holding a table after 20 minutes.
public interface ITableAvailabilityService
{
    Task<List<DiningTable>> GetAvailableTablesAsync(
        DateTime startAt, DateTime endAt, int pax, int? excludeReservationId = null);

    Task<bool> IsTableAvailableAsync(
        int diningTableId, DateTime startAt, DateTime endAt, int? excludeReservationId = null);

    Task<List<AvailableTimeSlotOption>> GetAvailableTimeSlotsAsync(
        DateTime reservationDate, int pax, int durationMinutes);
}

public class TableAvailabilityService(DB db) : ITableAvailabilityService
{
    private const int PendingHoldMinutes = 20;
    private static readonly TimeOnly OpeningTime = new(10, 0);
    private static readonly TimeOnly ClosingTime = new(22, 0);
    private const int SlotIntervalMinutes = 30;

    public async Task<List<DiningTable>> GetAvailableTablesAsync(
        DateTime startAt, DateTime endAt, int pax, int? excludeReservationId = null)
    {
        var candidateTables = await db.DiningTables
            .AsNoTracking()
            .Where(table => table.IsActive && table.Status != "Unavailable" && table.Capacity >= pax)
            .OrderBy(table => table.Capacity)
            .ThenBy(table => table.TableNumber)
            .ToListAsync();

        var pendingCutoff = DateTime.Now.AddMinutes(-PendingHoldMinutes);
        var busyTableIds = await db.Reservations
            .AsNoTracking()
            .Where(reservation =>
                reservation.DiningTableId != null &&
                (reservation.Status == "Confirmed" ||
                 reservation.Status == "CheckedIn" ||
                 (reservation.Status == "Pending" && reservation.CreatedAt >= pendingCutoff)) &&
                reservation.StartAt < endAt &&
                reservation.EndAt > startAt &&
                (excludeReservationId == null || reservation.ReservationId != excludeReservationId))
            .Select(reservation => reservation.DiningTableId!.Value)
            .ToListAsync();

        return candidateTables
            .Where(table => !busyTableIds.Contains(table.DiningTableId))
            .ToList();
    }

    public async Task<bool> IsTableAvailableAsync(
        int diningTableId, DateTime startAt, DateTime endAt, int? excludeReservationId = null)
    {
        var table = await db.DiningTables
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.DiningTableId == diningTableId);

        if (table is null || !table.IsActive || table.Status == "Unavailable")
        {
            return false;
        }

        var pendingCutoff = DateTime.Now.AddMinutes(-PendingHoldMinutes);
        var hasOverlap = await db.Reservations
            .AsNoTracking()
            .AnyAsync(reservation =>
                reservation.DiningTableId == diningTableId &&
                (reservation.Status == "Confirmed" ||
                 reservation.Status == "CheckedIn" ||
                 (reservation.Status == "Pending" && reservation.CreatedAt >= pendingCutoff)) &&
                reservation.StartAt < endAt &&
                reservation.EndAt > startAt &&
                (excludeReservationId == null || reservation.ReservationId != excludeReservationId));

        return !hasOverlap;
    }

    public async Task<List<AvailableTimeSlotOption>> GetAvailableTimeSlotsAsync(
        DateTime reservationDate, int pax, int durationMinutes)
    {
        var safeDuration = Math.Clamp(durationMinutes, 30, 240);
        var firstStart = reservationDate.Date + OpeningTime.ToTimeSpan();
        var lastStart = reservationDate.Date + ClosingTime.ToTimeSpan() - TimeSpan.FromMinutes(safeDuration);
        var earliestAllowed = DateTime.Now.AddMinutes(30);
        var slots = new List<AvailableTimeSlotOption>();

        for (var startAt = firstStart; startAt <= lastStart; startAt = startAt.AddMinutes(SlotIntervalMinutes))
        {
            if (startAt < earliestAllowed)
            {
                continue;
            }

            var availableTables = await GetAvailableTablesAsync(
                startAt, startAt.AddMinutes(safeDuration), pax);
            if (availableTables.Count == 0)
            {
                continue;
            }

            slots.Add(new AvailableTimeSlotOption
            {
                ReservationTime = startAt.ToString("HH:mm"),
                DisplayTime = startAt.ToString("h:mm tt"),
                AvailableTableCount = availableTables.Count
            });
        }

        return slots;
    }
}
