using firstproject.Helpers;
using firstproject.Models;
using firstproject.Models.BusinessLayer;
using Microsoft.AspNetCore.Mvc;

namespace firstproject.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;
        private readonly JwtHelper _jwtHelper;

        public UserController(IBusinessLayer businessLayer, JwtHelper jwtHelper)
        {
            _businessLayer = businessLayer;
            _jwtHelper = jwtHelper;
        }

        [HttpGet("getusers")]
        public async Task<IActionResult> GetUsers()
        {
            var result = await _businessLayer.GetUsers();
            //return Ok(result);
            var response = new
            {
                status = true,
                message = "Users fetched successfully",
                data = result.Select(u => new
                {
                    id = u.Id,
                    firstname = u.Firstname,
                    lastname = u.Lastname,
                    email = u.Email,
                    role = u.Role,
                    isactive = u.Isactive,
                    createdat = u.Createdat
                })
            };
            return Ok(response);
        }

        [HttpPost("adduser")]
        public async Task<IActionResult> AddUser([FromForm] Usermodel model)
        {
            var result = await _businessLayer.AddUser(model);
            return Ok(result);
        }

        [HttpPut("updateuser/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromForm] Usermodel model)
        {
            var result = await _businessLayer.UpdateUser(id, model);
            return Ok(result);
        }

        [HttpDelete("deleteuser/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var result = await _businessLayer.DeleteUser(id);
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromForm] UserLoginModel model)
        {
            // Step 1 - Empty check
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Email aur Password dono required hain"
                });
            }

            // Step 2 - User fetch karo email se
            var user = await _businessLayer.GetUserByEmail(model.Email);
            if (user == null)
            {
                return Unauthorized(new
                {
                    status = false,
                    message = "Email registered nahi hai"
                });
            }

            // Step 3 - Password null check
            if (string.IsNullOrEmpty(user.Password))
            {
                return Unauthorized(new
                {
                    status = false,
                    message = "Password database mein nahi hai"
                });
            }

            // Step 4 - BCrypt verify
            bool isPasswordValid = false;
            try
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.Password);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    status = false,
                    message = "Password format galat hai: " + ex.Message
                });
            }

            if (!isPasswordValid)
            {
                return Unauthorized(new
                {
                    status = false,
                    message = "Password galat hai"
                });
            }

            // Step 5 - Token generate karo
            user.Token = _jwtHelper.GenerateToken(user);
            user.Password = null;

            return Ok(new
            {
                status = true,
                message = "Login successful",
                data = new
                {
                    id = user.Id,
                    firstname = user.Firstname,
                    lastname = user.Lastname,
                    email = user.Email,
                    role = user.Role,
                    token = user.Token
                }
            });
        }
    }
}