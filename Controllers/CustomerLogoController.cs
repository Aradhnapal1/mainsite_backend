using firstproject.Models;
using firstproject.Models.BusinessLayer;
using firstproject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace firstproject.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class CustomerLogoController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        private readonly CloudinaryService _cloudinary;

        public CustomerLogoController(IBusinessLayer businessLayer, CloudinaryService cloudinary)
        {
            _businessLayer = businessLayer;
            _cloudinary = cloudinary;
        }

        // ✅ GET
        [HttpGet("getcustomerlogo")]
        public async Task<IActionResult> Get()
        {
            var result = await _businessLayer.GetCustomerLogo();
            return Ok(result);
        }

        // ✅ ADD (🔥 FIXED)
        [HttpPost("addcustomerlogo")]
        [Authorize]
        public async Task<IActionResult> Add([FromForm] customermodel model)
        {
            if (model.ImageFile != null)
            {
                // 🔥 upload in customerlogo folder
                var upload = await _cloudinary.UploadImageAsync(model.ImageFile, "customerlogo");

                model.customerimage = upload.Url;
                model.PublicId = upload.PublicId;
            }

            var result = await _businessLayer.Add(model);

            return Ok(new
            {
                status = true,
                message = "Customer logo added successfully",
                data = result
            });
        }

        // ✅ EDIT (🔥 FIXED)
        [HttpPut("editcustomerlogo/{id}")]
        [Authorize]
        public async Task<IActionResult> Edit(int id, [FromForm] customermodel model)
        {
            var existing = await _businessLayer.GetCustomerLogoById(id);

            if (existing == null)
                return NotFound(new
                {
                    status = false,
                    message = "Customer logo not found"
                });

            if (model.ImageFile != null)
            {
                // 🔥 DELETE OLD IMAGE
                if (!string.IsNullOrEmpty(existing.PublicId))
                {
                    await _cloudinary.DeleteImageAsync(existing.PublicId);
                }

                // 🔥 UPLOAD NEW IMAGE
                var upload = await _cloudinary.UploadImageAsync(model.ImageFile, "customerlogo");

                model.customerimage = upload.Url;
                model.PublicId = upload.PublicId;
            }
            else
            {
                model.customerimage = existing.customerimage;
                model.PublicId = existing.PublicId;
            }

            await _businessLayer.Edit(id, model);

            return Ok(new
            {
                status = true,
                message = "Record successfully updated"
            });
        }

        // ✅ DELETE (🔥 FIXED)
        [HttpDelete("deletecustomerlogo/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteCustomerLogo(int id)
        {
            var existing = await _businessLayer.GetCustomerLogoById(id);

            if (existing == null)
                return NotFound(new
                {
                    status = false,
                    message = "Customer logo not found"
                });

            // 🔥 DELETE IMAGE FROM CLOUDINARY
            if (!string.IsNullOrEmpty(existing.PublicId))
            {
                await _cloudinary.DeleteImageAsync(existing.PublicId);
            }

            await _businessLayer.DeleteCustomerLogo(id);

            return Ok(new
            {
                status = true,
                message = "Record successfully deleted"
            });
        }
    }
}