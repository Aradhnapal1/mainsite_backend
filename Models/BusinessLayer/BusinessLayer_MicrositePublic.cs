using firstproject.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace firstproject.Models.BusinessLayer
{
    public partial interface IBusinessLayer
    {
        Task<MicrositeResolvedData?> GetMicrositePublicData(string domain);
        Task<MicrositeModel?> GetMicrositePublicByUniqueId(string micrositeId);
        Task<List<MicrositeAssignedProduct>> GetMicrositeProducts(string domain);
        Task<IActionResult> SendMicrositeOtp(MicrositeOtpSendRequest request);
        Task<IActionResult> VerifyMicrositeOtp(MicrositeOtpVerifyRequest request);
        Task<IActionResult> PlaceMicrositeOrder(string token, MicrositeSingleOrderRequest request);
        Task<IActionResult> GetMicrositeOrders(string token, string domain);
        Task<IActionResult> GetMicrositeOrderDetail(string token, string domain, int orderId);
        Task<IActionResult> GetAdminMicrositeOrders(long micrositeId);
        Task<IActionResult> GetAdminMicrositeOrderDetail(long micrositeId, int orderId);
    }

    public partial class BusinessLayer : IBusinessLayer
    {
        private readonly Random _random = new();

        public async Task<MicrositeResolvedData?> GetMicrositePublicData(string domain)
        {
            await _databaseLayer.EnsureMicrositePublicSchema();
            return await _databaseLayer.ResolveMicrositeByDomain(domain);
        }

        public async Task<MicrositeModel?> GetMicrositePublicByUniqueId(string micrositeId)
        {
            await _databaseLayer.EnsureMicrositePublicSchema();
            return await _databaseLayer.GetMicrositeByUniqueId(micrositeId);
        }

        public async Task<List<MicrositeAssignedProduct>> GetMicrositeProducts(string domain)
        {
            await _databaseLayer.EnsureMicrositePublicSchema();
            var microsite = await _databaseLayer.ResolveMicrositeByDomain(domain);
            if (microsite == null)
                return new List<MicrositeAssignedProduct>();
            return await _databaseLayer.GetMicrositeProducts(microsite.MicrositeId);
        }

        public async Task<IActionResult> SendMicrositeOtp(MicrositeOtpSendRequest request)
        {
            await _databaseLayer.EnsureMicrositePublicSchema();

            if (string.IsNullOrWhiteSpace(request.Domain) || string.IsNullOrWhiteSpace(request.Email))
                return new BadRequestObjectResult(new { status = false, message = "Domain aur email required hain." });

            var microsite = await _databaseLayer.ResolveMicrositeByDomain(request.Domain);
            if (microsite == null)
                return new NotFoundObjectResult(new { status = false, message = "Microsite domain invalid hai." });

            var otp = _random.Next(100000, 999999).ToString();
            var expiry = DateTime.UtcNow.AddMinutes(10);
            await _databaseLayer.CreateMicrositeOtp(microsite.MicrositeId, request.Email, otp, expiry);

            var subject = $"{microsite.Name} login OTP";
            var html = $@"<div style='font-family:Arial,sans-serif'>
<h3>Microsite OTP Verification</h3>
<p>Your OTP is <strong>{otp}</strong></p>
<p>Valid for 10 minutes.</p>
<p>Domain: {microsite.Domain}</p>
</div>";
            await SendEmailIfConfigured(request.Email, subject, html);

            return new OkObjectResult(new
            {
                status = true,
                message = "OTP email par bhej diya gaya.",
                expiryMinutes = 10
            });
        }

        public async Task<IActionResult> VerifyMicrositeOtp(MicrositeOtpVerifyRequest request)
        {
            await _databaseLayer.EnsureMicrositePublicSchema();

            if (string.IsNullOrWhiteSpace(request.Domain) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Otp))
                return new BadRequestObjectResult(new { status = false, message = "Domain, email aur OTP required hain." });

            var microsite = await _databaseLayer.ResolveMicrositeByDomain(request.Domain);
            if (microsite == null)
                return new NotFoundObjectResult(new { status = false, message = "Microsite domain invalid hai." });

            var isValidOtp = await _databaseLayer.VerifyMicrositeOtp(microsite.MicrositeId, request.Email, request.Otp);
            if (!isValidOtp)
                return new UnauthorizedObjectResult(new { status = false, message = "OTP invalid ya expire ho chuka hai." });

            var user = await _databaseLayer.UpsertMicrositeUser(microsite.MicrositeId, request.Email, request.Name);
            var tokenUser = new Usermodel
            {
                Id = user.Id,
                Email = user.Email,
                Firstname = string.IsNullOrWhiteSpace(user.Name) ? "Microsite" : user.Name,
                Role = "MicrositeUser"
            };
            var helper = new JwtHelper(_configuration);
            var token = helper.GenerateToken(tokenUser);

            return new OkObjectResult(new
            {
                status = true,
                message = "Login/Register successful",
                data = new
                {
                    token,
                    user = new
                    {
                        id = user.Id,
                        name = user.Name,
                        email = user.Email,
                        micrositeId = user.MicrositeId
                    }
                }
            });
        }

        public async Task<IActionResult> PlaceMicrositeOrder(string token, MicrositeSingleOrderRequest request)
        {
            await _databaseLayer.EnsureMicrositePublicSchema();
            if (string.IsNullOrWhiteSpace(request.Domain))
                return new BadRequestObjectResult(new { status = false, message = "Domain required hai." });
            if (request.ProductId <= 0)
                return new BadRequestObjectResult(new { status = false, message = "Product required hai." });
            if (request.Quantity != 1)
                return new BadRequestObjectResult(new { status = false, message = "Microsite me sirf 1 product ka order allow hai." });

            var helper = new JwtHelper(_configuration);
            var userId = helper.GetUserIdFromToken(token);
            if (userId == null)
                return new UnauthorizedObjectResult(new { status = false, message = "Invalid token." });

            var microsite = await _databaseLayer.ResolveMicrositeByDomain(request.Domain);
            if (microsite == null)
                return new NotFoundObjectResult(new { status = false, message = "Microsite domain invalid hai." });

            var result = await _databaseLayer.PlaceMicrositeSingleOrder(userId.Value, microsite.MicrositeId, request);
            if (result is OkObjectResult ok && ok.Value != null)
            {
                var body = BuildOrderEmailHtml(microsite, request, ok.Value);
                await SendEmailIfConfigured(request.Email, $"Order Confirmed - {microsite.Name}", body);
            }
            return result;
        }

        public async Task<IActionResult> GetMicrositeOrders(string token, string domain)
        {
            await _databaseLayer.EnsureMicrositePublicSchema();
            var helper = new JwtHelper(_configuration);
            var userId = helper.GetUserIdFromToken(token);
            if (userId == null)
                return new UnauthorizedObjectResult(new { status = false, message = "Invalid token." });

            var microsite = await _databaseLayer.ResolveMicrositeByDomain(domain);
            if (microsite == null)
                return new NotFoundObjectResult(new { status = false, message = "Microsite domain invalid hai." });

            return await _databaseLayer.GetMicrositeOrders(userId.Value, microsite.MicrositeId);
        }

        public async Task<IActionResult> GetMicrositeOrderDetail(string token, string domain, int orderId)
        {
            await _databaseLayer.EnsureMicrositePublicSchema();
            var helper = new JwtHelper(_configuration);
            var userId = helper.GetUserIdFromToken(token);
            if (userId == null)
                return new UnauthorizedObjectResult(new { status = false, message = "Invalid token." });

            var microsite = await _databaseLayer.ResolveMicrositeByDomain(domain);
            if (microsite == null)
                return new NotFoundObjectResult(new { status = false, message = "Microsite domain invalid hai." });

            return await _databaseLayer.GetMicrositeOrderDetail(userId.Value, microsite.MicrositeId, orderId);
        }

        public async Task<IActionResult> GetAdminMicrositeOrders(long micrositeId)
        {
            await _databaseLayer.EnsureMicrositePublicSchema();
            return await _databaseLayer.GetAdminMicrositeOrders(micrositeId);
        }

        public async Task<IActionResult> GetAdminMicrositeOrderDetail(long micrositeId, int orderId)
        {
            await _databaseLayer.EnsureMicrositePublicSchema();
            return await _databaseLayer.GetAdminMicrositeOrderDetail(micrositeId, orderId);
        }

        private string BuildOrderEmailHtml(MicrositeResolvedData microsite, MicrositeSingleOrderRequest request, object responseData)
        {
            var headerColor = ((dynamic)microsite.Theme)?.headerColor?.ToString() ?? "#111827";
            var textColor = ((dynamic)microsite.Theme)?.textColor?.ToString() ?? "#111111";
            var buttonColor = ((dynamic)microsite.Theme)?.buttonColor?.ToString() ?? "#2563eb";

            var sb = new StringBuilder();
            sb.Append($"<div style='font-family:Arial,sans-serif;color:{textColor};max-width:620px;margin:auto;border:1px solid #e5e7eb'>");
            sb.Append($"<div style='background:{headerColor};padding:16px;color:#fff'><h2 style='margin:0'>{microsite.Name} - Order Confirmation</h2></div>");
            sb.Append("<div style='padding:16px'>");
            sb.Append($"<p>Hi {request.FirstName} {request.LastName},</p>");
            sb.Append("<p>Your order has been placed successfully.</p>");
            sb.Append($"<p><strong>Product Id:</strong> {request.ProductId}<br/>");
            sb.Append($"<strong>Quantity:</strong> {request.Quantity}<br/>");
            sb.Append($"<strong>Delivery Address:</strong> {request.Address}, {request.City}, {request.State}, {request.Pincode}, {request.Country}</p>");
            sb.Append($"<p><span style='display:inline-block;background:{buttonColor};color:#fff;padding:8px 14px;border-radius:4px'>Thank you for shopping</span></p>");
            sb.Append("<p style='font-size:12px;color:#6b7280'>This is an automated email from microsite order system.</p>");
            sb.Append("</div></div>");
            return sb.ToString();
        }

        private async Task SendEmailIfConfigured(string toEmail, string subject, string htmlBody)
        {
            var host = _configuration["Smtp:Host"];
            if (string.IsNullOrWhiteSpace(host))
                return;

            var port = int.TryParse(_configuration["Smtp:Port"], out var parsedPort) ? parsedPort : 587;
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];
            var fromEmail = _configuration["Smtp:FromEmail"] ?? username ?? "no-reply@example.com";
            var fromName = _configuration["Smtp:FromName"] ?? "Microsite";
            var enableSsl = !string.Equals(_configuration["Smtp:EnableSsl"], "false", StringComparison.OrdinalIgnoreCase);

            using var mail = new MailMessage
            {
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            mail.To.Add(toEmail);
            mail.From = new MailAddress(fromEmail, fromName);

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl
            };

            if (!string.IsNullOrWhiteSpace(username))
                client.Credentials = new NetworkCredential(username, password);

            await client.SendMailAsync(mail);
        }
    }
}
