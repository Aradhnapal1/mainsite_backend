using firstproject.Helpers;
using firstproject.Models;
using firstproject.Models.BusinessLayer;
using Microsoft.AspNetCore.Mvc;

namespace firstproject.Controllers
{
    [ApiController]
    [Route("api/cart")]
    public class CartController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;

        private readonly JwtHelper _jwtHelper;

        public CartController(
            IBusinessLayer businessLayer,
            JwtHelper jwtHelper
        )
        {
            _businessLayer = businessLayer;
            _jwtHelper = jwtHelper;
        }

        // =========================================
        // AUTH
        // =========================================

        private int? GetUserIdFromToken()
        {
            var authHeader =
                Request.Headers["Authorization"]
                .FirstOrDefault();

            if (
                string.IsNullOrEmpty(authHeader)
                || !authHeader.StartsWith("Bearer ")
            )
            {
                return null;
            }

            return _jwtHelper.GetUserIdFromToken(
                authHeader
                    .Replace("Bearer ", "")
                    .Trim()
            );
        }

        private string GetGuestId()
        {
            var guestId =
                Request.Cookies["guest_id"];

            if (!string.IsNullOrEmpty(guestId))
            {
                return guestId;
            }

            guestId =
                "guest_"
                + Guid.NewGuid().ToString("N");

            Response.Cookies.Append(
                "guest_id",
                guestId,
                new CookieOptions
                {
                    HttpOnly = true,

                    Expires =
                        DateTimeOffset.UtcNow
                        .AddDays(30),

                    SameSite = SameSiteMode.Lax,

                    Secure = false,

                    IsEssential = true
                }
            );

            return guestId;
        }

        private (
            int? userId,
            string? guestId
        ) GetIdentity()
        {
            var userId =
                GetUserIdFromToken();

            var guestId =
                userId.HasValue
                ? null
                : GetGuestId();

            return (
                userId,
                guestId
            );
        }

        // =========================================
        // ADD TO CART
        // =========================================

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart(
            [FromForm] int? productId = null,
            [FromForm] int? variantId = null
        )
        {
            if (
                !productId.HasValue
                && !variantId.HasValue
            )
            {
                return BadRequest(new
                {
                    status = false,

                    message =
                        "productId ya variantId bhejo"
                });
            }

            var (
                userId,
                guestId
            ) = GetIdentity();

            var result =
                await _businessLayer.AddToCart(
                    userId,
                    guestId,
                    productId,
                    variantId
                );

            var items =
                await _businessLayer.GetCart(
                    userId,
                    guestId
                );

            CartItemModel? item = null;

            // =====================================
            // PRODUCT
            // =====================================

            if (productId.HasValue)
            {
                item =
                    items.FirstOrDefault(x =>
                        x.productid
                        == productId
                    );
            }

            // =====================================
            // VARIANT
            // =====================================

            if (variantId.HasValue)
            {
                item =
                    items.FirstOrDefault(x =>
                        x.variantids != null
                        &&
                        x.variantids.Contains(
                            variantId.Value
                        )
                    );
            }

            return Ok(new
            {
                status = true,

                message = result,

                data = new
                {
                    id = item?.id,

                    productId =
                        item?.productid,

                    variantIds =
                        item?.variantids,

                    quantity =
                        item?.quantity,

                    name =
                        item?.Name,

                    image =
                        item?.Image,

                    price =
                        item?.Price,

                    totalprice =
                        item?.totalprice,

                    variants =
                        item?.variantids != null
                        && item.variantids.Any()

                        ? item?.Variants

                        : null
                }
            });
        }

        // =========================================
        // GET CART
        // =========================================

        [HttpGet("get")]
        public async Task<IActionResult> GetCart()
        {
            var (
                userId,
                guestId
            ) = GetIdentity();

            var items =
                await _businessLayer.GetCart(
                    userId,
                    guestId
                );

            return Ok(new
            {
                status = true,

                totalItems =
                    items.Count,

                grandTotal =
                    items.Sum(x =>
                        x.totalprice
                    ),

                data =
                    items.Select(x => new
                    {
                        id = x.id,

                        productId =
                            x.productid,

                        variantIds =
                            x.variantids,

                        quantity =
                            x.quantity,

                        name =
                            x.Name,

                        image =
                            x.Image,

                        price =
                            x.Price,

                        totalprice =
                            x.totalprice,

                        variants =
                            x.variantids != null
                            && x.variantids.Any()

                            ? x.Variants

                            : null
                    })
            });
        }

        // =========================================
        // UPDATE QUANTITY
        // =========================================

        [HttpPut("updatequantity")]
        public async Task<IActionResult> UpdateQuantity(
            [FromForm] int? productId = null,

            [FromForm] int? variantId = null,

            [FromForm] int change = 1
        )
        {
            if (
                !productId.HasValue
                && !variantId.HasValue
            )
            {
                return BadRequest(new
                {
                    status = false,
                    message =
                        "Product or Variant required"
                });
            }

            var (
                userId,
                guestId
            ) = GetIdentity();

            var result =
                await _businessLayer.UpdateCartQuantity(
                    userId,
                    guestId,
                    productId,
                    variantId,
                    change
                );

            var items =
                await _businessLayer.GetCart(
                    userId,
                    guestId
                );

            return Ok(new
            {
                status = true,

                message = result,

                totalItems =
                    items.Count,

                grandTotal =
                    items.Sum(x =>
                        x.totalprice
                    ),

                data =
                    items.Select(x => new
                    {
                        id = x.id,

                        productId =
                            x.productid,

                        variantIds =
                            x.variantids,

                        quantity =
                            x.quantity,

                        name =
                            x.Name,

                        image =
                            x.Image,

                        price =
                            x.Price,

                        totalprice =
                            x.totalprice,

                        variants =
                            x.Variants
                    })
            });
        }
        // =========================================
        // DELETE ITEM
        // =========================================

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteCartItem(
            int id
        )
        {
            return
                await _businessLayer
                .DeleteCartItem(id);
        }

        // =========================================
        // CLEAR CART
        // =========================================

        [HttpDelete("clearcart")]
        public async Task<IActionResult> ClearCart()
        {
            var (
                userId,
                guestId
            ) = GetIdentity();

            return
                await _businessLayer.ClearCart(
                    userId,
                    guestId
                );
        }

        // =========================================
        // ADD MULTIPLE
        // =========================================

        [HttpPost("add-multiple")]
        public async Task<IActionResult> AddMultipleToCart(
            [FromForm] List<int> productIds
        )
        {
            if (
                productIds == null
                || !productIds.Any()
            )
            {
                return BadRequest(new
                {
                    status = false,

                    message =
                        "ProductIds required"
                });
            }

            var (
                userId,
                guestId
            ) = GetIdentity();

            var result =
                await _businessLayer.AddMultipleToCart(
                    userId,
                    guestId,
                    productIds
                );

            var items =
                await _businessLayer.GetCart(
                    userId,
                    guestId
                );

            return Ok(new
            {
                status = true,

                message = result,

                totalItems =
                    items.Count,

                grandTotal =
                    items.Sum(x =>
                        x.totalprice
                    ),

                data =
                    items.Select(x => new
                    {
                        id = x.id,

                        productId =
                            x.productid,

                        variantIds =
                            x.variantids,

                        quantity =
                            x.quantity,

                        name =
                            x.Name,

                        image =
                            x.Image,

                        price =
                            x.Price,

                        totalprice =
                            x.totalprice,

                        variants =
                            x.variantids != null
                            && x.variantids.Any()

                            ? x.Variants

                            : null
                    })
            });
        }
    }
}