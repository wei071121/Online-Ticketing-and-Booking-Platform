using System.Data;
using Microsoft.EntityFrameworkCore;
using QigloRestaurant.Models.Entities;
using QigloRestaurant.Web.Models;

namespace QigloRestaurant.Web.Services;

// Reservation and seating: reservation, approval, check-in and walk-in business rules.
// Availability decisions are repeated inside serializable transactions so a
// crafted or simultaneous request cannot bypass the server-side rules.
public interface IReservationService
{
    Task<List<Reservation>> GetForCustomerAsync(int customerId);
    Task<List<Reservation>> GetAllAsync(string? statusFilter = null);
    Task<Reservation?> GetByIdAsync(int reservationId);
    Task<(bool Succeeded, string? Error, Reservation? Reservation)> CreateAsync(
        CreateReservationViewModel input, int customerId);
    Task<(bool Succeeded, string Message, Reservation? Reservation)> ApproveAsync(
        int reservationId, int staffUserId, int? reassignTableId);
    Task<(bool Succeeded, string Message)> RejectAsync(
        int reservationId, int staffUserId, string? reason);
    Task<(bool Succeeded, string Message)> CancelAsync(
        int reservationId, int actingUserId, bool isStaff);
    Task<(bool Succeeded, string Message)> CheckInAsync(string bookingCode, int staffUserId);
    Task<(bool Succeeded, string Message, DiningSession? Session)> CreateWalkInAsync(
        WalkInViewModel input, int staffUserId);
}

public sealed class ReservationService(DB db, ITableAvailabilityService availability) : IReservationService
{
    private const int CheckInEarlyMinutes = 30;

    public Task<List<Reservation>> GetForCustomerAsync(int customerId) => db.Reservations
        .AsNoTracking()
        .Include(item => item.DiningTable)
        .Where(item => item.CustomerId == customerId)
        .OrderByDescending(item => item.StartAt)
        .ToListAsync();

    public Task<List<Reservation>> GetAllAsync(string? statusFilter = null)
    {
        var query = db.Reservations
            .AsNoTracking()
            .Include(item => item.Customer)
            .Include(item => item.DiningTable)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All")
        {
            query = query.Where(item => item.Status == statusFilter);
        }

        return query.OrderByDescending(item => item.CreatedAt).ToListAsync();
    }

    public Task<Reservation?> GetByIdAsync(int reservationId) => db.Reservations
        .AsNoTracking()
        .Include(item => item.Customer)
        .Include(item => item.DiningTable)
        .SingleOrDefaultAsync(item => item.ReservationId == reservationId);

    public async Task<(bool Succeeded, string? Error, Reservation? Reservation)> CreateAsync(
        CreateReservationViewModel input, int customerId)
    {
        if (!TimeOnly.TryParse(input.ReservationTime, out var time))
        {
            return (false, "Reservation time is invalid.", null);
        }

        var startAt = input.ReservationDate.Date + time.ToTimeSpan();
        if (startAt < DateTime.Now.AddMinutes(30))
        {
            return (false, "Please choose a time at least 30 minutes from now.", null);
        }

        var endAt = startAt.AddMinutes(input.DurationMinutes);

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var bestFitTable = (await availability.GetAvailableTablesAsync(startAt, endAt, input.Pax))
            .FirstOrDefault();
        if (bestFitTable is null)
        {
            return (false, "That time has just become unavailable. Please choose another available time.", null);
        }

        var table = await db.DiningTables
            .SingleOrDefaultAsync(item => item.DiningTableId == bestFitTable.DiningTableId);
        if (table is null || !table.IsActive || table.Status == "Unavailable")
        {
            return (false, "The assigned table is no longer available. Please choose another time.", null);
        }

        if (table.Capacity < input.Pax)
        {
            return (false, $"Table {table.TableNumber} only seats up to {table.Capacity} guest(s).", null);
        }

        if (!await availability.IsTableAvailableAsync(table.DiningTableId, startAt, endAt))
        {
            return (false,
                $"Table {table.TableNumber} was just booked for that time. Please check availability again.",
                null);
        }

        var reservation = new Reservation
        {
            CustomerId = customerId,
            DiningTableId = table.DiningTableId,
            StartAt = startAt,
            EndAt = endAt,
            Pax = input.Pax,
            Status = "Pending",
            BookingCode = await GenerateBookingCodeAsync(),
            Notes = input.Notes?.Trim()
        };

        db.Reservations.Add(reservation);

        try
        {
            await db.SaveChangesAsync();
            AddAudit(customerId, "Reservation created", nameof(Reservation), reservation.ReservationId,
                $"Requested {startAt:yyyy-MM-dd HH:mm}-{endAt:HH:mm}, table {table.TableNumber}, pax {input.Pax}.");
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return (true, null, reservation);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return (false, "The table or booking code changed while saving. Please check availability and try again.", null);
        }
    }

