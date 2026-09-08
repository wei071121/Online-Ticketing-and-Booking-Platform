using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QigloRestaurant.Models.Entities;
using QigloRestaurant.Web.Models;

namespace QigloRestaurant.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(DB db, Helper hp) : Controller
{
    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var sevenDaysAgo = today.AddDays(-6);
        var dailyRows = await db.Payments.AsNoTracking()
            .Where(item => item.Status == "Paid" && item.PaidAt >= sevenDaysAgo)
            .GroupBy(item => item.PaidAt!.Value.Date)
            .Select(group => new DailySalesViewModel
            {
                Date = group.Key,
                Transactions = group.Count(),
                Revenue = group.Sum(item => item.TotalAmount)
            })
            .ToListAsync();

        var model = new AdminDashboardViewModel
        {
            ActiveUsers = await db.Users.CountAsync(item => item.IsActive),
            ActiveTables = await db.DiningTables.CountAsync(item => item.IsActive),
            PendingReservations = await db.Reservations.CountAsync(item => item.Status == "Pending"),
            ActiveDiningSessions = await db.DiningSessions.CountAsync(item => item.Status == "Active"),
            OpenOrders = await db.Orders.CountAsync(item =>
                item.Status == "Pending" || item.Status == "Preparing" || item.Status == "Ready"),
            TodayRevenue = await db.Payments
                .Where(item => item.Status == "Paid" && item.PaidAt >= today && item.PaidAt < today.AddDays(1))
                .SumAsync(item => (decimal?)item.TotalAmount) ?? 0m,
            DailySales = Enumerable.Range(0, 7)
                .Select(offset => sevenDaysAgo.AddDays(offset))
                .Select(date => dailyRows.FirstOrDefault(item => item.Date == date) ??
                    new DailySalesViewModel { Date = date })
                .ToList(),
            PopularItems = await PopularItemsAsync(5),
            RecentAuditLogs = await db.AuditLogs.AsNoTracking()
                .Include(item => item.User)
                .OrderByDescending(item => item.CreatedAt)
                .Take(8)
                .ToListAsync()
        };
        return View(model);
    }

    public async Task<IActionResult> Users(string? search = null, string? role = null)
    {
        var query = db.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(item => item.FullName.Contains(search) || item.Email.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(role) && role != "All")
            query = query.Where(item => item.Role == role);
        ViewBag.Search = search;
        ViewBag.Role = role ?? "All";
        return View(await query.OrderBy(item => item.Role).ThenBy(item => item.FullName).ToListAsync());
    }

    [HttpGet]
    public IActionResult CreateUser() => View("UserForm", new AdminUserFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(AdminUserFormViewModel model)
    {
        var email = NormalizeEmail(model.Email);
        if (string.IsNullOrWhiteSpace(model.Password))
            ModelState.AddModelError(nameof(model.Password), "A password is required for a new user.");
        else if (!StrongPassword(model.Password))
            ModelState.AddModelError(nameof(model.Password), "Use uppercase, lowercase, a number and a symbol.");
        if (await db.Users.AnyAsync(item => item.Email == email))
            ModelState.AddModelError(nameof(model.Email), "An account already uses this email.");
        if (!ModelState.IsValid) return View("UserForm", model);

        var user = new User
        {
            FullName = model.FullName.Trim(),
            Email = email,
            PhoneNumber = model.PhoneNumber?.Trim(),
            Role = model.Role,
            IsActive = model.IsActive,
            PasswordHash = hp.HashPassword(model.Password!)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        AddAudit("Admin created user", user, $"Created {user.Role} account {user.Email}.");
        await db.SaveChangesAsync();
        TempData["Success"] = "User account created.";
        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(int id)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(item => item.UserId == id);
        if (user is null) return NotFound();
        return View("UserForm", new AdminUserFormViewModel
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            IsActive = user.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(int id, AdminUserFormViewModel model)
    {
        if (id != model.UserId) return BadRequest();
        var user = await db.Users.SingleOrDefaultAsync(item => item.UserId == id);
        if (user is null) return NotFound();
        var email = NormalizeEmail(model.Email);
        if (await db.Users.AnyAsync(item => item.UserId != id && item.Email == email))
            ModelState.AddModelError(nameof(model.Email), "An account already uses this email.");
        if (!string.IsNullOrWhiteSpace(model.Password) && !StrongPassword(model.Password))
            ModelState.AddModelError(nameof(model.Password), "Use uppercase, lowercase, a number and a symbol.");
        if (id == CurrentUserId() && (!model.IsActive || model.Role != "Admin"))
            ModelState.AddModelError(string.Empty, "You cannot disable or remove the Admin role from your own signed-in account.");
        if (!ModelState.IsValid) return View("UserForm", model);

        user.FullName = model.FullName.Trim();
        user.Email = email;
        user.PhoneNumber = model.PhoneNumber?.Trim();
        user.Role = model.Role;
        user.IsActive = model.IsActive;
        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            user.PasswordHash = hp.HashPassword(model.Password);
            user.FailedLoginCount = 0;
            user.LockoutEnd = null;
        }
        AddAudit("Admin updated user", user, $"Updated {user.Role} account {user.Email}; active={user.IsActive}.");
        await db.SaveChangesAsync();
        TempData["Success"] = "User account updated.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUser(int id)
    {
        var user = await db.Users.SingleOrDefaultAsync(item => item.UserId == id);
        if (user is null) return NotFound();
        if (id == CurrentUserId())
        {
            TempData["Error"] = "You cannot disable your own signed-in account.";
            return RedirectToAction(nameof(Users));
        }
        user.IsActive = !user.IsActive;
        if (!user.IsActive)
        {
            user.LockoutEnd = null;
            user.FailedLoginCount = 0;
        }
        AddAudit("Admin changed user status", user, $"Set active={user.IsActive} for {user.Email}.");
        await db.SaveChangesAsync();
        TempData["Success"] = user.IsActive ? "User account enabled." : "User account disabled.";
        return RedirectToAction(nameof(Users));
    }

    public async Task<IActionResult> Reports()
    {
        var paidPayments = db.Payments.AsNoTracking().Where(item => item.Status == "Paid");
        var model = new AdminReportViewModel
        {
            TotalUsers = await db.Users.CountAsync(),
            ActiveUsers = await db.Users.CountAsync(item => item.IsActive),
            TotalOrders = await db.Orders.CountAsync(),
            OpenOrders = await db.Orders.CountAsync(item =>
                item.Status == "Pending" || item.Status == "Preparing" || item.Status == "Ready"),
            PaidTransactions = await paidPayments.CountAsync(),
            PaidRevenue = await paidPayments.SumAsync(item => (decimal?)item.TotalAmount) ?? 0m,
            TotalReservations = await db.Reservations.CountAsync(),
            ConfirmedReservations = await db.Reservations.CountAsync(item =>
                item.Status == "Confirmed" || item.Status == "CheckedIn" || item.Status == "Completed"),
            DailySales = await paidPayments
                .Where(item => item.PaidAt != null)
                .GroupBy(item => item.PaidAt!.Value.Date)
                .OrderByDescending(group => group.Key)
                .Take(14)
                .Select(group => new DailySalesViewModel
                {
                    Date = group.Key,
                    Transactions = group.Count(),
                    Revenue = group.Sum(payment => payment.TotalAmount)
                })
                .ToListAsync(),
            PopularItems = await PopularItemsAsync(10)
        };
        return View(model);
    }

    public async Task<IActionResult> Audit()
    {
        var entries = await db.AuditLogs.AsNoTracking()
            .Include(item => item.User)
            .OrderByDescending(item => item.CreatedAt)
            .Take(200)
            .ToListAsync();
        return View(entries);
    }

    private Task<List<PopularItemViewModel>> PopularItemsAsync(int count) => db.OrderItems
        .AsNoTracking()
        .Where(item => item.Order.Status != "Cancelled")
        .GroupBy(item => item.MenuItemName)
        .Select(group => new PopularItemViewModel
        {
            Name = group.Key,
            Quantity = group.Sum(item => item.Quantity)
        })
        .OrderByDescending(item => item.Quantity)
        .Take(count)
        .ToListAsync();

    private void AddAudit(string action, User target, string details) => db.AuditLogs.Add(new AuditLog
    {
        UserId = CurrentUserId(),
        Action = action,
        EntityName = nameof(User),
        EntityId = target.UserId.ToString(),
        Details = details
    });

    private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    private static bool StrongPassword(string password) => Regex.IsMatch(
        password,
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$");
}
