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
    }
}