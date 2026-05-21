using firstproject.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace firstproject.Models.BusinessLayer
{
    public partial interface IBusinessLayer
    {
        Task<List<Usermodel>> GetUsers();
        Task<IActionResult> AddUser([FromForm] Usermodel model);
        Task<IActionResult> UpdateUser(int id, [FromForm] Usermodel model);
        Task<IActionResult> DeleteUser(int id);
        Task<Usermodel> GetUserByEmail(string email);
        Task<IActionResult> ForgotPassword([FromForm] string email);
    }

    public partial class BusinessLayer : IBusinessLayer
    {
        public async Task<List<Usermodel>> GetUsers()
        {
            return await _databaseLayer.GetUsers();
        }

        public async Task<IActionResult> AddUser([FromForm] Usermodel model)
        {
            
            model.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
           
            model.Createdat = DateTime.UtcNow;

            return await _databaseLayer.AddUser(model);
        }

        public async Task<IActionResult> UpdateUser(int id, [FromForm] Usermodel model)
        {
           
            if (!string.IsNullOrEmpty(model.Password))
            {
                model.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
            }

            return await _databaseLayer.UpdateUser(id, model);
        }

        public async Task<IActionResult> DeleteUser(int id)
        {
            return await _databaseLayer.DeleteUser(id);
        }

        public async Task<Usermodel> GetUserByEmail(string email)
        {
            return await _databaseLayer.GetUserByEmail(email);
        }

        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return new BadRequestObjectResult(new { status = false, message = "Email is required" });
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var user = await _databaseLayer.GetUserByEmail(normalizedEmail);
            if (user == null || user.Id <= 0)
            {
                return new NotFoundObjectResult(new { status = false, message = "User with this email not found" });
            }

            var otp = Random.Shared.Next(100000, 999999).ToString();
            var expiry = DateTime.UtcNow.AddMinutes(15);
            await _databaseLayer.SaveUserPasswordResetOtp(user.Id, otp, expiry);

            var displayName = string.IsNullOrWhiteSpace(user.Firstname)
                ? "User"
                : $"{user.Firstname} {user.Lastname}".Trim();

            var subject = "Forgot Password - OTP";
            var html = $@"<div style='font-family:Arial,sans-serif;max-width:520px;margin:auto'>
<h2 style='color:#1f2937'>Password Reset OTP</h2>
<p>Hi {displayName},</p>
<p>Your password reset OTP is:</p>
<p style='font-size:28px;font-weight:bold;letter-spacing:4px;color:#2563eb'>{otp}</p>
<p>This OTP is valid for <strong>15 minutes</strong>.</p>
<p style='color:#6b7280;font-size:12px'>If you did not request this, please ignore this email.</p>
</div>";
            var plain = $"Hi {displayName},\n\nYour password reset OTP is: {otp}\nValid for 15 minutes.\n";

            try
            {
                await SmtpEmailHelper.SendAsync(_configuration, normalizedEmail, subject, html, plain);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(new
                {
                    status = false,
                    message = "Failed to send forgot password email.",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }

            return new OkObjectResult(new
            {
                status = true,
                message = "Forgot password OTP sent to your email.",
                sentTo = normalizedEmail,
                expiryMinutes = 15
            });
        }

    }
}