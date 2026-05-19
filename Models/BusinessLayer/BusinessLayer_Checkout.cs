using firstproject.Models.DatabaseLayer;
using Microsoft.AspNetCore.Mvc;

namespace firstproject.Models.BusinessLayer
{
    public partial interface IBusinessLayer
    {
        Task<IActionResult> PlaceOrder(int userId, string guestId, CheckoutRequestModel request);
        Task<IActionResult> GetMyOrders(int userId);
        Task<IActionResult> GetOrderDetail(int userId, int orderId);

        Task<IActionResult> GetAllOrders();
    }

    public partial class BusinessLayer : IBusinessLayer
    {
        public async Task<IActionResult> PlaceOrder(int userId, string guestId, CheckoutRequestModel request)
        {
            // Pehle cart merge karo agar guest cart hai
            if (!string.IsNullOrEmpty(guestId))
                await MergeGuestCart(userId, guestId);

            // Cart items lo
            var items = await _databaseLayer.GetCart(userId, null);

            if (items.Count == 0)
                return new OkObjectResult(new
                {
                    status = false,
                    message = "Cart khali hai. Kuch products add karein."
                });

            decimal grandTotal = items.Sum(x => x.totalprice);

            return await _databaseLayer.PlaceOrder(userId, request, items, grandTotal);
        }

        public async Task<IActionResult> GetMyOrders(int userId)
        {
            return await _databaseLayer.GetMyOrders(userId);
        }

        public async Task<IActionResult> GetOrderDetail(int userId, int orderId)
        {
            return await _databaseLayer.GetOrderDetail(userId, orderId);
        }

        public async Task<IActionResult> GetAllOrders()
        {
            return await _databaseLayer.GetAllOrders();
        }
    }
}