using firstproject.Helpers;
using firstproject.Models;
using firstproject.Models.BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace firstproject.Controllers
{
    [ApiController]
    [Route("api/checkout")]
    public class CheckoutController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        private readonly JwtHelper _jwtHelper;

        public CheckoutController(IBusinessLayer businessLayer, JwtHelper jwtHelper)
        {
            _businessLayer = businessLayer;
            _jwtHelper = jwtHelper;
        }

        private int? GetUserIdFromToken()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return null;

            return _jwtHelper.GetUserIdFromToken(
                authHeader.Substring("Bearer ".Length).Trim()
            );
        }

        private string GetGuestId()
        {
            if (Request.Cookies.TryGetValue("guest_id", out var existingId)
                && !string.IsNullOrEmpty(existingId))
                return existingId;

            return "";
        }

        // ✅ GET CHECKOUT PREVIEW (cart + address form)
        [HttpGet("preview")]
        public async Task<IActionResult> GetCheckoutPreview()
        {
            int? userId = GetUserIdFromToken();

            if (userId == null)
                return Unauthorized(new
                {
                    status = false,
                    message = "Aap login nahi hain. Checkout ke liye pehle login karein."
                });

            string guestId = GetGuestId();
            var items = await _businessLayer.GetCart(userId, guestId);

            if (items.Count == 0)
                return Ok(new
                {
                    status = false,
                    message = "Aapka cart khali hai. Pehle kuch products add karein."
                });

            decimal grandTotal = items.Sum(x => x.totalprice);

            return Ok(new
            {
                status = true,
                message = "Checkout preview",
                userId = userId,
                totalItems = items.Count,
                grandTotal = grandTotal,
                cartItems = items,
               
            });
        }

        // ✅ PLACE ORDER
        [HttpPost("placeorder")]
        public async Task<IActionResult> PlaceOrder([FromForm] CheckoutRequestModel request)
        {
            int? userId = GetUserIdFromToken();

            if (userId == null)
                return Unauthorized(new
                {
                    status = false,
                    message = "Aap login nahi hain. Checkout ke liye pehle login karein."
                });

            // Validation
            if (string.IsNullOrWhiteSpace(request.FirstName) ||
                string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Mobile) ||
                string.IsNullOrWhiteSpace(request.Pincode) ||
                string.IsNullOrWhiteSpace(request.Address) ||
                string.IsNullOrWhiteSpace(request.City) ||
                string.IsNullOrWhiteSpace(request.State))
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Sabhi address fields required hain."
                });
            }

            string guestId = GetGuestId();
            var result = await _businessLayer.PlaceOrder(userId.Value, guestId, request);

            return result;
        }

        // ✅ GET MY ORDERS
        [HttpGet("myorders")]
        public async Task<IActionResult> GetMyOrders()
        {
            int? userId = GetUserIdFromToken();

            if (userId == null)
                return Unauthorized(new
                {
                    status = false,
                    message = "Aap login nahi hain."
                });

            return await _businessLayer.GetMyOrders(userId.Value);
        }

        // ✅ GET ORDER DETAIL
        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetOrderDetail(int orderId)
        {
            int? userId = GetUserIdFromToken();

            if (userId == null)
                return Unauthorized(new
                {
                    status = false,
                    message = "Aap login nahi hain."
                });

            return await _businessLayer.GetOrderDetail(userId.Value, orderId);
        }

        [HttpGet("admin/allorders")]
        [Authorize]

        public async Task<IActionResult> GetAllOrders()
        {
            return await _businessLayer.GetAllOrders();
        }   
    }
}