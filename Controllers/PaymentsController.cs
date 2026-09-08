using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QigloRestaurant.Web.Models;
using QigloRestaurant.Web.Services;

namespace QigloRestaurant.Web.Controllers;

[Authorize]
public class PaymentsController(
    DB db,
    IBillingService billingService,
    IReceiptPdfService pdfService,
    IReceiptEmailService emailService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var query = db.DiningSessions
            .AsNoTracking()
            .Include(item => item.DiningTable)
            .Include(item => item.Customer)
            .Include(item => item.Payment)
            .Include(item => item.Orders)
            .AsQueryable();

        if (User.IsInRole("Customer"))
        {
            query = query.Where(item => item.CustomerId == CurrentUserId());
        }
        else if (!User.IsInRole("Staff") && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        return View(await query.OrderByDescending(item => item.StartedAt).ToListAsync());
    }

    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> Create(int diningSessionId)
    {
        var model = await billingService.BuildPaymentAsync(diningSessionId);
        return model is null ? NotFound() : View(model);
    }

    [Authorize(Roles = "Staff,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PaymentCreateViewModel model)
    {
        var calculated = await billingService.BuildPaymentAsync(model.DiningSessionId);
        if (calculated is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            calculated.PaymentMethod = model.PaymentMethod;
            calculated.AmountReceived = model.AmountReceived;
            return View(calculated);
        }

        var result = await billingService.CompletePaymentAsync(model, CurrentUserId());
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            calculated.PaymentMethod = model.PaymentMethod;
            calculated.AmountReceived = model.AmountReceived;
            return View(calculated);
        }

        var pdf = pdfService.Create(result.Payment!);
        TempData["EmailStatus"] = await emailService.SendAsync(result.Payment!, pdf);
        TempData["Success"] = $"Payment completed. Receipt {result.Payment!.ReceiptNumber} generated.";
        return RedirectToAction(nameof(Details), new { id = result.Payment.PaymentId });
    }

    public async Task<IActionResult> Details(int id)
    {
        var payment = await billingService.GetReceiptAsync(id);
        if (payment is null)
        {
            return NotFound();
        }

        if (User.IsInRole("Customer") && payment.DiningSession.CustomerId != CurrentUserId())
        {
            return Forbid();
        }

        if (User.IsInRole("KitchenStaff"))
        {
            return Forbid();
        }

        return View(payment);
    }

    public async Task<IActionResult> Pdf(int id)
    {
        var payment = await billingService.GetReceiptAsync(id);
        if (payment is null)
        {
            return NotFound();
        }

        if (User.IsInRole("Customer") && payment.DiningSession.CustomerId != CurrentUserId())
        {
            return Forbid();
        }

        if (User.IsInRole("KitchenStaff"))
        {
            return Forbid();
        }

        return File(
            pdfService.Create(payment),
            "application/pdf",
            $"{payment.ReceiptNumber}.pdf");
    }

    [Authorize(Roles = "Staff,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendEmail(int id)
    {
        var payment = await billingService.GetReceiptAsync(id);
        if (payment is null)
        {
            return NotFound();
        }

        TempData["EmailStatus"] = await emailService.SendAsync(payment, pdfService.Create(payment));
        return RedirectToAction(nameof(Details), new { id });
    }

    private int CurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
