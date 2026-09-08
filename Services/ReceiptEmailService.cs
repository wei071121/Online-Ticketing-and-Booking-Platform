using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;
using QigloRestaurant.Models.Entities;

namespace QigloRestaurant.Web.Services;

public class EmailSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "noreply@qiglo.test";
    public string FromName { get; set; } = "QIGLO Restaurant";
}

public interface IReceiptEmailService
{
    Task<string> SendAsync(Payment payment, byte[] pdfBytes);
}

public class ReceiptEmailService(
    IOptions<EmailSettings> options,
    IWebHostEnvironment environment) : IReceiptEmailService
{
    private readonly EmailSettings settings = options.Value;

    public async Task<string> SendAsync(Payment payment, byte[] pdfBytes)
    {
        var recipient = payment.DiningSession.Customer?.Email;
        if (string.IsNullOrWhiteSpace(recipient))
        {
            return "No customer email is linked to this dining session.";
        }

        var body = BuildBody(payment);

        if (string.IsNullOrWhiteSpace(settings.Host))
        {
            var outbox = Path.Combine(environment.ContentRootPath, "App_Data", "EmailOutbox");
            Directory.CreateDirectory(outbox);
            var safeReceipt = payment.ReceiptNumber.Replace("/", "-", StringComparison.Ordinal);
            await File.WriteAllTextAsync(Path.Combine(outbox, $"{safeReceipt}.html"), body);
            await File.WriteAllBytesAsync(Path.Combine(outbox, $"{safeReceipt}.pdf"), pdfBytes);
            return "SMTP is not configured; a demonstration email and PDF were saved in App_Data/EmailOutbox.";
        }

        using var message = new MailMessage
        {
            From = new MailAddress(settings.FromEmail, settings.FromName),
            Subject = $"QIGLO receipt {payment.ReceiptNumber}",
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(recipient);

        var attachmentStream = new MemoryStream(pdfBytes);
        message.Attachments.Add(
            new Attachment(attachmentStream, $"{payment.ReceiptNumber}.pdf", "application/pdf"));

        using var smtp = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.EnableSsl,
            Credentials = new NetworkCredential(settings.Username, settings.Password)
        };
        await smtp.SendMailAsync(message);
        return $"E-receipt sent to {recipient}.";
    }

    private static string BuildBody(Payment payment)
    {
        var rows = new StringBuilder();
        foreach (var order in payment.DiningSession.Orders
                     .Where(item => item.Status != "Cancelled")
                     .OrderBy(item => item.OrderedAt))
        {
            foreach (var item in order.OrderItems)
            {
                var addOnTotal = item.OrderItemAddOns.Sum(addOn => addOn.UnitPrice);
                var unitTotal = item.UnitPrice + addOnTotal;
                var addOns = item.OrderItemAddOns.Count == 0
                    ? string.Empty
                    : $"<div style=\"color:#68737d;font-size:12px;margin-top:3px\">" +
                      $"Add-ons: {Html(string.Join(", ", item.OrderItemAddOns.Select(addOn => addOn.AddOnName)))}</div>";

                rows.Append($$"""
                    <tr>
                      <td style="padding:12px 0;border-bottom:1px solid #e7eaec">
                        <strong>{{Html(item.MenuItemName)}}</strong>
                        <div style="color:#68737d;font-size:12px;margin-top:3px">{{Html(order.OrderNumber)}}</div>
                        {{addOns}}
                      </td>
                      <td style="padding:12px 8px;border-bottom:1px solid #e7eaec;text-align:center">{{item.Quantity}}</td>
                      <td style="padding:12px 0;border-bottom:1px solid #e7eaec;text-align:right;white-space:nowrap">RM {{item.Quantity * unitTotal:0.00}}</td>
                    </tr>
                    """);
            }
        }

        var customer = payment.DiningSession.Customer?.FullName ?? "Walk-in customer";
        var paidAt = payment.PaidAt?.ToString("dd MMM yyyy, HH:mm") ?? "Pending";

        return $$"""
            <!doctype html>
            <html lang="en">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width"></head>
            <body style="margin:0;background:#f4f1e9;font-family:Segoe UI,Arial,sans-serif;color:#1f2933">
              <div style="max-width:640px;margin:0 auto;padding:28px 16px">
                <div style="background:#174c3c;color:#fff;padding:28px 30px;border-radius:14px 14px 0 0">
                  <div style="font-size:12px;letter-spacing:1.5px;text-transform:uppercase;color:#f2c77d">Payment confirmed</div>
                  <h1 style="margin:8px 0 4px;font-size:28px">QIGLO Restaurant</h1>
                  <div style="color:#dbe7e2">Table reservation and in-house dining</div>
                </div>
                <div style="background:#fff;padding:28px 30px;border-radius:0 0 14px 14px;box-shadow:0 12px 35px rgba(31,41,51,.10)">
                  <p style="margin-top:0">Hi {{Html(customer)}}, thank you for dining with us. Your payment was completed successfully.</p>

                  <table role="presentation" style="width:100%;border-collapse:collapse;margin:22px 0;background:#f8faf9;border-radius:8px">
                    <tr><td style="padding:10px 12px;color:#68737d">Receipt</td><td style="padding:10px 12px;text-align:right;font-weight:600">{{Html(payment.ReceiptNumber)}}</td></tr>
                    <tr><td style="padding:10px 12px;color:#68737d">Table</td><td style="padding:10px 12px;text-align:right">{{Html(payment.DiningSession.DiningTable.TableNumber)}}</td></tr>
                    <tr><td style="padding:10px 12px;color:#68737d">Paid</td><td style="padding:10px 12px;text-align:right">{{Html(paidAt)}}</td></tr>
                    <tr><td style="padding:10px 12px;color:#68737d">Method</td><td style="padding:10px 12px;text-align:right">{{Html(payment.PaymentMethod)}}</td></tr>
                  </table>

                  <h2 style="font-size:18px;margin:26px 0 8px">Order summary</h2>
                  <table role="presentation" style="width:100%;border-collapse:collapse">
                    <thead><tr style="color:#68737d;font-size:12px;text-transform:uppercase"><th style="padding:8px 0;text-align:left">Item</th><th style="padding:8px;text-align:center">Qty</th><th style="padding:8px 0;text-align:right">Amount</th></tr></thead>
                    <tbody>{{rows}}</tbody>
                  </table>

                  <table role="presentation" style="width:100%;border-collapse:collapse;margin-top:20px">
                    <tr><td style="padding:5px 0;color:#68737d">Subtotal</td><td style="padding:5px 0;text-align:right">RM {{payment.Subtotal:0.00}}</td></tr>
                    <tr><td style="padding:5px 0;color:#68737d">Tax (6%)</td><td style="padding:5px 0;text-align:right">RM {{payment.TaxAmount:0.00}}</td></tr>
                    <tr><td style="padding:5px 0;color:#68737d">Service charge (10%)</td><td style="padding:5px 0;text-align:right">RM {{payment.ServiceCharge:0.00}}</td></tr>
                    <tr style="font-size:20px;font-weight:700;color:#174c3c"><td style="padding:14px 0;border-top:2px solid #174c3c">Total</td><td style="padding:14px 0;border-top:2px solid #174c3c;text-align:right">RM {{payment.TotalAmount:0.00}}</td></tr>
                  </table>

                  <div style="margin-top:20px;padding:14px;background:#fff7e6;border-left:4px solid #d59b3d;border-radius:4px">
                    Your detailed PDF receipt is attached to this email. Please keep it for your records.
                  </div>
                  <p style="margin:28px 0 0;color:#68737d;font-size:12px">This is an automated e-receipt from QIGLO Restaurant.</p>
                </div>
              </div>
            </body>
            </html>
            """;
    }

    private static string Html(string? value) =>
        WebUtility.HtmlEncode(value ?? string.Empty);
}