    public async Task<(bool Succeeded, string Message, Reservation? Reservation)> ApproveAsync(
        int reservationId, int staffUserId, int? reassignTableId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var reservation = await db.Reservations
            .Include(item => item.Customer)
            .Include(item => item.DiningTable)
            .SingleOrDefaultAsync(item => item.ReservationId == reservationId);
        if (reservation is null)
        {
            return (false, "Reservation not found.", null);
        }

        if (reservation.Status != "Pending")
        {
            return (false, $"Only pending reservations can be approved (current status: {reservation.Status}).", null);
        }

        var tableId = reassignTableId ?? reservation.DiningTableId;
        if (tableId is null)
        {
            return (false, "A table must be assigned before approval.", null);
        }

        var table = await db.DiningTables.SingleOrDefaultAsync(item => item.DiningTableId == tableId);
        if (table is null || !table.IsActive || table.Status == "Unavailable")
        {
            return (false, "The assigned table is unavailable.", null);
        }

        if (table.Capacity < reservation.Pax)
        {
            return (false, $"Table {table.TableNumber} cannot seat {reservation.Pax} guest(s).", null);
        }

        if (!await availability.IsTableAvailableAsync(
                tableId.Value, reservation.StartAt, reservation.EndAt, reservation.ReservationId))
        {
            return (false, "The selected table is no longer available for this time slot.", null);
        }

        reservation.DiningTableId = tableId;
        reservation.DiningTable = table;
        reservation.Status = "Confirmed";
        reservation.ApprovedByUserId = staffUserId;
        reservation.ApprovedAt = DateTime.Now;
        AddAudit(staffUserId, "Reservation approved", nameof(Reservation), reservation.ReservationId,
            $"Booking {reservation.BookingCode}; table {table.TableNumber}.");

        try
        {
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return (true, $"Reservation {reservation.BookingCode} confirmed.", reservation);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return (false, "The reservation changed while it was being approved. Please try again.", null);
        }
    }

    public async Task<(bool Succeeded, string Message)> RejectAsync(
        int reservationId, int staffUserId, string? reason)
    {
        var reservation = await db.Reservations.SingleOrDefaultAsync(item => item.ReservationId == reservationId);
        if (reservation is null)
        {
            return (false, "Reservation not found.");
        }

        if (reservation.Status != "Pending")
        {
            return (false, $"Only pending reservations can be rejected (current status: {reservation.Status}).");
        }

        reservation.Status = "Rejected";
        reservation.ApprovedByUserId = staffUserId;
        reservation.ApprovedAt = DateTime.Now;
        reservation.Notes = AppendNote(reservation.Notes, reason, "Rejected");
        AddAudit(staffUserId, "Reservation rejected", nameof(Reservation), reservation.ReservationId,
            $"Booking {reservation.BookingCode}; reason: {NormalizeForAudit(reason)}");
        await db.SaveChangesAsync();
        return (true, $"Reservation {reservation.BookingCode} rejected.");
    }

    public async Task<(bool Succeeded, string Message)> CancelAsync(
        int reservationId, int actingUserId, bool isStaff)
    {
        var reservation = await db.Reservations.SingleOrDefaultAsync(item => item.ReservationId == reservationId);
        if (reservation is null)
        {
            return (false, "Reservation not found.");
        }

        if (!isStaff && reservation.CustomerId != actingUserId)
        {
            return (false, "You can only cancel your own reservation.");
        }

        if (reservation.Status is not ("Pending" or "Confirmed"))
        {
            return (false, $"A {reservation.Status.ToLowerInvariant()} reservation cannot be cancelled.");
        }

        reservation.Status = "Cancelled";
        AddAudit(actingUserId, "Reservation cancelled", nameof(Reservation), reservation.ReservationId,
            $"Booking {reservation.BookingCode}; actor type: {(isStaff ? "staff" : "customer")}.");
        await db.SaveChangesAsync();
        return (true, $"Reservation {reservation.BookingCode} cancelled.");
    }

