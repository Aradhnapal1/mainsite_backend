using firstproject.Models;
using firstproject.Models.BusinessLayer;
using Microsoft.AspNetCore.Mvc;

namespace firstproject.Controllers
{
    [ApiController]
    [Route("api/microsite-public")]
[ApiExplorerSettings(GroupName = "microsite-v1")]
    public class MicroSitePublicController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;

        public MicroSitePublicController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
        }

        [HttpGet("by-id")]
        public async Task<IActionResult> GetMicrositeByPublicId([FromQuery] string microsite_id)
        {
            if (string.IsNullOrWhiteSpace(microsite_id))
                return BadRequest(new { status = false, message = "microsite_id required hai." });

            var microsite = await _businessLayer.GetMicrositePublicByUniqueId(microsite_id);
            if (microsite == null)
                return NotFound(new { status = false, message = "Microsite nahi mila." });

            return Ok(new { status = true, data = microsite });
        }

        [HttpGet("home")]
        public async Task<IActionResult> GetMicrositeHome([FromQuery] string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return BadRequest(new { status = false, message = "Domain required hai." });

            var microsite = await _businessLayer.GetMicrositePublicData(domain);
            if (microsite == null)
                return NotFound(new { status = false, message = "Microsite nahi mila." });

            var products = await _businessLayer.GetMicrositeProducts(domain);
            return Ok(new
            {
                status = true,
                data = new
                {
                    microsite,
                    products
                }
            });
        }

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts([FromQuery] string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return BadRequest(new { status = false, message = "Domain required hai." });

            var products = await _businessLayer.GetMicrositeProducts(domain);
            return Ok(new { status = true, data = products });
        }

        [HttpPost("auth/send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] MicrositeOtpSendRequest request)
        {
            return await _businessLayer.SendMicrositeOtp(request);
        }

        [HttpPost("auth/verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] MicrositeOtpVerifyRequest request)
        {
            return await _businessLayer.VerifyMicrositeOtp(request);
        }

        [HttpPost("order")]
        public async Task<IActionResult> PlaceOrder([FromBody] MicrositeSingleOrderRequest request)
        {
            var token = ExtractBearerToken();
            if (string.IsNullOrEmpty(token))
                return Unauthorized(new { status = false, message = "Bearer token required hai." });

            return await _businessLayer.PlaceMicrositeOrder(token, request);
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetMyOrders([FromQuery] string domain)
        {
            var token = ExtractBearerToken();
            if (string.IsNullOrEmpty(token))
                return Unauthorized(new { status = false, message = "Bearer token required hai." });

            return await _businessLayer.GetMicrositeOrders(token, domain);
        }

        [HttpGet("orders/{orderId}")]
        public async Task<IActionResult> GetOrderDetail(int orderId, [FromQuery] string domain)
        {
            var token = ExtractBearerToken();
            if (string.IsNullOrEmpty(token))
                return Unauthorized(new { status = false, message = "Bearer token required hai." });

            return await _businessLayer.GetMicrositeOrderDetail(token, domain, orderId);
        }

        private string? ExtractBearerToken()
        {
            var auth = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(auth) || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return null;
            return auth["Bearer ".Length..].Trim();
        }
    }
}
