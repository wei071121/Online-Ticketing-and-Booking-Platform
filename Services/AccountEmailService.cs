using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;
using QigloRestaurant.Models.Entities;

namespace QigloRestaurant.Web.Services;

public interface IAccountEmailService
{
    Task<string> SendPasswordResetAsync(User user, string resetUrl);
}

public class AccountEmailService(
    IOptions<EmailSettings> options,
    IWebHostEnvironment environment) : IAccountEmailService
{
    private readonly EmailSettings settings = options.Value;

    public async Task<string> SendPasswordResetAsync(User user, string resetUrl)
    {
        var body = $$"""
            <!doctype html>
            <html lang="en"><body style="margin:0;background:#f4f1e9;font-family:Segoe UI,Arial,sans-serif;color:#1f2933">
              <div style="max-width:620px;margin:0 auto;padding:28px 16px">
                <div style="background:#174c3c;color:white;padding:28px 30px;border-radius:14px 14px 0 0">
                  <div style="color:#f2c77d;text-transform:uppercase;letter-spacing:1.4px;font-size:12px">Account security</div>
                  <h1 style="margin:8px 0 0">QIGLO Restaurant</h1>
                </div>
                <div style="background:white;padding:30px;border-radius:0 0 14px 14px">
                  <p>Hi {{Html(user.FullName)}},</p>
                  <p>A password reset was requested for your QIGLO account. This one-time link expires in 30 minutes.</p>
                  <p><a href="{{Html(resetUrl)}}" style="display:inline-block;background:#174c3c;color:white;padding:12px 18px;border-radius:6px;text-decoration:none">Reset password</a></p>
                  <p style="color:#68737d;font-size:13px">If you did not request this, ignore this email. Your password remains unchanged.</p>
                </div>
              </div>
            </body></html>
            """;

        if (string.IsNullOrWhiteSpace(settings.Host))
        {
            var outbox = Path.Combine(environment.ContentRootPath, "App_Data", "EmailOutbox");
            Directory.CreateDirectory(outbox);
            var fileName = $"password-reset-{user.UserId}-{DateTime.Now:yyyyMMddHHmmss}.html";
            await File.WriteAllTextAsync(Path.Combine(outbox, fileName), body, Encoding.UTF8);
            return "SMTP is not configured; the demonstration email was saved in App_Data/EmailOutbox.";
        }

        using var message = new MailMessage
        {
            From = new MailAddress(settings.FromEmail, settings.FromName),
            Subject = "Reset your QIGLO password",
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(user.Email);
        using var smtp = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.EnableSsl,
            Credentials = new NetworkCredential(settings.Username, settings.Password)
        };
        await smtp.SendMailAsync(message);
        return $"Password reset email sent to {user.Email}.";
    }

    private static string Html(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
