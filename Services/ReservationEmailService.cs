using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;
using QigloRestaurant.Models.Entities;

namespace QigloRestaurant.Web.Services;

public interface IReservationEmailService
{
    Task<string> SendConfirmationAsync(Reservation reservation);
}

public sealed class ReservationEmailService(
    IOptions<EmailSettings> options,
    IWebHostEnvironment environment) : IReservationEmailService
{
    private readonly EmailSettings settings = options.Value;

    public async Task<string> SendConfirmationAsync(Reservation reservation)
    {
        var recipient = reservation.Customer?.Email;
        if (string.IsNullOrWhiteSpace(recipient))
        {
            return "Reservation confirmed, but no customer email is available.";
        }

        var body = BuildBody(reservation);
        if (string.IsNullOrWhiteSpace(settings.Host))
        {
            var outbox = Path.Combine(environment.ContentRootPath, "App_Data", "EmailOutbox");
            Directory.CreateDirectory(outbox);
            var safeCode = reservation.BookingCode.Replace("/", "-", StringComparison.Ordinal);
            await File.WriteAllTextAsync(
                Path.Combine(outbox, $"booking-confirmation-{safeCode}.html"),
                body,
                Encoding.UTF8);
            return "SMTP is not configured; a booking confirmation email was saved in App_Data/EmailOutbox.";
        }

        using var message = new MailMessage
        {
            From = new MailAddress(settings.FromEmail, settings.FromName),
            Subject = $"QIGLO booking confirmed - {reservation.BookingCode}",
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(recipient);

        using var smtp = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.EnableSsl,
            Credentials = new NetworkCredential(settings.Username, settings.Password)
        };
        await smtp.SendMailAsync(message);
        return $"Booking confirmation sent to {recipient}.";
    }

    private static string BuildBody(Reservation reservation)
    {
        var customer = Html(reservation.Customer?.FullName ?? "Customer");
        var table = Html(reservation.DiningTable?.TableNumber ?? "To be assigned");
        var bookingCode = Html(reservation.BookingCode);
        var start = Html(reservation.StartAt.ToString("dd MMM yyyy, HH:mm"));
        var end = Html(reservation.EndAt.ToString("HH:mm"));

        return $$"""
            <!doctype html>
            <html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width"></head>
            <body style="margin:0;background:#f4f1e9;font-family:Segoe UI,Arial,sans-serif;color:#1f2933">
              <div style="max-width:620px;margin:0 auto;padding:28px 16px">
                <div style="background:#174c3c;color:white;padding:28px 30px;border-radius:14px 14px 0 0">
                  <div style="color:#f2c77d;text-transform:uppercase;letter-spacing:1.4px;font-size:12px">Booking confirmed</div>
                  <h1 style="margin:8px 0 0">QIGLO Restaurant</h1>
                </div>
                <div style="background:white;padding:30px;border-radius:0 0 14px 14px;box-shadow:0 12px 35px rgba(31,41,51,.10)">
                  <p style="margin-top:0">Hi {{customer}}, your table reservation is confirmed.</p>
                  <table role="presentation" style="width:100%;border-collapse:collapse;margin:22px 0;background:#f8faf9;border-radius:8px">
                    <tr><td style="padding:10px 12px;color:#68737d">Booking code</td><td style="padding:10px 12px;text-align:right;font-weight:700">{{bookingCode}}</td></tr>
                    <tr><td style="padding:10px 12px;color:#68737d">Date and time</td><td style="padding:10px 12px;text-align:right">{{start}} - {{end}}</td></tr>
                    <tr><td style="padding:10px 12px;color:#68737d">Table</td><td style="padding:10px 12px;text-align:right">{{table}}</td></tr>
                    <tr><td style="padding:10px 12px;color:#68737d">Guests</td><td style="padding:10px 12px;text-align:right">{{reservation.Pax}}</td></tr>
                  </table>
                  <div style="padding:14px;background:#fff7e6;border-left:4px solid #d59b3d;border-radius:4px">
                    Show the QR code or booking code to our staff when you arrive. Check-in opens 30 minutes before the reservation time.
                  </div>
                  <p style="margin:26px 0 0;color:#68737d;font-size:12px">This is an automated booking confirmation from QIGLO Restaurant.</p>
                </div>
              </div>
            </body></html>
            """;
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);
}
