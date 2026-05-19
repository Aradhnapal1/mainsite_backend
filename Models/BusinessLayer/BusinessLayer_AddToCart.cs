using firstproject.Models.DatabaseLayer;
using Microsoft.AspNetCore.Mvc;

namespace firstproject.Models.BusinessLayer
{
    // =========================================
    // INTERFACE
    // =========================================

    public partial interface IBusinessLayer
    {
        Task<List<CartItemModel>> GetCart(
            int? userId,
            string? ipAddress
        );

        Task<string> AddToCart(
            int? userId,
            string? ipAddress,
            int? productId = null,
            int? variantId = null
        );

        Task<string> AddMultipleToCart(
            int? userId,
            string? ipAddress,
            List<int> productIds
        );

        Task<string> UpdateCartQuantity(
            int? userId,
            string? ipAddress,
            int? productId = null,
            int? variantId = null,
            int change = 1
        );

        Task MergeGuestCart(
            int userId,
            string ipAddress
        );

        Task<IActionResult> DeleteCartItem(
            int id
        );

        Task<IActionResult> ClearCart(
            int? userId,
            string? ipAddress
        );
    }

    // =========================================
    // BUSINESS LAYER
    // =========================================

    public partial class BusinessLayer : IBusinessLayer
    {
        // =====================================
        // GET CART
        // =====================================

        public async Task<List<CartItemModel>> GetCart(
            int? userId,
            string? ipAddress
        )
        {
            return await _databaseLayer.GetCart(
                userId,
                ipAddress
            );
        }

        // =====================================
        // ADD TO CART
        // =====================================

        public async Task<string> AddToCart(
            int? userId,
            string? ipAddress,
            int? productId = null,
            int? variantId = null
        )
        {
            // ✅ validation

            if (
                !productId.HasValue
                && !variantId.HasValue
            )
            {
                return "InvalidRequest";
            }

            // ✅ login merge guest cart

            if (
                userId.HasValue
                && !string.IsNullOrEmpty(ipAddress)
            )
            {
                await _databaseLayer.MergeGuestCart(
                    userId.Value,
                    ipAddress
                );
            }

            return await _databaseLayer.AddToCart(
                userId,
                ipAddress,
                productId,
                variantId
            );
        }

        // =====================================
        // ADD MULTIPLE
        // =====================================

        public async Task<string> AddMultipleToCart(
            int? userId,
            string? ipAddress,
            List<int> productIds
        )
        {
            // ✅ validation

            if (
                productIds == null
                || !productIds.Any()
            )
            {
                return "InvalidProducts";
            }

            // ✅ login merge guest cart

            if (
                userId.HasValue
                && !string.IsNullOrEmpty(ipAddress)
            )
            {
                await _databaseLayer.MergeGuestCart(
                    userId.Value,
                    ipAddress
                );
            }

            return await _databaseLayer.AddMultipleToCart(
                userId,
                ipAddress,
                productIds
            );
        }

        // =====================================
        // UPDATE QUANTITY
        // ============================+++=========

        public async Task<string> UpdateCartQuantity(
            int? userId,
            string? ipAddress,
            int? productId = null,
            int? variantId = null,
            int change = 1
        )
        {
            return await _databaseLayer.UpdateCartQuantity(
                userId,
                ipAddress,
                productId,
                variantId,
                change
            );
        }

        // =====================================
        // MERGE GUEST CART
        // =====================================

        public async Task MergeGuestCart(
            int userId,
            string ipAddress
        )
        {
            if (
                string.IsNullOrEmpty(ipAddress)
            )
            {
                return;
            }

            await _databaseLayer.MergeGuestCart(
                userId,
                ipAddress
            );
        }

        // =====================================
        // DELETE ITEM
        // =====================================

        public async Task<IActionResult> DeleteCartItem(
            int id
        )
        {
            if (id <= 0)
            {
                return new BadRequestObjectResult(
                    new
                    {
                        status = false,
                        message = "Invalid cart id"
                    }
                );
            }

            return await _databaseLayer
                .DeleteCartItem(id);
        }

        // =====================================
        // CLEAR CART
        // =====================================

        public async Task<IActionResult> ClearCart(
            int? userId,
            string? ipAddress
        )
        {
            if (
                !userId.HasValue
                && string.IsNullOrEmpty(ipAddress)
            )
            {
                return new BadRequestObjectResult(
                    new
                    {
                        status = false,
                        message =
                            "User or Guest required"
                    }
                );
            }

            return await _databaseLayer.ClearCart(
                userId,
                ipAddress
            );
        }
    }
}