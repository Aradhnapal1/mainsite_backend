using firstproject.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Text;

namespace firstproject.Models.BusinessLayer
{
    public partial interface IBusinessLayer
    {
        Task<MicrositeResolvedData?> GetMicrositePublicData(string domain);
        Task<MicrositeModel?> GetMicrositePublicByUniqueId(string micrositeId);
        Task<List<MicrositeAssignedProduct>> GetMicrositeProductsByUniqueId(string micrositeId);
        Task<List<MicrositeAssignedProduct>> GetMicrositeProducts(string domain);
        Task<MicrositeAssignedProduct?> GetMicrositeProductByUniqueId(string micrositeId, int productId);
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

        public async Task<List<MicrositeAssignedProduct>> GetMicrositeProductsByUniqueId(string micrositeId)
        {
            await _databaseLayer.EnsureMicrositePublicSchema();
            var microsite = await _databaseLayer.GetMicrositeByUniqueId(micrositeId);
            if (microsite == null)
                return new List<MicrositeAssignedProduct>();
            return await _databaseLayer.GetMicrositeProducts(microsite.Id);
        }

        public async Task<List<MicrositeAssignedProduct>> GetMicrositeProducts(string domain)
        {
            await _databaseLayer.EnsureMicrositePublicSchema();
            var microsite = await _databaseLayer.ResolveMicrositeByDomain(domain);
            if (microsite == null)
                return new List<MicrositeAssignedProduct>();
            return await _databaseLayer.GetMicrositeProducts(microsite.MicrositeId);
        }

        public async Task<MicrositeAssignedProduct?> GetMicrositeProductByUniqueId(string micrositeId, int productId)
        {
            await _databaseLayer.EnsureMicrositePublicSchema();
            var microsite = await _databaseLayer.ResolveMicrositeByUniqueId(micrositeId);
            if (microsite == null || productId <= 0)
                return null;
            return await _databaseLayer.GetMicrositeProduct(microsite.MicrositeId, productId);
        }

        private async Task<MicrositeResolvedData?> ResolveMicrositeForPublic(string? micrositeId, string? domain)
        {
            if (!string.IsNullOrWhiteSpace(micrositeId))
                return await _databaseLayer.ResolveMicrositeByUniqueId(micrositeId);
            if (!string.IsNullOrWhiteSpace(domain))
                return await _databaseLayer.ResolveMicrositeByDomain(domain);
            return null;
        }

        private static bool IsEmailEligibleForDomains(string email, List<string> domains)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                return false;
            if (domains == null || domains.Count == 0)
                return false;

            var emailDomain = email.Split('@').Last().Trim().ToLowerInvariant();
            return domains.Any(d =>
            {
                var allowed = (d ?? "").Trim().ToLowerInvariant();
                if (allowed.StartsWith("@"))
                    allowed = allowed[1..];
                if (allowed.Contains("://"))
                {
                    try
                    {
                        var uri = new Uri(allowed.Contains("://") ? allowed : "https://" + allowed);
                        allowed = uri.Host.ToLowerInvariant();
                    }
                    catch { /* keep raw */ }
                }
                return emailDomain == allowed || emailDomain.EndsWith("." + allowed, StringComparison.Ordinal);
            });
        }

        private async Task<IActionResult?> ValidateMicrositeEmailDomain(long micrositeId, string email)
        {
            var domains = await _databaseLayer.GetMicrositeDomains(micrositeId);
            if (domains == null || domains.Count == 0)
            {
                return new BadRequestObjectResult(new
                {
                    status = false,
                    message = "This microsite has no allowed email domains configured."
                });
            }
            if (!IsEmailEligibleForDomains(email, domains))
            {
                var hint = string.Join(", ", domains.Select(d => "@" + (d ?? "").Trim().TrimStart('@')));
                return new BadRequestObjectResult(new
                {
                    status = false,
                    message = $"Order can only be placed using allowed email domains: {hint}."
                });
            }
            return null;
        }

        public async Task<IActionResult> SendMicrositeOtp(MicrositeOtpSendRequest request)
        {
            await _databaseLayer.EnsureMicrositePublicSchema();

            if (string.IsNullOrWhiteSpace(request.Email))
                return new BadRequestObjectResult(new { status = false, message = "Email required hai." });
            if (string.IsNullOrWhiteSpace(request.MicrositeId) && string.IsNullOrWhiteSpace(request.Domain))
                return new BadRequestObjectResult(new { status = false, message = "microsite_id ya domain required hai." });

            var microsite = await ResolveMicrositeForPublic(request.MicrositeId, request.Domain);
            if (microsite == null)
                return new NotFoundObjectResult(new { status = false, message = "Microsite invalid hai." });

            var domainCheck = await ValidateMicrositeEmailDomain(microsite.MicrositeId, request.Email);
            if (domainCheck != null)
                return domainCheck;

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
            var sent = await SendEmailIfConfigured(request.Email, subject, html);
            if (!sent)
            {
                return new ObjectResult(new
                {
                    status = false,
                    message = "OTP email could not be sent. Please check SMTP settings or try again."
                })
                { StatusCode = 500 };
            }

            return new OkObjectResult(new
            {
                status = true,
                message = "OTP sent to your email.",
                expiryMinutes = 10
            });
        }

