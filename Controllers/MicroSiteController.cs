using firstproject.Models;
using firstproject.Models.BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Drawing;


namespace firstproject.Controllers
{
    [ApiController]
    [Route("api/admin")]
[ApiExplorerSettings(GroupName = "microsite-v1")]
    [Authorize]
    public class MicroSiteController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;

        public MicroSiteController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
        }

        //======================================================== MicroSite_website Start =============================================================
        [HttpGet("all-microsite")]
        public async Task<IActionResult> GetMicrosite()
        {
            var result = await _businessLayer.GetMicrosite();
            return Ok(result);
        }

        [HttpGet("microsite/{id}")]
        public async Task<IActionResult> GetMicrositeById(long id)
        {
            var result = await _businessLayer.GetMicrositeById(id);

            if (result == null)
                return NotFound(new { error = "Microsite not found" });

            return Ok(result);
        }

        [HttpPost("create-microsite")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateMicrosite([FromForm] MicrositeModel model)
        {
            try
            {
                // 🔥 DEBUG (optional)
                Console.WriteLine("ThemeJson: " + model.ThemeJson);
                Console.WriteLine("SeoJson: " + model.SeoJson);

                // 🔥 JSON → OBJECT
                if (!string.IsNullOrEmpty(model.ThemeJson))
                {
                    model.Theme = JsonConvert.DeserializeObject<MicrositeTheme>(model.ThemeJson);
                }

                if (!string.IsNullOrEmpty(model.SeoJson))
                {
                    model.Seo = JsonConvert.DeserializeObject<MicrositeSeo>(model.SeoJson);
                }

                // ❗ Required check (agar rakhna hai)
                if (model.Theme == null)
                    return BadRequest("Theme is required");

                if (model.Seo == null)
                    return BadRequest("Seo is required");

                var result = await _businessLayer.CreateMicrosite(model);

                return Ok(new
                {
                    message = "Microsite Created Successfully",
                    micrositeId = result.Id,
                    uniqueId = result.UniqueId,
                    micrositeUrl = result.Url
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("update-microsite/{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateMicrosite(long id, [FromForm] MicrositeModel model)
        {
            ApplyMicrositeUpdateThemeSeo(model);
            var result = await _businessLayer.UpdateMicrosite(id, model);
            return Ok(new { message = result });
        }

        /// <summary>
        /// JSON body update (separate path so Swagger/OpenAPI stays valid). Multipart files: use PUT update-microsite/{id}.
        /// </summary>
        [HttpPut("update-microsite/{id}/json")]
        [Consumes("application/json")]
        public async Task<IActionResult> UpdateMicrositeFromJson(long id, [FromBody] MicrositeModel model)
        {
            ApplyMicrositeUpdateThemeSeo(model);
            var result = await _businessLayer.UpdateMicrosite(id, model);
            return Ok(new { message = result });
        }

        /// <summary>
        /// ThemeJson/SeoJson override nested theme/seo when provided. Otherwise nested objects from JSON body or form remain.
        /// </summary>
        private static void ApplyMicrositeUpdateThemeSeo(MicrositeModel model)
        {
            if (!string.IsNullOrEmpty(model.ThemeJson))
                model.Theme = JsonConvert.DeserializeObject<MicrositeTheme>(model.ThemeJson);

            if (!string.IsNullOrEmpty(model.SeoJson))
                model.Seo = JsonConvert.DeserializeObject<MicrositeSeo>(model.SeoJson);
        }

        [HttpDelete("delete-microsite/{id}")]
        public async Task<IActionResult> DeleteMicrosite(long id)
        {
            var result = await _businessLayer.DeleteMicrosite(id);

            if (result.Contains("Not Found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = result });

            return Ok(new { message = result });
        }

        ////================================================================assign_product Microsite Start ===============================================================

        [HttpPost("assign-product")]
        public async Task<IActionResult> AssignProduct([FromBody] AssignProductRequest model)
        {
            //if (model.MicrositeId <= 0 || model.ProductId <= 0)
            //    return BadRequest(new { message = "Invalid MicrositeId or ProductId" });

            var result = await _businessLayer.AssignProduct(model.MicrositeId, model.ProductId);

            if (!result)
                return BadRequest(new { message = "Assignment failed" });

            return Ok(new { message = "Product Assigned Successfully" });
        }

        [HttpGet("assigned-products")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAssignedProducts()
        {
            var result = await _businessLayer.GetAssignedProducts();
            return Ok(result);
        }

        [HttpDelete("assigned-product/{id}")]
        public async Task<IActionResult> DeleteAssignedProduct(long id)
        {
            var result = await _businessLayer.DeleteAssignedProduct(id);

            if (!result)
                return NotFound(new { message = "Not found" });

            return Ok(new { message = "Deleted Successfully" });
        }

        [HttpPut("assigned-product/{id}")]
        public async Task<IActionResult> UpdateAssignedProduct(long id, [FromBody] AssignProductUpdateRequest model)
        {
            var result = await _businessLayer.UpdateAssignedProduct(id, model.MicrositeId, model.ProductId, model.Status);
            if (!result)
                return NotFound(new { message = "Assigned product not found" });

            return Ok(new { message = "Assigned product updated successfully" });
        }

        [HttpGet("microsite-orders")]
        public async Task<IActionResult> GetMicrositeOrdersForAdmin([FromQuery] long? micrositeId, [FromQuery] string? domain)
        {
            long resolvedMicrositeId;

            if (micrositeId.HasValue && micrositeId.Value > 0)
            {
                resolvedMicrositeId = micrositeId.Value;
            }
            else if (!string.IsNullOrWhiteSpace(domain))
            {
                var microsite = await _businessLayer.GetMicrositePublicData(domain);
                if (microsite == null)
                    return NotFound(new { status = false, message = "Microsite domain invalid hai." });
                resolvedMicrositeId = microsite.MicrositeId;
            }
            else
            {
                return BadRequest(new { status = false, message = "micrositeId ya domain required hai." });
            }

            return await _businessLayer.GetAdminMicrositeOrders(resolvedMicrositeId);
        }

        [HttpGet("microsite-orders/{orderId}")]
        public async Task<IActionResult> GetMicrositeOrderDetailForAdmin(int orderId, [FromQuery] long? micrositeId, [FromQuery] string? domain)
        {
            long resolvedMicrositeId;

            if (micrositeId.HasValue && micrositeId.Value > 0)
            {
                resolvedMicrositeId = micrositeId.Value;
            }
            else if (!string.IsNullOrWhiteSpace(domain))
            {
                var microsite = await _businessLayer.GetMicrositePublicData(domain);
                if (microsite == null)
                    return NotFound(new { status = false, message = "Microsite domain invalid hai." });
                resolvedMicrositeId = microsite.MicrositeId;
            }
            else
            {
                return BadRequest(new { status = false, message = "micrositeId ya domain required hai." });
            }

            return await _businessLayer.GetAdminMicrositeOrderDetail(resolvedMicrositeId, orderId);
        }

        [HttpPut("microsite-orders/{orderId}/status")]
        public async Task<IActionResult> UpdateMicrositeOrderStatus(int orderId, [FromBody] MicrositeOrderStatusUpdateRequest request, [FromQuery] long? micrositeId, [FromQuery] string? domain)
        {
            if (string.IsNullOrWhiteSpace(request.Status))
                return BadRequest(new { status = false, message = "Status required hai." });

            long resolvedMicrositeId;
            if (micrositeId.HasValue && micrositeId.Value > 0)
            {
                resolvedMicrositeId = micrositeId.Value;
            }
            else if (!string.IsNullOrWhiteSpace(domain))
            {
                var microsite = await _businessLayer.GetMicrositePublicData(domain);
                if (microsite == null)
                    return NotFound(new { status = false, message = "Microsite domain invalid hai." });
                resolvedMicrositeId = microsite.MicrositeId;
            }
            else
            {
                return BadRequest(new { status = false, message = "micrositeId ya domain required hai." });
            }

            var updated = await _businessLayer.UpdateMicrositeOrderStatus(resolvedMicrositeId, orderId, request.Status.Trim());
            if (!updated)
                return NotFound(new { status = false, message = "Order nahi mila." });

            return Ok(new { status = true, message = "Order status updated." });
        }

        [HttpDelete("microsite-orders/{orderId}")]
        public async Task<IActionResult> DeleteMicrositeOrder(int orderId, [FromQuery] long? micrositeId, [FromQuery] string? domain)
        {
            long resolvedMicrositeId;
            if (micrositeId.HasValue && micrositeId.Value > 0)
            {
                resolvedMicrositeId = micrositeId.Value;
            }
            else if (!string.IsNullOrWhiteSpace(domain))
            {
                var microsite = await _businessLayer.GetMicrositePublicData(domain);
                if (microsite == null)
                    return NotFound(new { status = false, message = "Microsite domain invalid hai." });
                resolvedMicrositeId = microsite.MicrositeId;
            }
            else
            {
                return BadRequest(new { status = false, message = "micrositeId ya domain required hai." });
            }

            var deleted = await _businessLayer.DeleteMicrositeOrder(resolvedMicrositeId, orderId);
            if (!deleted)
                return NotFound(new { status = false, message = "Order nahi mila." });

            return Ok(new { status = true, message = "Order deleted." });
        }
    }

}

