using firstproject.Helpers;
using firstproject.Models;
using firstproject.Models.BusinessLayer;
using Microsoft.AspNetCore.Mvc;

namespace firstproject.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        private readonly JwtHelper _jwtHelper;

        public AdminController(IBusinessLayer businessLayer, JwtHelper jwtHelper)
        {
            _businessLayer = businessLayer;
            _jwtHelper = jwtHelper;
        }

        // POST api/admin/login ok
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromForm] AdminLoginModel model)
        {
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Email aur Password dono required hain"
                });
            }

            var admin = await _businessLayer.GetAdminByEmail(model.Email);

            if (admin == null)
            {
                return Unauthorized(new
                {
                    status = false,
                    message = "Email ya Password galat hai"
                });
            }

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, admin.PasswordHash);

            if (!isPasswordValid)
            {
                return Unauthorized(new
                {
                    status = false,
                    message = "Email ya Password galat hai"
                });
            }

            // ✅ Token generate karo aur AdminModel ke Token field mein set karo
            admin.Token = _jwtHelper.GenerateToken(admin);
            admin.PasswordHash = null; // ✅ Password response mein mat bhejo

            return Ok(new
            {
                status = true,
                message = "Login successful",
                data = new
                {
                    //id = admin.Id,
                    //firstName = admin.FirstName,
                    //lastName = admin.LastName,
                    //phone = admin.Phone,
                    //email = admin.Email,
                    token = admin.Token,      // ✅ Sirf login mein token aayega
                  
                }
            });
        }

        // GET api/admin/get
        [HttpGet("get")]
        public async Task<IActionResult> Get()
        {
            var result = await _businessLayer.GetAllAdmins();

            // ✅ PasswordHash aur Token dono exclude karo
            var safeResult = result.Select(admin => new
            {
                id = admin.Id,
                firstName = admin.FirstName,
                lastName = admin.LastName,
                phone = admin.Phone,
                email = admin.Email,
                createdAt = admin.CreatedAt
                // PasswordHash ❌ nahi
                // Token ❌ nahi
            });

            return Ok(safeResult);
        }

        // POST api/admin/add
        [HttpPost("add")]
        public async Task<IActionResult> Add([FromForm] AdminModel model)
        {
            var result = await _businessLayer.Add(model);

            // ✅ Sirf safe fields return karo
            return Ok(new
            {
                status = true,
                message = "Record successfully added",
                data = new
                {
                    id = result.Id,
                    firstName = result.FirstName,
                    lastName = result.LastName,
                    phone = result.Phone,
                    email = result.Email,
                    createdAt = result.CreatedAt
                }
            });
        }

        // PUT api/admin/edit/{id}
        [HttpPut("edit/{id}")]
        public async Task<IActionResult> Edit(int id, [FromForm] AdminModel model)
        {
            var result = await _businessLayer.Edit(id, model);

            if (result == null)
                return NotFound(new { status = false, message = "Record not found" });

            // ✅ Sirf safe fields return karo
            return Ok(new
            {
                status = true,
                message = "Record updated successfully",
                data = new
                {
                    id = result.Id,
                    firstName = result.FirstName,
                    lastName = result.LastName,
                    phone = result.Phone,
                    email = result.Email,
                    createdAt = result.CreatedAt
                    // PasswordHash ❌ nahi
                    // Token ❌ nahi
                }
            });
        }

        // DELETE api/admin/delete/{id}
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _businessLayer.Delete(id);

            if (result == null)
                return NotFound(new { status = false, message = "Record not found" });

            return Ok(new
            {
                status = true,
                message = "Record deleted successfully"
            });
        }
    }
}