using firstproject.Models;
using firstproject.Models.BusinessLayer;
using firstproject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace firstproject.Controllers
{
    [ApiController]
    [Route("api/product")]
    public class ProductController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        private readonly CloudinaryService _cloudinary;

        public ProductController(IBusinessLayer businessLayer, CloudinaryService cloudinary)
        {
            _businessLayer = businessLayer;
            _cloudinary = cloudinary;
        }

        [HttpGet("getproduct")]
        public async Task<IActionResult> GetProduct()
        {
            var products = await _businessLayer.GetProduct();
            return Ok(products);
        }

        // 🔥 ADD PRODUCT
        [HttpPost("addproduct")]
        [Authorize]
        public async Task<IActionResult> AddProduct([FromForm] Productmodel product)
        {
            // ✅ Main image
            if (product.ImageFile != null)
            {
                var upload = await _cloudinary.UploadImageAsync(product.ImageFile, "products");
                product.Image = upload.Url;
                product.PublicId = upload.PublicId;
            }

            // ✅ Gallery images
            if (product.GalleryFiles != null && product.GalleryFiles.Length > 0)
            {
                var galleryList = new List<string>();
                var publicIds = new List<string>();

                foreach (var file in product.GalleryFiles)
                {
                    if (file != null)
                    {
                        var upload = await _cloudinary.UploadImageAsync(file, "products");
                        galleryList.Add(upload.Url);
                        publicIds.Add(upload.PublicId);
                    }
                }

                product.ImageGallery = galleryList.ToArray();
                product.GalleryPublicIds = publicIds.ToArray();
            }

            // ✅ Slug
            product.Slug = !string.IsNullOrEmpty(product.Slug)
                ? product.Slug
                : GenerateSlug(product.ProductName);

            var result = await _businessLayer.AddProduct(product);

            return Ok(new
            {
                status = true,
                message = "Product added successfully",
                data = result
            });
        }

        // 🔥 UPDATE PRODUCT
        [HttpPut("updateproduct/{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateProduct(int id, [FromForm] Productmodel product)
        {
            var existingProduct = await _businessLayer.GetProductById(id);

            if (existingProduct == null)
                return NotFound(new { status = false, message = "Product not found" });

            // ✅ Main image update
            if (product.ImageFile != null)
            {
                // delete old
                if (!string.IsNullOrEmpty(existingProduct.PublicId))
                {
                    await _cloudinary.DeleteImageAsync(existingProduct.PublicId);
                }

                var upload = await _cloudinary.UploadImageAsync(product.ImageFile, "products");
                product.Image = upload.Url;
                product.PublicId = upload.PublicId;
            }
            else
            {
                product.Image = existingProduct.Image;
                product.PublicId = existingProduct.PublicId;
            }

            // ✅ Gallery update
            if (product.GalleryFiles != null && product.GalleryFiles.Length > 0)
            {
                // delete old gallery
                if (existingProduct.GalleryPublicIds != null)
                {
                    foreach (var pid in existingProduct.GalleryPublicIds)
                    {
                        if (!string.IsNullOrEmpty(pid))
                        {
                            await _cloudinary.DeleteImageAsync(pid);
                        }
                    }
                }

                var galleryList = new List<string>();
                var publicIds = new List<string>();

                foreach (var file in product.GalleryFiles)
                {
                    if (file != null)
                    {
                        var upload = await _cloudinary.UploadImageAsync(file, "products");
                        galleryList.Add(upload.Url);
                        publicIds.Add(upload.PublicId);
                    }
                }

                product.ImageGallery = galleryList.ToArray();
                product.GalleryPublicIds = publicIds.ToArray();
            }
            else
            {
                product.ImageGallery = existingProduct.ImageGallery;
                product.GalleryPublicIds = existingProduct.GalleryPublicIds;
            }

            var result = await _businessLayer.UpdateProduct(id, product);

            return Ok(new
            {
                status = true,
                message = "Product updated successfully",
                data = result
            });
        }

        // 🔥 DELETE PRODUCT
        [HttpDelete("deleteproduct/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var existingProduct = await _businessLayer.GetProductById(id);

            if (existingProduct == null)
                return NotFound(new { status = false, message = "Product not found" });

            // delete main image
            if (!string.IsNullOrEmpty(existingProduct.PublicId))
            {
                await _cloudinary.DeleteImageAsync(existingProduct.PublicId);
            }

            // delete gallery images
            if (existingProduct.GalleryPublicIds != null)
            {
                foreach (var pid in existingProduct.GalleryPublicIds)
                {
                    if (!string.IsNullOrEmpty(pid))
                    {
                        await _cloudinary.DeleteImageAsync(pid);
                    }
                }
            }

            await _businessLayer.DeleteProduct(id);

            return Ok(new
            {
                status = true,
                message = "Product deleted successfully"
            });
        }

        [HttpGet("getproductbyid/{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _businessLayer.GetProductById(id);

            if (product == null)
                return NotFound(new { status = false, message = "Product not found" });

            return Ok(new { status = true, data = product });
        }

        // 🔥 SLUG
        private string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Guid.NewGuid().ToString();

            text = text.ToLower();
            text = System.Text.RegularExpressions.Regex.Replace(text, @"[^a-z0-9\s-]", "");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
            text = text.Replace(" ", "-");

            return text;
        }

        // 🔥 FILTER
        [HttpGet("filter")]
        public async Task<IActionResult> FilterProducts([FromQuery] ProductFilterModel? filter)
        {
            if (filter == null)
                filter = new ProductFilterModel();

            var result = await _businessLayer.FilterProducts(filter);

            return Ok(new
            {
                status = true,
                count = result.Count,
                data = result
            });
        }
    }
}