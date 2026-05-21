
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;

namespace firstproject.Models




{
    public class AdminModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string? PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; }

        public string? Token { get; set; }
    }

    public class AdminLoginModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }


    public class Size
    {
        public int Id { get; set; }

        public string SizeName { get; set; }   // S, M, L, XL, 42, 44

        public string Description { get; set; } // Optional

        public bool IsActive { get; set; } = true;
    }

    public class categoryModel
    {
        public int id { get; set; }

        public string Name { get; set; }

        public bool Status { get; set; }

        // ✅ DB ke liye (nullable)
        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }

        // ✅ Image upload ke liye (IMPORTANT)
        public IFormFile? ImageFile { get; set; }
        public string? PublicId { get; set; }
    }

    public class SubCategoryModel
    {
        public int Id { get; set; }

        public string? SubCategoryName { get; set; }

        public string? SubCategoryImageUrl { get; set; }

        public int CategoryId { get; set; }

        public string? CategoryName { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool Status { get; set; } = true;

        public IFormFile? ImageFile { get; set; }
        public string? PublicId { get; set; }
    }

    public class childCategoryModel
    {
        public int Id { get; set; }
        public string? ChildCategoryName { get; set; }
        public string? ChildCategoryImageUrl { get; set; }
        public int SubCategoryId { get; set; }
        public string? SubCategoryName { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Status { get; set; } = true;
        public IFormFile? ImageFile { get; set; }
        public string? PublicId { get; set; }
    }



    public class Brandmodel
    {
        public int Id { get; set; }
        public string? BrandName { get; set; }
       
        public string? BrandImage { get; set; }
        public IFormFile? ImageFile { get; set; }
        public bool IsActive { get; set; } = true;
        public string? PublicId { get; set; }
    }

    public class customermodel
    {
        public int id { get; set; }
        public string? customername { get; set; }
        public string? customerimage { get; set; }
        public IFormFile? ImageFile { get; set; }
        public bool status { get; set; } = true;

        public DateTime createdat { get; set; }
        public string? PublicId { get; set; }
    }

    public class Colormodel
    {
        public int Id { get; set; }
        public string? Colorname { get; set; }
        public string? Colorcode { get; set; }
        public bool Isactive { get; set; } = true;
    }


    public class Usermodel
    {
        public int Id { get; set; }
        public string? Firstname { get; set; }
        public string? Lastname { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }
        public bool Isactive { get; set; } = true;
        public string? Token { get; set; }
        public DateTime Createdat { get; set; }
    }

    public class UserLoginModel
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    public class ForgotPasswordRequest
    {
        public string? Email { get; set; }
    }

    public class ForgotPasswordVerifyOtpRequest
    {
        public string? Email { get; set; }
        public string? Otp { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string? Email { get; set; }
        public string? Otp { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }
    }


    public class Contactmodel 
    {
        public int Id { get; set; }

        public string FullName { get; set; }

        public string Email { get; set; }

        public string? PhoneNumber { get; set; }
        public string? Subject { get; set; }

        public string Message { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; }
    }

    public class Productmodel
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? Sku { get; set; }
        public string? ShortDescription { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int Stock { get; set; }

        public int CategoryId { get; set; }
        public int SubCategoryId { get; set; }
        public int? ChildCategoryId { get; set; }
        public int? BrandId { get; set; }

        public int[]? SizeIds { get; set; }
        public int[]? ColorIds { get; set; }

        // 🔥 FILES
        public IFormFile? ImageFile { get; set; }
        public IFormFile[]? GalleryFiles { get; set; }

        // 🔥 STORED DATA
        public string? Image { get; set; }
        public string[]? ImageGallery { get; set; }

        // 🔥 NEW (VERY IMPORTANT)
        public string? PublicId { get; set; }              // main image
        public string[]? GalleryPublicIds { get; set; }    // gallery images

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // ✅ Name fields
        public string? CategoryName { get; set; }
        public string? SubCategoryName { get; set; }
        public string? ChildCategoryName { get; set; }
        public string? BrandName { get; set; }

        public List<string>? SizeNames { get; set; }
        public List<string>? ColorNames { get; set; }

        public List<Variantmodel>? Variants { get; set; }
    }

    public class ProductFilterModel
    {
        public int[]? CategoryIds { get; set; }
        public int[]? SubCategoryIds { get; set; }
        public int[]? ChildCategoryIds { get; set; }

        public int? BrandId { get; set; }

        public int[]? SizeIds { get; set; }
        public int[]? ColorIds { get; set; }

        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        public string? Search { get; set; }
    }



    public class Variantmodel
    {
        public int Id { get; set; }
        public string? VariantName { get; set; }

        public int ProductId { get; set; }
        public int[]? SizeId { get; set; }
        public int[]? ColorId { get; set; }

        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int Stock { get; set; }

        public string? Sku { get; set; }

        // Images
        public IFormFile? ImageFile { get; set; }
        public IFormFile[]? GalleryFiles { get; set; }

        public string? Image { get; set; }
        public string[]? ImageGallery { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // For joins (optional)
        public string[]? SizeNames { get; set; }
        public string[]? ColorNames { get; set; }
        public string[]? GalleryPublicIds { get; set; }
        public string? PublicId { get; set; }

    }

    public class CartItemModel
    {
        public int id { get; set; }

        public int? userid { get; set; }

        public string? ipaddress { get; set; }

        public int? productid { get; set; }

        // ✅ VARIANT ARRAY
        public int[]? variantids { get; set; }

        public int quantity { get; set; }

        // ✅ PRODUCT DATA
        public string? Name { get; set; }

        public string? Image { get; set; }

        // ✅ VARIANT DATA ARRAY
        public List<VariantCartModel>? Variants { get; set; }

        // ✅ FINAL
        public decimal Price { get; set; }

        public decimal totalprice { get; set; }

        public DateTime? createdat { get; set; }
    }

    public class VariantCartModel
    {
        public int id { get; set; }

        public string? variantname { get; set; }

        public int[]? sizeid { get; set; }

        public int[]? colorid { get; set; }

        public decimal price { get; set; }

        public string? image { get; set; }

        public string[]? imagegallery { get; set; }
    }
    public class AddToCartRequest
    {
        public int ProductId { get; set; }
        public string? ipaddress { get; set; }
    }

  

    public class CheckoutRequestModel
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Mobile { get; set; } = "";
        public string Pincode { get; set; } = "";
        public string Address { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string Country { get; set; } = "India";
        public string PaymentMethod { get; set; } = "COD";
    }

    public class CloudinarySettings
    {
        public string CloudName { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
    }


    public class Blogmodel
    {
        public int Id { get; set; }

        //public int UserId { get; set; }

        public string? Title { get; set; }

        public string? Slug { get; set; }

        public string? Content { get; set; }

        public string? FeaturedImage { get; set; }

        public IFormFile? ImageFile { get; set; }

        public string? Status { get; set; } = "draft";

        public int Views { get; set; } = 0;

        public string? Author { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;
        public string? PublicId { get; set; }


    }





}
