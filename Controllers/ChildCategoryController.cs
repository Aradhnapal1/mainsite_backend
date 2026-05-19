using firstproject.Models;
using firstproject.Models.BusinessLayer;
using firstproject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace firstproject.Controllers
{
    [ApiController]
    [Route("api/childCategory")]
    public class ChildCategoryController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        private readonly CloudinaryService _cloudinary;

        public ChildCategoryController(IBusinessLayer businessLayer, CloudinaryService cloudinary)
        {
            _businessLayer = businessLayer;
            _cloudinary = cloudinary;
        }

        // ✅ GET
        [HttpGet("get")]
        public async Task<IActionResult> Get()
        {
            var result = await _businessLayer.GetAllChildCategory();
            return Ok(result);
        }

        // ✅ ADD (🔥 FIXED)
        [HttpPost("add")]
        [Authorize]
        public async Task<IActionResult> Add([FromForm] childCategoryModel model)
        {
            try
            {
                if (model.ImageFile != null)
                {
                    // 🔥 upload in childcategory folder
                    var upload = await _cloudinary.UploadImageAsync(model.ImageFile, "childcategory");

                    model.ChildCategoryImageUrl = upload.Url;
                    model.PublicId = upload.PublicId;
                }

                var result = await _businessLayer.Add(model);

                return Ok(new
                {
                    status = true,
                    message = "Child Category added successfully",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        // ✅ EDIT (🔥 FIXED)
        [HttpPut("edit/{id}")]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [FromForm] childCategoryModel model)
        {
            var existingData = await _businessLayer.GetChildCategoryById(id);

            if (existingData == null)
            {
                return NotFound(new
                {
                    status = false,
                    message = "Child Category not found"
                });
            }

            if (model.ImageFile != null)
            {
                // 🔥 delete old image
                if (!string.IsNullOrEmpty(existingData.PublicId))
                {
                    await _cloudinary.DeleteImageAsync(existingData.PublicId);
                }

                // 🔥 upload new image
                var upload = await _cloudinary.UploadImageAsync(model.ImageFile, "childcategory");

                model.ChildCategoryImageUrl = upload.Url;
                model.PublicId = upload.PublicId;
            }
            else
            {
                model.ChildCategoryImageUrl = existingData.ChildCategoryImageUrl;
                model.PublicId = existingData.PublicId;
            }

            await _businessLayer.Edit(id, model);

            return Ok(new
            {
                status = true,
                message = "Child Category updated successfully"
            });
        }

        // ✅ DELETE (🔥 FIXED)
        [HttpDelete("delete/{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var existingData = await _businessLayer.GetChildCategoryById(id);

            if (existingData == null)
            {
                return NotFound(new
                {
                    status = false,
                    message = "Child Category not found"
                });
            }

            // 🔥 delete image from cloudinary
            if (!string.IsNullOrEmpty(existingData.PublicId))
            {
                await _cloudinary.DeleteImageAsync(existingData.PublicId);
            }

            await _businessLayer.DeleteChildCategory(id);

            return Ok(new
            {
                status = true,
                message = "Child Category deleted successfully"
            });
        }
    }
}