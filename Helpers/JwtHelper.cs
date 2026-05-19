using firstproject.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace firstproject.Helpers
{
    public class JwtHelper
    {
        private readonly IConfiguration _configuration;

        public JwtHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ✅ Admin ke liye token
        public string GenerateToken(AdminModel admin)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                new Claim(ClaimTypes.Email, admin.Email),
                new Claim(ClaimTypes.GivenName, admin.FirstName),
                new Claim(ClaimTypes.Surname, admin.LastName),
                new Claim(ClaimTypes.Role, "Admin")
            };

            return BuildToken(claims);
        }

        // ✅ User ke liye token
        public string GenerateToken(Usermodel user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.GivenName, user.Firstname ?? string.Empty),
                new Claim(ClaimTypes.Surname, user.Lastname ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role ?? "User")
            };

            return BuildToken(claims);
        }

        // ✅ Common token build logic
        private string BuildToken(Claim[] claims)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"]);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // ✅ Token se userId nikalo — Cart aur other controllers ke liye
        public int? GetUserIdFromToken(string token)
        {
            try
            {
                var jwtSettings = _configuration.GetSection("JwtSettings");
                var secretKey = jwtSettings["SecretKey"];

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(secretKey); // ✅ _configuration use karo

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;

                // ✅ ClaimTypes.NameIdentifier use karo — GenerateToken se match karta hai
                var userIdClaim = jwtToken.Claims
                    .First(c => c.Type == ClaimTypes.NameIdentifier);

                return int.Parse(userIdClaim.Value);
            }
            catch
            {
                return null; // ✅ Invalid/expired token → guest treat karo
            }
        }
    }
}