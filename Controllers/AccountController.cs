using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QigloRestaurant.Models.Entities;
using QigloRestaurant.Web.Models;
using QigloRestaurant.Web.Services;

namespace QigloRestaurant.Web.Controllers;

public class AccountController(
    DB db,
    Helper hp,
    IAccountEmailService emailService) : Controller
{
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var normalizedEmail = NormalizeEmail(model.Email);
        var user = await db.Users.SingleOrDefaultAsync(item => item.Email == normalizedEmail);
        var now = DateTime.Now;
        if (user?.LockoutEnd > now)
        {
            ModelState.AddModelError(string.Empty, $"Account is temporarily locked until {user.LockoutEnd:HH:mm}.");
            return View(model);
        }

        var valid = user is not null && user.IsActive && hp.VerifyPassword(user.PasswordHash, model.Password);
        db.LoginAttempts.Add(new LoginAttempt
        {
            UserId = user?.UserId,
            EmailEntered = normalizedEmail,
            IsSuccessful = valid,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            AttemptedAt = now
        });

        if (!valid)
        {
            if (user is not null)
            {
                user.FailedLoginCount++;
                if (user.FailedLoginCount >= 3)
                {
                    user.LockoutEnd = now.AddMinutes(5);
                    user.FailedLoginCount = 0;
                    AddAudit(user.UserId, "Account locked", nameof(User), user.UserId.ToString(), "Three failed login attempts; locked for five minutes.");
                }
            }
            await db.SaveChangesAsync();
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        user!.FailedLoginCount = 0;
        user.LockoutEnd = null;
        AddAudit(user.UserId, "Login", nameof(User), user.UserId.ToString(), "Successful manual cookie login.");
        await db.SaveChangesAsync();
        await hp.SignInAsync(user, model.RememberMe);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View(new RegisterViewModel());
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        var normalizedEmail = NormalizeEmail(model.Email);
        if (await db.Users.AnyAsync(item => item.Email == normalizedEmail))
            ModelState.AddModelError(nameof(model.Email), "An account already uses this email.");
        if (!ModelState.IsValid) return View(model);

        var user = new User
        {
            FullName = model.FullName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = model.PhoneNumber?.Trim(),
            PasswordHash = hp.HashPassword(model.Password),
            Role = "Customer",
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        AddAudit(user.UserId, "Register", nameof(User), user.UserId.ToString(), "Customer self-registration completed.");
        await db.SaveChangesAsync();
        await hp.SignInAsync(user, false);
        TempData["Success"] = "Your customer account was created successfully.";
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var user = await CurrentUserAsync();
        if (user is null) return Challenge();
        return View(new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive
        });
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await CurrentUserAsync();
        if (user is null) return Challenge();
        var normalizedEmail = NormalizeEmail(model.Email);
        if (await db.Users.AnyAsync(item => item.UserId != user.UserId && item.Email == normalizedEmail))
            ModelState.AddModelError(nameof(model.Email), "That email address is already in use.");
        if (!ModelState.IsValid)
        {
            model.Role = user.Role;
            model.CreatedAt = user.CreatedAt;
            model.IsActive = user.IsActive;
            return View(model);
        }

        user.FullName = model.FullName.Trim();
        user.Email = normalizedEmail;
        user.PhoneNumber = model.PhoneNumber?.Trim();
        AddAudit(user.UserId, "Profile updated", nameof(User), user.UserId.ToString(), "Profile contact details changed.");
        await db.SaveChangesAsync();
        await hp.SignInAsync(user, false);
        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var user = await CurrentUserAsync();
        if (user is null) return Challenge();
        if (!ModelState.IsValid) return View(model);
        if (!hp.VerifyPassword(user.PasswordHash, model.CurrentPassword))
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Your current password is incorrect.");
            return View(model);
        }

        user.PasswordHash = hp.HashPassword(model.NewPassword);
        AddAudit(user.UserId, "Password changed", nameof(User), user.UserId.ToString(), "Password changed by authenticated user.");
        await db.SaveChangesAsync();
        TempData["Success"] = "Your password has been changed.";
        return RedirectToAction(nameof(Profile));
    }

    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await db.Users.SingleOrDefaultAsync(item =>
            item.Email == NormalizeEmail(model.Email) && item.IsActive);
        if (user is not null)
        {
            var now = DateTime.Now;
            var oldTokens = await db.PasswordResetTokens
                .Where(item => item.UserId == user.UserId && item.UsedAt == null)
                .ToListAsync();
            foreach (var oldToken in oldTokens) oldToken.UsedAt = now;

            var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            db.PasswordResetTokens.Add(new PasswordResetToken
            {
                UserId = user.UserId,
                TokenHash = HashToken(rawToken),
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(30)
            });
            AddAudit(user.UserId, "Password reset requested", nameof(User), user.UserId.ToString(), "One-time 30-minute reset token generated.");
            await db.SaveChangesAsync();

            var resetUrl = Url.Action(nameof(ResetPassword), "Account", new { token = rawToken }, Request.Scheme)!;
            await emailService.SendPasswordResetAsync(user, resetUrl);
        }

        // Never reveal whether an account exists or expose its reset token to the requester.
        TempData["Success"] = "If that active account exists, reset instructions have been sent to its email address.";
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult ResetPassword(string? token) =>
        View(new ResetPasswordViewModel { Token = token ?? string.Empty });

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var tokenHash = HashToken(model.Token);
        var reset = await db.PasswordResetTokens
            .Include(item => item.User)
            .SingleOrDefaultAsync(item =>
                item.TokenHash == tokenHash && item.UsedAt == null && item.ExpiresAt > DateTime.Now);
        if (reset is null)
        {
            ModelState.AddModelError(string.Empty, "This password-reset link is invalid, expired or already used.");
            return View(model);
        }

        reset.User.PasswordHash = hp.HashPassword(model.NewPassword);
        reset.User.FailedLoginCount = 0;
        reset.User.LockoutEnd = null;
        reset.UsedAt = DateTime.Now;
        AddAudit(reset.UserId, "Password reset completed", nameof(User), reset.UserId.ToString(), "Password reset using a one-time token.");
        await db.SaveChangesAsync();
        TempData["Success"] = "Password reset complete. You can now log in.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = CurrentUserIdOrNull();
        if (userId.HasValue)
        {
            AddAudit(userId, "Logout", nameof(User), userId.Value.ToString(), "Manual cookie logout.");
            await db.SaveChangesAsync();
        }
        await hp.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private async Task<User?> CurrentUserAsync()
    {
        var userId = CurrentUserIdOrNull();
        return userId.HasValue ? await db.Users.FindAsync(userId.Value) : null;
    }

    private int? CurrentUserIdOrNull() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var value) ? value : null;

    private void AddAudit(int? userId, string action, string entityName, string entityId, string details) =>
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details
        });

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