    public async Task<(bool Succeeded, string Message)> CheckInAsync(string bookingCode, int staffUserId)
    {
        var code = bookingCode.Trim().ToUpperInvariant();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var reservation = await db.Reservations
            .Include(item => item.Customer)
            .Include(item => item.DiningTable)
            .Include(item => item.DiningSession)
            .SingleOrDefaultAsync(item => item.BookingCode == code);

        if (reservation is null)
        {
            return (false, "No reservation found for this booking code.");
        }

        if (reservation.Status != "Confirmed")
        {
            return (false, $"This reservation is {reservation.Status.ToLowerInvariant()} and cannot be checked in.");
        }

        var now = DateTime.Now;
        if (now < reservation.StartAt.AddMinutes(-CheckInEarlyMinutes))
        {
            return (false,
                $"Check-in opens {CheckInEarlyMinutes} minutes before {reservation.StartAt:dd MMM yyyy, HH:mm}.");
        }

        if (now > reservation.EndAt)
        {
            return (false, "This reservation time has already ended. Staff should create a walk-in session instead.");
        }

        if (reservation.DiningTableId is null || reservation.DiningTable is null ||
            !reservation.DiningTable.IsActive || reservation.DiningTable.Status == "Unavailable")
        {
            return (false, "The assigned table is unavailable.");
        }

        if (reservation.DiningSession is not null)
        {
            return (false, "This reservation has already been checked in.");
        }

        if (await db.DiningSessions.AnyAsync(item =>
                item.DiningTableId == reservation.DiningTableId && item.Status == "Active"))
        {
            return (false, $"Table {reservation.DiningTable.TableNumber} currently has an active dining session.");
        }

        if (await db.DiningSessions.AnyAsync(item =>
                item.CustomerId == reservation.CustomerId && item.Status == "Active"))
        {
            return (false, "This customer already has an active dining session.");
        }

        var session = new DiningSession
        {
            DiningTableId = reservation.DiningTableId.Value,
            ReservationId = reservation.ReservationId,
            CustomerId = reservation.CustomerId,
            Pax = reservation.Pax,
            Status = "Active"
        };

        reservation.Status = "CheckedIn";
        reservation.DiningTable.Status = "Occupied";
        db.DiningSessions.Add(session);
        AddAudit(staffUserId, "Reservation checked in", nameof(Reservation), reservation.ReservationId,
            $"Booking {reservation.BookingCode}; table {reservation.DiningTable.TableNumber}.");
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return (true,
            $"{reservation.Customer?.FullName ?? "Customer"} checked in to table {reservation.DiningTable.TableNumber}.");
    }

    public async Task<(bool Succeeded, string Message, DiningSession? Session)> CreateWalkInAsync(
        WalkInViewModel input, int staffUserId)
    {
        if (input.DiningTableId is null)
        {
            return (false, "Please select a table.", null);
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var table = await db.DiningTables.SingleOrDefaultAsync(item => item.DiningTableId == input.DiningTableId);
        if (table is null || !table.IsActive || table.Status != "Available")
        {
            return (false, "The selected table is not available.", null);
        }

        if (table.Capacity < input.Pax)
        {
            return (false, $"Table {table.TableNumber} only seats up to {table.Capacity} guest(s).", null);
        }

        var now = DateTime.Now;
        if (!await availability.IsTableAvailableAsync(table.DiningTableId, now, now.AddHours(2)))
        {
            return (false, "This table has an upcoming reservation and cannot be used for a two-hour walk-in session.", null);
        }

        if (await db.DiningSessions.AnyAsync(item =>
                item.DiningTableId == table.DiningTableId && item.Status == "Active"))
        {
            return (false, "This table already has an active dining session.", null);
        }

        var session = new DiningSession
        {
            DiningTableId = table.DiningTableId,
            Pax = input.Pax,
            Status = "Active"
        };

        table.Status = "Occupied";
        db.DiningSessions.Add(session);
        await db.SaveChangesAsync();
        AddAudit(staffUserId, "Walk-in seated", nameof(DiningSession), session.DiningSessionId,
            $"Table {table.TableNumber}; pax {input.Pax}.");
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return (true, $"Walk-in seated at table {table.TableNumber}.", session);
    }

    private async Task<string> GenerateBookingCodeAsync()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = $"QIGLO-{Random.Shared.Next(100000, 999999)}";
            if (!await db.Reservations.AnyAsync(item => item.BookingCode == code))
            {
                return code;
            }
        }

        return $"QIGLO-{DateTime.Now:yyMMddHHmmss}";
    }

    private void AddAudit(int? userId, string action, string entityName, int? entityId, string details) =>
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId?.ToString(),
            Details = details.Length <= 1000 ? details : details[..1000]
        });

    private static string? AppendNote(string? existing, string? value, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return existing;
        }

        var suffix = $"[{label}: {value.Trim()}]";
        var combined = string.IsNullOrWhiteSpace(existing) ? suffix : $"{existing.Trim()} {suffix}";
        return combined.Length <= 500 ? combined : combined[..500];
    }

    private static string NormalizeForAudit(string? value)
    {
        var result = string.IsNullOrWhiteSpace(value) ? "Not provided" : value.Trim();
        return result.Length <= 500 ? result : result[..500];
    }
}
