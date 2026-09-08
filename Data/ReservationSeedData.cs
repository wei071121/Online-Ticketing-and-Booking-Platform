using Microsoft.EntityFrameworkCore;
using QigloRestaurant.Models.Entities;

namespace QigloRestaurant.Web.Data;

// Idempotent reservation demonstration data. A dedicated booking customer is
// used so the ready-to-check-in sample never conflicts with the separate
// active customer dining session used by the ordering demonstration.
public static class ReservationSeedData
{
    public static async Task SeedAsync(DB db, Helper helper)
    {
        var staff = await db.Users.SingleOrDefaultAsync(item => item.Email == "staff@qiglo.test");
        if (staff is null)
        {
            return;
        }

        var bookingCustomer = await db.Users
            .SingleOrDefaultAsync(item => item.Email == "booking@qiglo.test");
        if (bookingCustomer is null)
        {
            bookingCustomer = new User
            {
                FullName = "Booking Demo Customer",
                Email = "booking@qiglo.test",
                PasswordHash = helper.HashPassword("Booking123!"),
                PhoneNumber = "0123456799",
                Role = "Customer"
            };
            db.Users.Add(bookingCustomer);
        }

        var table4 = await db.DiningTables.SingleOrDefaultAsync(item => item.TableNumber == "T04");
        if (table4 is null)
        {
            table4 = new DiningTable { TableNumber = "T04", Capacity = 2 };
            db.DiningTables.Add(table4);
        }

        var table5 = await db.DiningTables.SingleOrDefaultAsync(item => item.TableNumber == "T05");
        if (table5 is null)
        {
            table5 = new DiningTable { TableNumber = "T05", Capacity = 8 };
            db.DiningTables.Add(table5);
        }

        var additionalTables = new (string Number, int Capacity)[]
        {
            ("T06", 2),
            ("T07", 4),
            ("T08", 4),
            ("T09", 6),
            ("T10", 8),
            ("T11", 10),
            ("T12", 12)
        };
        var existingTableNumbers = await db.DiningTables
            .Select(item => item.TableNumber)
            .ToListAsync();
        foreach (var definition in additionalTables.Where(item => !existingTableNumbers.Contains(item.Number)))
        {
            db.DiningTables.Add(new DiningTable
            {
                TableNumber = definition.Number,
                Capacity = definition.Capacity
            });
        }

        await db.SaveChangesAsync();

        var pending = await db.Reservations.SingleOrDefaultAsync(item => item.BookingCode == "QIGLO-100201");
        if (pending is null)
        {
            pending = new Reservation
            {
                CustomerId = bookingCustomer.UserId,
                DiningTableId = table4.DiningTableId,
                StartAt = DateTime.Today.AddDays(1).AddHours(19),
                EndAt = DateTime.Today.AddDays(1).AddHours(21),
                Pax = 2,
                Status = "Pending",
                BookingCode = "QIGLO-100201"
            };
            db.Reservations.Add(pending);
        }
        else if (pending.Status == "Pending")
        {
            pending.StartAt = DateTime.Today.AddDays(1).AddHours(19);
            pending.EndAt = DateTime.Today.AddDays(1).AddHours(21);
            pending.CreatedAt = DateTime.Now;
        }

        var confirmed = await db.Reservations.SingleOrDefaultAsync(item => item.BookingCode == "QIGLO-100202");
        if (confirmed is null)
        {
            var start = DateTime.Now.AddMinutes(15);
            confirmed = new Reservation
            {
                CustomerId = bookingCustomer.UserId,
                DiningTableId = table5.DiningTableId,
                StartAt = start,
                EndAt = start.AddHours(2),
                Pax = 6,
                Status = "Confirmed",
                BookingCode = "QIGLO-100202",
                ApprovedByUserId = staff.UserId,
                ApprovedAt = DateTime.Now
            };
            db.Reservations.Add(confirmed);
        }
        else if (confirmed.Status == "Confirmed")
        {
            confirmed.StartAt = DateTime.Now.AddMinutes(15);
            confirmed.EndAt = confirmed.StartAt.AddHours(2);
        }

        if (!await db.Reservations.AnyAsync(item => item.BookingCode == "QIGLO-100203"))
        {
            db.Reservations.Add(new Reservation
            {
                CustomerId = bookingCustomer.UserId,
                DiningTableId = table4.DiningTableId,
                StartAt = DateTime.Today.AddDays(-1).AddHours(18),
                EndAt = DateTime.Today.AddDays(-1).AddHours(20),
                Pax = 2,
                Status = "Rejected",
                BookingCode = "QIGLO-100203",
                ApprovedByUserId = staff.UserId,
                ApprovedAt = DateTime.Now.AddDays(-1),
                Notes = "[Rejected: Table under maintenance]"
            });
        }

        await db.SaveChangesAsync();
    }
}
