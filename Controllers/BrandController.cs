using firstproject.Models;
using firstproject.Models.BusinessLayer;
using firstproject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace firstproject.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class BrandController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        private readonly CloudinaryService _cloudinary;

        public BrandController(IBusinessLayer businessLayer, CloudinaryService cloudinary)
        {
            _businessLayer = businessLayer;
            _cloudinary = cloudinary;
        }

        // ✅ GET
        [HttpGet("getbrand")]
        public async Task<IActionResult> Get()
        {
            var result = await _businessLayer.GetBrand();
            return Ok(new { status = true, data = result });
        }

        // ✅ ADD BRAND
        [HttpPost("addbrand")]
        [Authorize]
        public async Task<IActionResult> Add([FromForm] Brandmodel model)
        {
            var result = await _businessLayer.Add(model);

            if (model.ImageFile != null)
            {
                var upload = await _cloudinary.UploadImageAsync(
                    model.ImageFile,
                    $"brands/{result.Id}"
                );

                model.BrandImage = upload.Url;
                model.PublicId = upload.PublicId;

                await _businessLayer.UpdateBrandImage(result.Id, model);
            }

            return Ok(new
            {
                status = true,
                message = "Brand added successfully",
                data = result
            });
        }

        // ✅ EDIT BRAND (🔥 FIXED REPLACE)
        [HttpPut("editbrand/{id}")]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [FromForm] Brandmodel model)
        {
            var existing = await _businessLayer.GetBrandById(id);

            if (existing == null)
                return NotFound(new { status = false, message = "Brand not found" });

            if (model.ImageFile != null)
            {
                var upload = await _cloudinary.ReplaceImageAsync(
                    model.ImageFile,
                    $"brands/{id}"   // 🔥 SAME ID
                );

                model.BrandImage = upload.Url;
                model.PublicId = upload.PublicId;
            }
            else
            {
                model.BrandImage = existing.BrandImage;
                model.PublicId = existing.PublicId;
            }

            await _businessLayer.Edit(id, model);

            return Ok(new
            {
                status = true,
                message = "Brand updated successfully"
            });
        }

        // ✅ DELETE BRAND
        [HttpDelete("deletebrand/{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            await _cloudinary.DeleteImageAsync($"brands/{id}");

            await _businessLayer.DeleteBrand(id);

            return Ok(new
            {
                status = true,
                message = "Brand deleted successfully"
            });
        }
    }
}