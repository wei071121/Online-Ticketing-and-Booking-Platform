using Microsoft.EntityFrameworkCore;
using QigloRestaurant.Models.Entities;
using QigloRestaurant.Web.Models;

namespace QigloRestaurant.Web.Services;

public interface ITableService
{
    Task<List<DiningTable>> GetAllAsync();
    Task<DiningTable?> GetByIdAsync(int id);
    Task<(bool Succeeded, string? Error, DiningTable? Table)> CreateAsync(
        TableFormViewModel input, int actingUserId);
    Task<(bool Succeeded, string? Error)> UpdateAsync(
        TableFormViewModel input, int actingUserId);
    Task<(bool Succeeded, string Message)> DeleteAsync(int id, int actingUserId);
}

public sealed class TableService(DB db) : ITableService
{
    public Task<List<DiningTable>> GetAllAsync() => db.DiningTables
        .AsNoTracking()
        .OrderBy(item => item.TableNumber)
        .ToListAsync();

    public Task<DiningTable?> GetByIdAsync(int id) => db.DiningTables
        .AsNoTracking()
        .SingleOrDefaultAsync(item => item.DiningTableId == id);

    public async Task<(bool Succeeded, string? Error, DiningTable? Table)> CreateAsync(
        TableFormViewModel input, int actingUserId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        var number = input.TableNumber.Trim().ToUpperInvariant();
        if (input.Status == "Occupied")
        {
            return (false, "A new table cannot start as Occupied. Occupied status is set automatically when a dining session starts.", null);
        }

        if (await db.DiningTables.AnyAsync(item => item.TableNumber == number))
        {
            return (false, "A table with this number already exists.", null);
        }

        var table = new DiningTable
        {
            TableNumber = number,
            Capacity = input.Capacity,
            Status = input.Status,
            IsActive = input.IsActive
        };

        db.DiningTables.Add(table);
        await db.SaveChangesAsync();
        AddAudit(actingUserId, "Table created", table, $"Capacity {table.Capacity}; status {table.Status}.");
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return (true, null, table);
    }

    public async Task<(bool Succeeded, string? Error)> UpdateAsync(
        TableFormViewModel input, int actingUserId)
    {
        var table = await db.DiningTables.SingleOrDefaultAsync(item => item.DiningTableId == input.DiningTableId);
        if (table is null)
        {
            return (false, "Table not found.");
        }

        var number = input.TableNumber.Trim().ToUpperInvariant();
        if (await db.DiningTables.AnyAsync(item =>
                item.TableNumber == number && item.DiningTableId != input.DiningTableId))
        {
            return (false, "A table with this number already exists.");
        }

        var activeSession = await db.DiningSessions
            .AsNoTracking()
            .Where(item => item.DiningTableId == table.DiningTableId && item.Status == "Active")
            .OrderByDescending(item => item.StartedAt)
            .Select(item => new { item.DiningSessionId, item.Pax })
            .FirstOrDefaultAsync();

        if (activeSession is not null)
        {
            if (!input.IsActive || input.Status != "Occupied")
            {
                return (false, "This table has an active dining session and must remain Active and Occupied.");
            }

            if (input.Capacity < activeSession.Pax)
            {
                return (false, $"Capacity cannot be lower than the active party size ({activeSession.Pax}).");
            }
        }
        else if (input.Status == "Occupied")
        {
            return (false, "Occupied status can only be set automatically by check-in or walk-in seating.");
        }

        table.TableNumber = number;
        table.Capacity = input.Capacity;
        table.Status = input.Status;
        table.IsActive = input.IsActive;
        AddAudit(actingUserId, "Table updated", table,
            $"Number {table.TableNumber}; capacity {table.Capacity}; status {table.Status}; active {table.IsActive}.");
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Succeeded, string Message)> DeleteAsync(int id, int actingUserId)
    {
        var table = await db.DiningTables.SingleOrDefaultAsync(item => item.DiningTableId == id);
        if (table is null)
        {
            return (false, "Table not found.");
        }

        var hasOpenActivity = await db.Reservations.AnyAsync(item =>
                item.DiningTableId == id &&
                (item.Status == "Pending" || item.Status == "Confirmed" || item.Status == "CheckedIn")) ||
            await db.DiningSessions.AnyAsync(item => item.DiningTableId == id && item.Status == "Active");
        if (hasOpenActivity)
        {
            return (false, "This table has an active reservation or dining session and cannot be removed.");
        }

        var hasHistory = await db.Reservations.AnyAsync(item => item.DiningTableId == id) ||
            await db.DiningSessions.AnyAsync(item => item.DiningTableId == id);
        if (hasHistory)
        {
            table.IsActive = false;
            table.Status = "Unavailable";
            AddAudit(actingUserId, "Table deactivated", table, "Historical records were retained.");
            await db.SaveChangesAsync();
            return (true, $"Table {table.TableNumber} has history, so it was deactivated instead of deleted.");
        }

        AddAudit(actingUserId, "Table deleted", table, "Unused table removed.");
        db.DiningTables.Remove(table);
        await db.SaveChangesAsync();
        return (true, $"Table {table.TableNumber} was deleted.");
    }

    private void AddAudit(int userId, string action, DiningTable table, string details) =>
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = nameof(DiningTable),
            EntityId = table.DiningTableId == 0 ? null : table.DiningTableId.ToString(),
            Details = $"Table {table.TableNumber}. {details}"
        });
}
