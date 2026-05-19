using Microsoft.AspNetCore.Http;

namespace firstproject.S3Services
{
    public static class S3StorageHelper
    {
        public static async Task<string> UploadFileAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                return "";

            var safeFolder = folder.Replace("/", Path.DirectorySeparatorChar.ToString());
            var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", safeFolder);
            Directory.CreateDirectory(root);

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(root, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            var publicFolder = folder.Trim('/').Replace("\\", "/");
            return $"/{publicFolder}/{fileName}";
        }

        public static Task DeleteByPathAsync(string? filePath)
        {
            TryDeleteLocalFile(filePath);
            return Task.CompletedTask;
        }

        public static Task DeleteStoredMediaAsync(string? filePath)
        {
            TryDeleteLocalFile(filePath);
            return Task.CompletedTask;
        }

        private static void TryDeleteLocalFile(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            var normalized = filePath.Replace("/", Path.DirectorySeparatorChar.ToString()).TrimStart(Path.DirectorySeparatorChar);
            var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", normalized);

            if (File.Exists(absolutePath))
                File.Delete(absolutePath);
        }
    }
}
