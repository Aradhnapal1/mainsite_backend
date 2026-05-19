using Microsoft.AspNetCore.Mvc;

namespace firstproject.Models.BusinessLayer
{
   public partial interface IBusinessLayer
    {
        Task<List<Colormodel>> GetColor();
        Task<IActionResult> AddColor([FromForm] Colormodel color);
        Task<IActionResult> EditColor(int id, [FromForm] Colormodel color);
        Task<IActionResult> DeleteColor(int id);


    }
    public partial class BusinessLayer : IBusinessLayer
    {
        public async Task<List<Colormodel>> GetColor()
        {
            var result = await _databaseLayer.GetColor();
            return result;
        }

        public async Task<IActionResult> AddColor([FromForm] Colormodel color)
        {
            var result = await _databaseLayer.AddColor(color);
            return result;
        }

        public async Task<IActionResult> EditColor(int id, [FromForm] Colormodel color)
        {
            var result = await _databaseLayer.EditColor(id, color);
            return result;
        }

        public async Task<IActionResult> DeleteColor(int id)
        {
            var result = await _databaseLayer.DeleteColor(id);
            return result;
        }

    }
}
