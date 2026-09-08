using Microsoft.EntityFrameworkCore;
using QigloRestaurant.Models.Entities;
using QigloRestaurant.Web.Models;

namespace QigloRestaurant.Web.Data;

/// <summary>
/// Menu and cart demo data only. This does not change the shared schema or migrations.
/// It enriches an already-created team database with a fuller menu and photos once.
/// </summary>
public static class MenuDemoCatalog
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static async Task EnsureSeededAsync(DB db)
    {
        await Gate.WaitAsync();
        try
        {
            var categories = await db.Categories.ToListAsync();

            Category EnsureCategory(string name, string description)
            {
                var existing = categories.FirstOrDefault(x =>
                    string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
                if (existing is not null)
                {
                    return existing;
                }

                var category = new Category
                {
                    Name = name,
                    Description = description,
                    IsActive = true
                };
                db.Categories.Add(category);
                categories.Add(category);
                return category;
            }

            var chicken = EnsureCategory("Chicken", "Western chicken dishes");
            var pasta = EnsureCategory("Pasta", "Creamy and classic pasta dishes");
            var burger = EnsureCategory("Burger", "Western-style burgers");
            var fish = EnsureCategory("Fish", "Fish-based western meals");
            var sides = EnsureCategory("Side Dish", "Sides and snacks");
            var drinks = EnsureCategory("Drinks", "Cold and refreshing beverages");
            var dessert = EnsureCategory("Dessert", "Sweet dishes and desserts");

            await db.SaveChangesAsync();

            var specs = new[]
            {
                new MenuSpec("Grilled Chicken Chop", chicken.CategoryId,
                    "Juicy grilled chicken chop served with black pepper sauce, vegetables and fries.", 18.90m,
                    "/images/menu/chicken.jpg"),
                new MenuSpec("Crispy Chicken Cutlet", chicken.CategoryId,
                    "Golden crispy chicken cutlet served with fries, coleslaw and house sauce.", 17.90m,
                    "/images/menu/chicken.jpg"),
                new MenuSpec("Chicken Carbonara", pasta.CategoryId,
                    "Creamy carbonara pasta with sliced chicken, parmesan and black pepper.", 16.50m,
                    "/images/menu/pasta.jpg"),
                new MenuSpec("Aglio Olio", pasta.CategoryId,
                    "Garlic and olive oil spaghetti finished with herbs and chilli flakes.", 14.90m,
                    "/images/menu/pasta.jpg"),
                new MenuSpec("Classic Beef Burger", burger.CategoryId,
                    "Grilled beef patty with lettuce, cheese, tomato and QIGLO house sauce.", 19.90m,
                    "/images/menu/burger.jpg"),
                new MenuSpec("Crispy Chicken Burger", burger.CategoryId,
                    "Crispy chicken fillet with lettuce, cheese and creamy house sauce.", 17.90m,
                    "/images/menu/burger.jpg"),
                new MenuSpec("Fish & Chips", fish.CategoryId,
                    "Crispy battered fish served with fries, salad and tartar sauce.", 21.90m,
                    "/images/menu/fish.jpg"),
                new MenuSpec("French Fries", sides.CategoryId,
                    "Golden crispy fries seasoned lightly and served hot.", 7.90m,
                    "/images/menu/burger.jpg"),
                new MenuSpec("Iced Lemon Tea", drinks.CategoryId,
                    "Refreshing iced tea with lemon and plenty of ice.", 5.50m,
                    "/images/menu/lemon-tea.jpg"),
                new MenuSpec("Chocolate Brownie", dessert.CategoryId,
                    "Warm chocolate brownie served with vanilla ice cream and chocolate drizzle.", 10.90m,
                    "/images/menu/brownie.jpg")
            };

            var existingItems = await db.MenuItems
                .Include(x => x.Photos)
                .ToListAsync();

            foreach (var spec in specs)
            {
                var item = existingItems.FirstOrDefault(x =>
                    string.Equals(x.Name, spec.Name, StringComparison.OrdinalIgnoreCase));

                if (item is null)
                {
                    item = new MenuItem
                    {
                        CategoryId = spec.CategoryId,
                        Name = spec.Name,
                        Description = spec.Description,
                        Price = spec.Price,
                        IsAvailable = true,
                        CreatedAt = DateTime.Now
                    };
                    db.MenuItems.Add(item);
                    existingItems.Add(item);
                    await db.SaveChangesAsync();
                }
                else
                {
                    // Keep the team's existing item identity/order links, but make sure
                    // the demo item has useful presentation data.
                    if (string.IsNullOrWhiteSpace(item.Description))
                        item.Description = spec.Description;
                }

                if (!await db.MenuItemPhotos.AnyAsync(x => x.MenuItemId == item.MenuItemId))
                {
                    db.MenuItemPhotos.Add(new MenuItemPhoto
                    {
                        MenuItemId = item.MenuItemId,
                        FileName = spec.PhotoUrl,
                        AltText = spec.Name,
                        DisplayOrder = 0,
                        IsPrimary = true,
                        UploadedAt = DateTime.Now
                    });
                }
            }

            await db.SaveChangesAsync();

            async Task<AddOn> EnsureAddOnAsync(string name, decimal price)
            {
                var addOn = await db.AddOns.FirstOrDefaultAsync(x => x.Name == name);
                if (addOn is null)
                {
                    addOn = new AddOn { Name = name, Price = price, IsActive = true };
                    db.AddOns.Add(addOn);
                    await db.SaveChangesAsync();
                }
                return addOn;
            }

            var cheese = await EnsureAddOnAsync("Extra Cheese", 2.50m);
            var egg = await EnsureAddOnAsync("Sunny Side Egg", 2.00m);
            var sauce = await EnsureAddOnAsync("Extra Sauce", 1.00m);
            var friesAddon = await EnsureAddOnAsync("Extra Fries", 3.50m);
            var noOnion = await EnsureAddOnAsync("No Onion", 0.00m);
            var extraPatty = await EnsureAddOnAsync("Extra Patty", 5.50m);
            var tartar = await EnsureAddOnAsync("Extra Tartar Sauce", 1.50m);
            var parmesan = await EnsureAddOnAsync("Extra Parmesan", 2.00m);
            var iceCream = await EnsureAddOnAsync("Extra Ice Cream Scoop", 2.50m);

            var itemMap = (await db.MenuItems.AsNoTracking().ToListAsync())
                .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
            var desiredLinks = new (string Item, AddOn AddOn)[]
            {
                ("Grilled Chicken Chop", egg),
                ("Grilled Chicken Chop", sauce),
                ("Grilled Chicken Chop", friesAddon),
                ("Crispy Chicken Cutlet", sauce),
                ("Crispy Chicken Cutlet", friesAddon),
                ("Chicken Carbonara", cheese),
                ("Chicken Carbonara", parmesan),
                ("Aglio Olio", parmesan),
                ("Classic Beef Burger", cheese),
                ("Classic Beef Burger", noOnion),
                ("Classic Beef Burger", extraPatty),
                ("Crispy Chicken Burger", cheese),
                ("Crispy Chicken Burger", noOnion),
                ("Crispy Chicken Burger", friesAddon),
                ("Fish & Chips", tartar),
                ("Fish & Chips", friesAddon),
                ("Chocolate Brownie", iceCream)
            };

            foreach (var (itemName, addOn) in desiredLinks)
            {
                if (!itemMap.TryGetValue(itemName, out var item))
                    continue;

                var exists = await db.MenuItemAddOns.AnyAsync(x =>
                    x.MenuItemId == item.MenuItemId && x.AddOnId == addOn.AddOnId);
                if (!exists)
                {
                    db.MenuItemAddOns.Add(new MenuItemAddOn
                    {
                        MenuItemId = item.MenuItemId,
                        AddOnId = addOn.AddOnId
                    });
                }
            }

            await db.SaveChangesAsync();

        }
        finally
        {
            Gate.Release();
        }
    }

    private sealed record MenuSpec(
        string Name,
        int CategoryId,
        string Description,
        decimal Price,
        string PhotoUrl);
}
