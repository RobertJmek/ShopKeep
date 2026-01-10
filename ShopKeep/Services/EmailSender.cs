using Microsoft.AspNetCore.Identity.UI.Services;

namespace ShopKeep.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // For development purposes, just log the email
            // In production, implement actual email sending using SMTP, SendGrid, etc.
            Console.WriteLine($"Email to: {email}");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine($"Message: {htmlMessage}");
            return Task.CompletedTask;
        }
    }
}
