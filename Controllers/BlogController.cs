using firstproject.Models;
using firstproject.Models.BusinessLayer;
using firstproject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace firstproject.Controllers
{
    [Route("api/blog")]
    public class BlogController : ControllerBase
    {

        private readonly IBusinessLayer _businessLayer;
        private readonly CloudinaryService _cloudinary;


        public BlogController(IBusinessLayer businessLayer, CloudinaryService cloudinary)
        {
            _businessLayer = businessLayer;
            _cloudinary = cloudinary;
        }
        [HttpGet("getall")]
        public async Task<IActionResult> GetAllBlogs()
        {
            var result = await _businessLayer.GetAllBlogs();
            return Ok(new { status = true, data = result });
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddBlog([FromForm] Blogmodel model)
        {
            // ✅ Upload image first
            if (model.ImageFile != null)
            {
                var upload = await _cloudinary.UploadImageAsync(
                    model.ImageFile,
                    "blogs"
                );

                model.FeaturedImage = upload.Url;
                model.PublicId = upload.PublicId;
            }

            // ✅ Then save in DB
            var result = await _businessLayer.AddBlog(model);

            return Ok(new
            {
                status = true,
                message = "Blog added successfully",
                data = result
            });
        }


        [HttpPut("edit/{id}")]
        public async Task<IActionResult> EditBlog(
    int id,
    [FromForm] Blogmodel model)
        {
            // ✅ Upload new image
            if (model.ImageFile != null)
            {
                var upload = await _cloudinary.UploadImageAsync(
                    model.ImageFile,
                    "blogs"
                );

                model.FeaturedImage = upload.Url;
                model.PublicId = upload.PublicId;
            }

            // ✅ Update DB
            var result = await _businessLayer.EditBlog(id, model);

            if (result == null)
            {
                return NotFound(new
                {
                    status = false,
                    message = "Blog not found"
                });
            }

            return Ok(new
            {
                status = true,
                message = "Blog updated successfully",
                data = result
            });
        }



        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteBlog(int id)
        {

            await _cloudinary.DeleteImageAsync($"blogs/{id}"); // ✅ Delete image from Cloudinary

            await _businessLayer.DeleteBlog(id); // ✅ Then delete from DB

            return Ok(new
            {
                status = true,
                message = "Blog deleted successfully"
            });


        }


        [HttpGet("getblogbyid/{id}")]
        public async Task<IActionResult> GetBlogById(int id)
        {
            var result = await _businessLayer.GetBlogById(id);

            if (result == null)
            {
                return NotFound(new
                {
                    status = false,
                    message = "Blog not found"
                });
            }

            return Ok(new
            {
                status = true,
                data = result
            });
        }


    }




 }
