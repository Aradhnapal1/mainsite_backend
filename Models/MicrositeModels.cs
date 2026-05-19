namespace firstproject.Models
{
    public class MicrositeModel
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Slug { get; set; }
        public string? Heading { get; set; }
        public string? Content { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? LogoImage { get; set; }
        public string? BannerImage { get; set; }
        public string? Favicon { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool Status { get; set; } = true;
        public string? Url { get; set; }
        public string? UniqueId { get; set; }
        public List<string> Domains { get; set; } = new();
        public MicrositeTheme? Theme { get; set; }
        public MicrositeSeo? Seo { get; set; }

        public IFormFile? LogoFile { get; set; }
        public IFormFile? BannerFile { get; set; }
        public IFormFile? FaviconFile { get; set; }
        public string? ThemeJson { get; set; }
        public string? SeoJson { get; set; }
    }

    public class MicrositeTheme
    {
        public string? HeaderColor { get; set; }
        public string? TextColor { get; set; }
        public string? BackgroundColor { get; set; }
        public string? ButtonColor { get; set; }
        public string? ButtonTextColor { get; set; }
        public string? FooterColor { get; set; }
        public string? FooterTextColor { get; set; }
        public string? FontFamily { get; set; }
    }

    public class MicrositeSeo
    {
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? MetaKeywords { get; set; }
        public string? OgImage { get; set; }
    }

    public class AssignProductRequest
    {
        public long MicrositeId { get; set; }
        public long ProductId { get; set; }
    }

    public class AssignProductUpdateRequest
    {
        public long MicrositeId { get; set; }
        public long ProductId { get; set; }
        public bool Status { get; set; } = true;
    }

    public class MicrositeOrderStatusUpdateRequest
    {
        public string Status { get; set; } = "Placed";
    }
}
