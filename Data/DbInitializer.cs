using Microsoft.EntityFrameworkCore;
using QigloRestaurant.Models.Entities;
using QigloRestaurant.Web.Models;

namespace QigloRestaurant.Web.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        DB db,
        Helper helper)
    {
        await db.Database.MigrateAsync();

        if (await db.Users.AnyAsync())
        {
            return;
        }

        var customer = new User
        {
            FullName = "Demo Customer",
            Email = "customer@qiglo.test",
            PasswordHash = helper.HashPassword("Customer123!"),
            PhoneNumber = "0123456789",
            Role = "Customer"
        };
        var staff = new User
        {
            FullName = "Demo Staff",
            Email = "staff@qiglo.test",
            PasswordHash = helper.HashPassword("Staff123!"),
            PhoneNumber = "0123456790",
            Role = "Staff"
        };
        var kitchen = new User
        {
            FullName = "Demo Kitchen",
            Email = "kitchen@qiglo.test",
            PasswordHash = helper.HashPassword("Kitchen123!"),
            Role = "KitchenStaff"
        };
        var admin = new User
        {
            FullName = "Demo Admin",
            Email = "admin@qiglo.test",
            PasswordHash = helper.HashPassword("Admin123!"),
            Role = "Admin"
        };

        db.Users.AddRange(customer, staff, kitchen, admin);

        var table2 = new DiningTable { TableNumber = "T01", Capacity = 2 };
        var table4 = new DiningTable { TableNumber = "T02", Capacity = 4, Status = "Occupied" };
        var table6 = new DiningTable { TableNumber = "T03", Capacity = 6, Status = "Occupied" };
        db.DiningTables.AddRange(table2, table4, table6);

        var chicken = new Category { Name = "Chicken", Description = "Western chicken dishes" };
        var pasta = new Category { Name = "Pasta", Description = "Freshly cooked pasta" };
        var drinks = new Category { Name = "Drinks", Description = "Cold and hot beverages" };

        var chickenChop = new MenuItem
        {
            Category = chicken,
            Name = "Grilled Chicken Chop",
            Description = "Grilled chicken with black pepper sauce",
            Price = 18.90m
        };
        var carbonara = new MenuItem
        {
            Category = pasta,
            Name = "Chicken Carbonara",
            Description = "Creamy carbonara with sliced chicken",
            Price = 16.50m
        };
        var lemonTea = new MenuItem
        {
            Category = drinks,
            Name = "Iced Lemon Tea",
            Description = "Fresh lemon tea",
            Price = 5.50m
        };
        db.MenuItems.AddRange(chickenChop, carbonara, lemonTea);

        var cheese = new AddOn { Name = "Extra Cheese", Price = 2.50m };
        var egg = new AddOn { Name = "Sunny Side Egg", Price = 2.00m };
        var sauce = new AddOn { Name = "Extra Sauce", Price = 1.00m };
        db.AddOns.AddRange(cheese, egg, sauce);

        await db.SaveChangesAsync();

        db.MenuItemAddOns.AddRange(
            new MenuItemAddOn { MenuItemId = chickenChop.MenuItemId, AddOnId = egg.AddOnId },
            new MenuItemAddOn { MenuItemId = chickenChop.MenuItemId, AddOnId = sauce.AddOnId },
            new MenuItemAddOn { MenuItemId = carbonara.MenuItemId, AddOnId = cheese.AddOnId });

        var reservation = new Reservation
        {
            CustomerId = customer.UserId,
            DiningTableId = table4.DiningTableId,
            StartAt = DateTime.Now.AddHours(-1),
            EndAt = DateTime.Now.AddHours(2),
            Pax = 2,
            Status = "CheckedIn",
            BookingCode = "QIGLO-DEMO-01",
            ApprovedByUserId = staff.UserId,
            ApprovedAt = DateTime.Now.AddHours(-2)
        };
        db.Reservations.Add(reservation);
        await db.SaveChangesAsync();

        var customerSession = new DiningSession
        {
            DiningTableId = table4.DiningTableId,
            ReservationId = reservation.ReservationId,
            CustomerId = customer.UserId,
            Pax = 2,
            Status = "Active",
            StartedAt = DateTime.Now.AddMinutes(-30)
        };
        var walkInSession = new DiningSession
        {
            DiningTableId = table6.DiningTableId,
            Pax = 3,
            Status = "Active",
            StartedAt = DateTime.Now.AddMinutes(-20)
        };
        db.DiningSessions.AddRange(customerSession, walkInSession);
        await db.SaveChangesAsync();

        var demoOrder = new Order
        {
            DiningSessionId = walkInSession.DiningSessionId,
            CreatedByUserId = staff.UserId,
            OrderNumber = "ORD-DEMO-001",
            Status = "Pending",
            OrderedAt = DateTime.Now.AddMinutes(-10),
            OrderItems = new List<OrderItem>
            {
                new()
                {
                    MenuItemId = chickenChop.MenuItemId,
                    MenuItemName = chickenChop.Name,
                    UnitPrice = chickenChop.Price,
                    Quantity = 2,
                    OrderItemAddOns = new List<OrderItemAddOn>
                    {
                        new()
                        {
                            AddOnId = sauce.AddOnId,
                            AddOnName = sauce.Name,
                            UnitPrice = sauce.Price
                        }
                    }
                },
                new()
                {
                    MenuItemId = lemonTea.MenuItemId,
                    MenuItemName = lemonTea.Name,
                    UnitPrice = lemonTea.Price,
                    Quantity = 3
                }
            }
        };
        db.Orders.Add(demoOrder);
        await db.SaveChangesAsync();
    }
}