        public async Task<IActionResult> VerifyMicrositeOtp(MicrositeOtpVerifyRequest request)
        {
            await _databaseLayer.EnsureMicrositePublicSchema();

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Otp))
                return new BadRequestObjectResult(new { status = false, message = "Email aur OTP required hain." });
            if (string.IsNullOrWhiteSpace(request.MicrositeId) && string.IsNullOrWhiteSpace(request.Domain))
                return new BadRequestObjectResult(new { status = false, message = "microsite_id ya domain required hai." });

            var microsite = await ResolveMicrositeForPublic(request.MicrositeId, request.Domain);
            if (microsite == null)
                return new NotFoundObjectResult(new { status = false, message = "Microsite invalid hai." });

            var domainCheck = await ValidateMicrositeEmailDomain(microsite.MicrositeId, request.Email);
            if (domainCheck != null)
                return domainCheck;

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
            if (string.IsNullOrWhiteSpace(request.MicrositeId) && string.IsNullOrWhiteSpace(request.Domain))
                return new BadRequestObjectResult(new { status = false, message = "microsite_id ya domain required hai." });
            if (request.ProductId <= 0)
                return new BadRequestObjectResult(new { status = false, message = "Product required hai." });
            if (request.Quantity != 1)
                return new BadRequestObjectResult(new { status = false, message = "One order place only. Only 1 quantity is allowed." });

            var helper = new JwtHelper(_configuration);
            var userId = helper.GetUserIdFromToken(token);
            if (userId == null)
                return new UnauthorizedObjectResult(new { status = false, message = "Invalid token." });

            var microsite = await ResolveMicrositeForPublic(request.MicrositeId, request.Domain);
            if (microsite == null)
                return new NotFoundObjectResult(new { status = false, message = "Microsite domain invalid hai." });

            var dbUser = await _databaseLayer.GetMicrositeUserById(userId.Value);
            if (dbUser == null)
                return new UnauthorizedObjectResult(new { status = false, message = "User not found." });
            if (dbUser.MicrositeId != microsite.MicrositeId)
                return new BadRequestObjectResult(new { status = false, message = "User does not belong to this microsite." });

            request.Email = dbUser.Email.Trim();
            var domainCheck = await ValidateMicrositeEmailDomain(microsite.MicrositeId, request.Email);
            if (domainCheck != null)
                return domainCheck;

            if (string.IsNullOrWhiteSpace(request.FirstName))
            {
                if (!string.IsNullOrWhiteSpace(dbUser.Name))
                    request.FirstName = dbUser.Name.Trim();
                else if (!string.IsNullOrWhiteSpace(dbUser.Email) && dbUser.Email.Contains('@'))
                    request.FirstName = dbUser.Email.Split('@')[0];
            }

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
            var buttonColor = ((dynamic)microsite.Theme)?.buttonColor?.ToString() ?? "#2563eb";
            var bodyTextColor = "#1f2937";
            var mutedColor = "#6b7280";
            var customerName = $"{request.FirstName} {request.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(customerName))
                customerName = "Customer";

            var sb = new StringBuilder();
            sb.Append($"<div style='font-family:Arial,sans-serif;max-width:620px;margin:auto;border:1px solid #e5e7eb;background:#ffffff'>");
            sb.Append($"<div style='background:{headerColor};padding:16px;color:#ffffff'><h2 style='margin:0;color:#ffffff'>{microsite.Name} - Order Confirmation</h2></div>");
            sb.Append($"<div style='padding:16px;color:{bodyTextColor};background:#ffffff'>");
            sb.Append($"<p style='color:{bodyTextColor};margin:0 0 12px'>Hi {customerName},</p>");
            sb.Append($"<p style='color:{bodyTextColor};margin:0 0 12px'>Your order has been placed successfully.</p>");
            sb.Append($"<p style='color:{bodyTextColor};margin:0 0 12px'><strong style='color:{bodyTextColor}'>Product Id:</strong> {request.ProductId}<br/>");
            sb.Append($"<strong style='color:{bodyTextColor}'>Quantity:</strong> {request.Quantity}<br/>");
            sb.Append($"<strong style='color:{bodyTextColor}'>Email:</strong> {request.Email}<br/>");
            sb.Append($"<strong style='color:{bodyTextColor}'>Delivery Address:</strong> {request.Address}, {request.City}, {request.State}, {request.Pincode}, {request.Country}</p>");
            sb.Append($"<p style='margin:16px 0'><span style='display:inline-block;background:{buttonColor};color:#ffffff;padding:8px 14px;border-radius:4px'>Thank you for shopping</span></p>");
            sb.Append($"<p style='font-size:12px;color:{mutedColor};margin:0'>This is an automated email from microsite order system.</p>");
            sb.Append("</div></div>");
            return sb.ToString();
        }

        private async Task<bool> SendEmailIfConfigured(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(_configuration["Smtp:Host"]))
                return false;

            try
            {
                await SmtpEmailHelper.SendAsync(_configuration, toEmail, subject, htmlBody);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
