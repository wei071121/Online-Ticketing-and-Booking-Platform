using System.ComponentModel.DataAnnotations;
using QigloRestaurant.Models.Entities;

namespace QigloRestaurant.Web.Models;

// View Models ----------------------------------------------------------------

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required, StringLength(100), Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(20), Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$",
        ErrorMessage = "Use uppercase, lowercase, a number and a symbol.")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password)), Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

// Account and security: account security and profile ------------------------------------
public class ProfileViewModel
{
    [Required, StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    public string Role { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }
}

public class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password), Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$",
        ErrorMessage = "Use uppercase, lowercase, a number and a symbol.")]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(NewPassword))]
    [Display(Name = "Confirm new password")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public class ForgotPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$",
        ErrorMessage = "Use uppercase, lowercase, a number and a symbol.")]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(NewPassword))]
    [Display(Name = "Confirm new password")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public class AdminReportViewModel
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalOrders { get; set; }
    public int OpenOrders { get; set; }
    public int PaidTransactions { get; set; }
    public decimal PaidRevenue { get; set; }
    public int TotalReservations { get; set; }
    public int ConfirmedReservations { get; set; }
    public List<DailySalesViewModel> DailySales { get; set; } = [];
    public List<PopularItemViewModel> PopularItems { get; set; } = [];
}

public class DailySalesViewModel
{
    public DateTime Date { get; set; }
    public int Transactions { get; set; }
    public decimal Revenue { get; set; }
}

public class PopularItemViewModel
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class AdminDashboardViewModel
{
    public int ActiveUsers { get; set; }
    public int ActiveTables { get; set; }
    public int PendingReservations { get; set; }
    public int ActiveDiningSessions { get; set; }
    public int OpenOrders { get; set; }
    public decimal TodayRevenue { get; set; }
    public List<DailySalesViewModel> DailySales { get; set; } = [];
    public List<PopularItemViewModel> PopularItems { get; set; } = [];
    public List<AuditLog> RecentAuditLogs { get; set; } = [];
}

public class AdminUserFormViewModel
{
    public int UserId { get; set; }

    [Required, StringLength(100), Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(20), Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [Required, RegularExpression("^(Customer|Staff|KitchenStaff|Admin)$")]
    public string Role { get; set; } = "Customer";

    [Display(Name = "Active account")]
    public bool IsActive { get; set; } = true;

    [DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
    [Display(Name = "Password (leave blank to keep current)")]
    public string? Password { get; set; }
}

// Order ----------------------------------------------------------------------

public class CreateOrderViewModel
{
    [Required]
    [Display(Name = "Dining session")]
    public int DiningSessionId { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public List<OrderMenuItemInput> Items { get; set; } = [];
    public List<DiningSessionOption> DiningSessions { get; set; } = [];
}

public class OrderMenuItemInput
{
    public int MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }

    [Range(0, 20)]
    public int Quantity { get; set; }

    [StringLength(300)]
    public string? SpecialRequest { get; set; }

    public List<int> SelectedAddOnIds { get; set; } = [];
    public List<AddOnOption> AddOns { get; set; } = [];
}

public class AddOnOption
{
    public int AddOnId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class DiningSessionOption
{
    public int DiningSessionId { get; set; }
    public string Label { get; set; } = string.Empty;
}

// Payment --------------------------------------------------------------------

public class PaymentCreateViewModel
{
    public int DiningSessionId { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ServiceCharge { get; set; }
    public decimal TotalAmount { get; set; }
    public List<BillingLineViewModel> Lines { get; set; } = [];

    [Required, StringLength(20)]
    [RegularExpression("^(Cash|Card|EWallet)$")]
    [Display(Name = "Payment method")]
    public string PaymentMethod { get; set; } = "Cash";

    [Range(typeof(decimal), "0.01", "999999.99")]
    [Display(Name = "Amount received")]
    public decimal AmountReceived { get; set; }
}

public class BillingLineViewModel
{
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPriceWithAddOns { get; set; }
    public decimal LineTotal { get; set; }
}

public record PaymentCompletionResult(Payment Payment, string EmailStatus);
