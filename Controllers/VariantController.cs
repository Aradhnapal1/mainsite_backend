using firstproject.Models;
using firstproject.Models.BusinessLayer;
using firstproject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace firstproject.Controllers
{
    [ApiController]
    [Route("api/variant")]
    public class VariantController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        private readonly CloudinaryService _cloudinary;

        public VariantController(IBusinessLayer businessLayer, CloudinaryService cloudinary)
        {
            _businessLayer = businessLayer;
            _cloudinary = cloudinary;
        }

        // ✅ GET
        [HttpGet("getvariant")]
        public async Task<IActionResult> GetVariant()
        {
            var variants = await _businessLayer.GetVariant();
            return Ok(variants);
        }

        // 🔥 ADD VARIANT
        [HttpPost("addvariant")]
        [Authorize]
        public async Task<IActionResult> AddVariant([FromForm] Variantmodel variant)
        {
            try
            {
                // ✅ MAIN IMAGE
                if (variant.ImageFile != null)
                {
                    var upload = await _cloudinary.UploadImageAsync(variant.ImageFile, "variants");

                    variant.Image = upload.Url;
                    variant.PublicId = upload.PublicId;
                }

                // ✅ GALLERY IMAGES
                if (variant.GalleryFiles != null && variant.GalleryFiles.Length > 0)
                {
                    var galleryList = new List<string>();
                    var publicIds = new List<string>();

                    foreach (var file in variant.GalleryFiles)
                    {
                        if (file != null)
                        {
                            var upload = await _cloudinary.UploadImageAsync(file, "variants");

                            galleryList.Add(upload.Url);
                            publicIds.Add(upload.PublicId);
                        }
                    }

                    variant.ImageGallery = galleryList.ToArray();
                    variant.GalleryPublicIds = publicIds.ToArray();
                }

                var result = await _businessLayer.AddVariant(variant);

                return Ok(new
                {
                    status = true,
                    message = "Variant added successfully",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // 🔥 UPDATE VARIANT
        [HttpPut("updatevariant/{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateVariant(int id, [FromForm] Variantmodel variant)
        {
            var existing = await _businessLayer.GetVariantById(id);

            if (existing == null)
                return NotFound(new { message = "Variant not found" });

            // ✅ MAIN IMAGE UPDATE
            if (variant.ImageFile != null)
            {
                // delete old
                if (!string.IsNullOrEmpty(existing.PublicId))
                {
                    await _cloudinary.DeleteImageAsync(existing.PublicId);
                }

                var upload = await _cloudinary.UploadImageAsync(variant.ImageFile, "variants");

                variant.Image = upload.Url;
                variant.PublicId = upload.PublicId;
            }
            else
            {
                variant.Image = existing.Image;
                variant.PublicId = existing.PublicId;
            }

            // ✅ GALLERY UPDATE
            if (variant.GalleryFiles != null && variant.GalleryFiles.Length > 0)
            {
                // delete old gallery
                if (existing.GalleryPublicIds != null)
                {
                    foreach (var pid in existing.GalleryPublicIds)
                    {
                        if (!string.IsNullOrEmpty(pid))
                        {
                            await _cloudinary.DeleteImageAsync(pid);
                        }
                    }
                }

                var galleryList = new List<string>();
                var publicIds = new List<string>();

                foreach (var file in variant.GalleryFiles)
                {
                    if (file != null)
                    {
                        var upload = await _cloudinary.UploadImageAsync(file, "variants");

                        galleryList.Add(upload.Url);
                        publicIds.Add(upload.PublicId);
                    }
                }

                variant.ImageGallery = galleryList.ToArray();
                variant.GalleryPublicIds = publicIds.ToArray();
            }
            else
            {
                variant.ImageGallery = existing.ImageGallery;
                variant.GalleryPublicIds = existing.GalleryPublicIds;
            }

            var result = await _businessLayer.UpdateVariant(id, variant);

            return Ok(new
            {
                status = true,
                message = "Variant updated successfully",
                data = result
            });
        }

        // 🔥 DELETE VARIANT
        [HttpDelete("deletevariant/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteVariant(int id)
        {
            var existing = await _businessLayer.GetVariantById(id);

            if (existing == null)
                return NotFound(new { message = "Variant not found" });

            // 🔥 delete main image
            if (!string.IsNullOrEmpty(existing.PublicId))
            {
                await _cloudinary.DeleteImageAsync(existing.PublicId);
            }

            // 🔥 delete gallery
            if (existing.GalleryPublicIds != null)
            {
                foreach (var pid in existing.GalleryPublicIds)
                {
                    if (!string.IsNullOrEmpty(pid))
                    {
                        await _cloudinary.DeleteImageAsync(pid);
                    }
                }
            }

            await _businessLayer.DeleteVariant(id);

            return Ok(new
            {
                status = true,
                message = "Variant deleted successfully"
            });
        }
    }
}