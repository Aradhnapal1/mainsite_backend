using System.Net;
using System.Net.Mail;

namespace firstproject.Helpers
{
    public static class SmtpEmailHelper
    {
        public static async Task SendAsync(
            IConfiguration configuration,
            string toEmail,
            string subject,
            string htmlBody,
            string? plainTextBody = null)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("Recipient email is required.", nameof(toEmail));

            var host = configuration["Smtp:Host"];
            if (string.IsNullOrWhiteSpace(host))
                throw new InvalidOperationException("SMTP is not configured. Set Smtp:Host in appsettings.");

            var port = int.TryParse(configuration["Smtp:Port"], out var parsedPort) ? parsedPort : 587;
            var username = configuration["Smtp:Username"];
            var password = NormalizeAppPassword(configuration["Smtp:Password"]);
            var fromEmail = configuration["Smtp:FromEmail"] ?? username ?? "no-reply@example.com";
            var fromName = configuration["Smtp:FromName"] ?? "HyperCompany";
            var enableSsl = !string.Equals(configuration["Smtp:EnableSsl"], "false", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("SMTP username/password missing in appsettings.");

            using var mail = new MailMessage
            {
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            if (!string.IsNullOrWhiteSpace(plainTextBody))
                mail.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(plainTextBody, null, "text/plain"));

            mail.To.Add(toEmail.Trim());
            mail.From = new MailAddress(fromEmail, fromName);

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                UseDefaultCredentials = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential(username, password)
            };

            await client.SendMailAsync(mail);
        }

        public static string NormalizeAppPassword(string? password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return string.Empty;
            return password.Replace(" ", "", StringComparison.Ordinal);
        }
    }
}
