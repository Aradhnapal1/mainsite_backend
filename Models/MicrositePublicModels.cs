namespace firstproject.Models
{
    public class MicrositeOtpSendRequest
    {
        public string Domain { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Name { get; set; }
    }

    public class MicrositeOtpVerifyRequest
    {
        public string Domain { get; set; } = "";
        public string Email { get; set; } = "";
        public string Otp { get; set; } = "";
        public string? Name { get; set; }
    }

    public class MicrositeSingleOrderRequest
    {
        public string Domain { get; set; } = "";
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Mobile { get; set; } = "";
        public string Address { get; set; } = "";
        public string City { get; set; } = "";
        public string State { get; set; } = "";
        public string Pincode { get; set; } = "";
        public string Country { get; set; } = "India";
    }

    public class MicrositePublicUser
    {
        public int Id { get; set; }
        public long MicrositeId { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public class MicrositeResolvedData
    {
        public long MicrositeId { get; set; }
        public string Domain { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Heading { get; set; }
        public string? Content { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? LogoImage { get; set; }
        public string? BannerImage { get; set; }
        public object Theme { get; set; } = new { };
        public object Seo { get; set; } = new { };
    }

    public class MicrositeAssignedProduct
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string? Slug { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int Stock { get; set; }
        public List<string> Images { get; set; } = new();
    }
}
