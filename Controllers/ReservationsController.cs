using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QigloRestaurant.Models.Entities;
using QigloRestaurant.Web.Models;
using QigloRestaurant.Web.Services;

namespace QigloRestaurant.Web.Controllers;

// Reservation and seating: Reservation Module, Reservation Management, Check-in, Walk-in.
[Authorize]
public class ReservationsController(
    IReservationService reservationService,
    ITableAvailabilityService availabilityService,
    IReservationEmailService emailService,
    IQrCodeService qrCodeService) : Controller
{
    [Authorize(Roles = "Customer,Staff,Admin")]
    public async Task<IActionResult> Index(string? status = null)
    {
        ViewBag.StatusFilter = status ?? "All";

        if (User.IsInRole("Customer"))
        {
            return View(await reservationService.GetForCustomerAsync(CurrentUserId()));
        }

        return View(await reservationService.GetAllAsync(status));
    }

    [Authorize(Roles = "Customer")]
    public IActionResult Create() => View(new CreateReservationViewModel());

    [Authorize(Roles = "Customer")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateReservationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        (bool Succeeded, string? Error, Reservation? Reservation) result;
        try
        {
            result = await reservationService.CreateAsync(model, CurrentUserId());
        }
        catch (Exception exception) when (DatabaseConflict.IsDeadlock(exception))
        {
            ModelState.AddModelError(
                string.Empty,
                "Another booking was saved at the same time. Please check availability and try again.");
            return View(model);
        }
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] =
            $"Reservation requested. Your booking code is {result.Reservation!.BookingCode}. Wait for staff approval.";
        return RedirectToAction(nameof(Index));
    }

    // AJAX availability checking presents customer-friendly time choices. Exact
    // table numbers remain an operational concern and are assigned server-side.
    [Authorize(Roles = "Customer")]
    [HttpGet]
    public async Task<IActionResult> AvailabilityJson(
        DateTime reservationDate, int durationMinutes, int pax)
    {
        if (reservationDate == default || pax < 1 || pax > 30)
        {
            return Json(new { reservationDate, pax, slots = Array.Empty<AvailableTimeSlotOption>() });
        }

        var slots = await availabilityService.GetAvailableTimeSlotsAsync(
            reservationDate, pax, Math.Max(30, durationMinutes));
        return Json(new { reservationDate = reservationDate.Date, pax, slots });
    }

    [Authorize(Roles = "Staff,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? returnUrl = null)
    {
        (bool Succeeded, string Message, Reservation? Reservation) result;
        try
        {
            result = await reservationService.ApproveAsync(id, CurrentUserId(), null);
        }
        catch (Exception exception) when (DatabaseConflict.IsDeadlock(exception))
        {
            TempData["Error"] = "Another booking changed at the same time. Please review the list and try again.";
            return RedirectBack(returnUrl);
        }
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        if (result.Succeeded && result.Reservation is not null)
        {
            try
            {
                TempData["EmailStatus"] = await emailService.SendConfirmationAsync(result.Reservation);
            }
            catch
            {
                TempData["EmailStatus"] = "Reservation confirmed, but the confirmation email could not be sent.";
            }
        }
        return RedirectBack(returnUrl);
    }

    [Authorize(Roles = "Staff,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(RejectReservationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "The rejection reason must not exceed 250 characters.";
            return RedirectBack(model.ReturnUrl);
        }

        var result = await reservationService.RejectAsync(model.Id, CurrentUserId(), model.Reason);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectBack(model.ReturnUrl);
    }

    [Authorize(Roles = "Customer,Staff,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? returnUrl = null)
    {
        var isStaff = User.IsInRole("Staff") || User.IsInRole("Admin");
        var result = await reservationService.CancelAsync(id, CurrentUserId(), isStaff);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
        return RedirectBack(returnUrl);
    }

    [Authorize(Roles = "Staff,Admin")]
    public async Task<IActionResult> CheckIn()
    {
        ViewBag.ConfirmedReservations = await reservationService.GetAllAsync("Confirmed");
        return View(new CheckInViewModel());
    }

    [Authorize(Roles = "Staff,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(CheckInViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ConfirmedReservations = await reservationService.GetAllAsync("Confirmed");
            return View(model);
        }

        (bool Succeeded, string Message) result;
        try
        {
            result = await reservationService.CheckInAsync(model.BookingCode, CurrentUserId());
        }
        catch (Exception exception) when (DatabaseConflict.IsDeadlock(exception))
        {
            ModelState.AddModelError(string.Empty, "The table changed at the same time. Please try check-in again.");
            ViewBag.ConfirmedReservations = await reservationService.GetAllAsync("Confirmed");
            return View(model);
        }
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            ViewBag.ConfirmedReservations = await reservationService.GetAllAsync("Confirmed");
            return View(model);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(CheckIn));
    }

    [Authorize(Roles = "Staff,Admin")]
    public IActionResult WalkIn()
    {
        return View(new WalkInViewModel());
    }

    [Authorize(Roles = "Staff,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> WalkIn(WalkInViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        (bool Succeeded, string Message, DiningSession? Session) result;
        try
        {
            result = await reservationService.CreateWalkInAsync(model, CurrentUserId());
        }
        catch (Exception exception) when (DatabaseConflict.IsDeadlock(exception))
        {
            ModelState.AddModelError(string.Empty, "The table changed at the same time. Please choose an available table again.");
            return View(model);
        }
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index), "Tables");
    }

    // AJAX table availability checking - called from the walk-in page whenever
    // staff changes the party size.
    [Authorize(Roles = "Staff,Admin")]
    [HttpGet]
    public async Task<IActionResult> WalkInAvailabilityJson(int pax)
    {
        return Json(ToOptions(await ImmediateAvailableTablesAsync(pax)));
    }

    [Authorize(Roles = "Customer,Staff,Admin")]
    [HttpGet]
    public async Task<IActionResult> Qr(int id)
    {
        var reservation = await reservationService.GetByIdAsync(id);
        if (reservation is null)
        {
            return NotFound();
        }

        if (User.IsInRole("Customer") && reservation.CustomerId != CurrentUserId())
        {
            return Forbid();
        }

        if (reservation.Status is not ("Pending" or "Confirmed"))
        {
            TempData["Error"] = "A QR code is only available for an active booking.";
            return RedirectToAction(nameof(Index));
        }

        return View(reservation);
    }

    [Authorize(Roles = "Customer,Staff,Admin")]
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> QrCode(int id)
    {
        var reservation = await reservationService.GetByIdAsync(id);
        if (reservation is null)
        {
            return NotFound();
        }

        if (User.IsInRole("Customer") && reservation.CustomerId != CurrentUserId())
        {
            return Forbid();
        }

        if (reservation.Status is not ("Pending" or "Confirmed"))
        {
            return BadRequest("A QR code is only available for an active booking.");
        }

        return File(qrCodeService.CreatePng(reservation.BookingCode), "image/png");
    }

    private async Task<List<DiningTable>> ImmediateAvailableTablesAsync(int pax)
    {
        var now = DateTime.Now;
        var tables = await availabilityService.GetAvailableTablesAsync(now, now.AddHours(2), Math.Max(1, pax));
        return tables.Where(table => table.Status == "Available").ToList();
    }

    private static List<AvailableTableOption> ToOptions(IEnumerable<DiningTable> tables) =>
        tables.Select(table => new AvailableTableOption
        {
            DiningTableId = table.DiningTableId,
            TableNumber = table.TableNumber,
            Capacity = table.Capacity
        }).ToList();

    private IActionResult RedirectBack(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
