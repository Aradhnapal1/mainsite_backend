using firstproject.Models;
using firstproject.Models.BusinessLayer;
using firstproject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace firstproject.Controllers
{
    [ApiController]
    [Route("api/subcategory")]
    public class SubCategoryController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        private readonly CloudinaryService _cloudinary;

        public SubCategoryController(IBusinessLayer businessLayer, CloudinaryService cloudinary)
        {
            _businessLayer = businessLayer;
            _cloudinary = cloudinary;
        }

        // ✅ GET
        [HttpGet("get")]
        public async Task<IActionResult> Get()
        {
            var result = await _businessLayer.GetAllSubCategory();
            return Ok(result);
        }

        // 🔥 ADD
        [HttpPost("add")]
        [Authorize]
        public async Task<IActionResult> Add([FromForm] SubCategoryModel model)
        {
            if (model.ImageFile != null)
            {
                var upload = await _cloudinary.UploadImageAsync(model.ImageFile, "subcategory");

                model.SubCategoryImageUrl = upload.Url;
                model.PublicId = upload.PublicId; // 🔥 SAVE
            }

            await _businessLayer.Add(model);

            return Ok(new
            {
                status = true,
                message = "Record successfully added"
            });
        }

        // 🔥 EDIT
        [HttpPut("edit/{id}")]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [FromForm] SubCategoryModel model)
        {
            var existing = await _businessLayer.GetSubCategoryById(id);

            if (existing == null)
                return NotFound(new
                {
                    status = false,
                    message = "SubCategory not found"
                });

            if (model.ImageFile != null)
            {
                // 🔥 DELETE OLD IMAGE
                if (!string.IsNullOrEmpty(existing.PublicId))
                {
                    await _cloudinary.DeleteImageAsync(existing.PublicId);
                }

                // 🔥 UPLOAD NEW
                var upload = await _cloudinary.UploadImageAsync(model.ImageFile, "subcategory");

                model.SubCategoryImageUrl = upload.Url;
                model.PublicId = upload.PublicId;
            }
            else
            {
                model.SubCategoryImageUrl = existing.SubCategoryImageUrl;
                model.PublicId = existing.PublicId;
            }

            await _businessLayer.Edit(id, model);

            return Ok(new
            {
                status = true,
                message = "Record successfully updated"
            });
        }

        // 🔥 DELETE
        [HttpDelete("delete/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteSubCategory(int id)
        {
            var existing = await _businessLayer.GetSubCategoryById(id);

            if (existing == null)
                return NotFound(new
                {
                    status = false,
                    message = "SubCategory not found"
                });

            // 🔥 DELETE IMAGE FROM CLOUDINARY
            if (!string.IsNullOrEmpty(existing.PublicId))
            {
                await _cloudinary.DeleteImageAsync(existing.PublicId);
            }

            await _businessLayer.DeleteSubCategory(id);

            return Ok(new
            {
                status = true,
                message = "Record successfully deleted"
            });
        }
    }
}