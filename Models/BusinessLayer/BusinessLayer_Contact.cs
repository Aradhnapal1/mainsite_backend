using Microsoft.AspNetCore.Mvc;

namespace firstproject.Models.BusinessLayer
{
    public partial interface IBusinessLayer
    {
        Task<List<Contactmodel>> GetContact();

        Task<IActionResult> AddContact([FromForm] Contactmodel model);
        Task<IActionResult> UpdateContact(int id, [FromForm] Contactmodel model);
        Task<IActionResult> DeleteContact(int id);
    }

    public partial class BusinessLayer : IBusinessLayer
    {
        public async Task<List<Contactmodel>> GetContact()
        {
            var result = await _databaseLayer.GetContact();
            return result;
        }

       public async Task<IActionResult> AddContact([FromForm] Contactmodel model)
        {
            var result = await _databaseLayer.AddContact(model);
            return result;
        }
        
        public async Task<IActionResult> UpdateContact(int id, [FromForm] Contactmodel model)
        {
            var result = await _databaseLayer.UpdateContact(id, model);
            return result;
        }

        public async Task<IActionResult> DeleteContact(int id)
        {
            var result = await _databaseLayer.DeleteContact(id);
            return result;
        }
    }

}