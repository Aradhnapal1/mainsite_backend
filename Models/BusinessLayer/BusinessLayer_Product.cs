using firstproject.Models.DatabaseLayer;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace firstproject.Models.BusinessLayer
{
    public partial interface IBusinessLayer
    {
        Task<List<Productmodel>> GetProduct();
        Task<IActionResult> AddProduct(Productmodel product);
        Task<Productmodel?> GetProductById(int id);
        Task<IActionResult> UpdateProduct(int id, Productmodel product);
        Task<IActionResult> DeleteProduct(int id);
        Task<List<Productmodel>> FilterProducts(ProductFilterModel filter);

    }

    public partial class BusinessLayer : IBusinessLayer
    {
       

        public BusinessLayer(IDatabaseLayer databaseLayer)
        {
            _databaseLayer = databaseLayer;
        }

        // ✅ Get All Products
        public async Task<List<Productmodel>> GetProduct()
        {
            return await _databaseLayer.GetProduct();
        }

        // ✅ Add Product
        public async Task<IActionResult> AddProduct(Productmodel product)
        {
            // 🔥 Basic validation
            if (string.IsNullOrWhiteSpace(product.ProductName))
                return new BadRequestObjectResult(new { status = false, message = "Product name is required" });

            if (product.Price <= 0)
                return new BadRequestObjectResult(new { status = false, message = "Price must be greater than 0" });

            // 🔥 Slug generate (safety)
            if (string.IsNullOrEmpty(product.Slug))
            {
                var baseSlug = GenerateSlug(product.ProductName);
                product.Slug = baseSlug + "-" + Guid.NewGuid().ToString().Substring(0, 5);
            }

            return await _databaseLayer.AddProduct(product);
        }

        // ✅ Get By Id
        public async Task<Productmodel?> GetProductById(int id)
        {
            return await _databaseLayer.GetProductById(id);
        }

        // ✅ Update Product
        public async Task<IActionResult> UpdateProduct(int id, Productmodel product)
        {
            if (id <= 0)
                return new BadRequestObjectResult(new { status = false, message = "Invalid product id" });

            if (string.IsNullOrWhiteSpace(product.ProductName))
                return new BadRequestObjectResult(new { status = false, message = "Product name is required" });

            // 🔥 Update slug if name changed
            if (string.IsNullOrEmpty(product.Slug))
            {
                product.Slug = GenerateSlug(product.ProductName);
            }

            return await _databaseLayer.UpdateProduct(id, product);
        }

        // ✅ Delete Product
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (id <= 0)
                return new BadRequestObjectResult(new { status = false, message = "Invalid product id" });

            return await _databaseLayer.DeleteProduct(id);
        }

        // 🔥 Slug Generator
        private string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Guid.NewGuid().ToString();

            text = text.ToLower();
            text = Regex.Replace(text, @"[^a-z0-9\s-]", "");
            text = Regex.Replace(text, @"\s+", " ").Trim();

            return text.Replace(" ", "-");
        }

        public async Task<List<Productmodel>> FilterProducts(ProductFilterModel filter)
        {
            // 🔥 NULL SAFE
            filter ??= new ProductFilterModel();

            // 🔥 SEARCH CLEAN
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                filter.Search = filter.Search.Trim();
            }

            // 🔥 OPTIONAL: EMPTY ARRAY SAFE (avoid null checks everywhere)
            filter.CategoryIds ??= Array.Empty<int>();
            filter.SubCategoryIds ??= Array.Empty<int>();
            filter.ChildCategoryIds ??= Array.Empty<int>();
            filter.SizeIds ??= Array.Empty<int>();
            filter.ColorIds ??= Array.Empty<int>();

            return await _databaseLayer.FilterProducts(filter);
        }
    }
}