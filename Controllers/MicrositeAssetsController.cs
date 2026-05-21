using Microsoft.AspNetCore.Mvc;

namespace firstproject.Controllers
{
    [ApiController]
    public class MicrositeAssetsController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public MicrositeAssetsController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet("/microsites/{**assetPath}")]
        public IActionResult GetMicrositeAsset(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return NotFound();

            var safePath = assetPath.Replace("..", "").Replace("\\", "/").TrimStart('/');
            var fullPath = Path.Combine(_env.WebRootPath, "microsites", safePath);

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                ".ico" => "image/x-icon",
                _ => "application/octet-stream"
            };

            return PhysicalFile(fullPath, contentType);
        }
    }
}
