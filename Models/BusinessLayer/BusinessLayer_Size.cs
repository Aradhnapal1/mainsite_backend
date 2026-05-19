using Microsoft.AspNetCore.Mvc;


namespace firstproject.Models.BusinessLayer

{
    public partial interface IBusinessLayer
    {
        Task<List<Size>> GetSize();
        //Task<IActionResult> AddSize([FormBody])
            Task<IActionResult> AddSize([FromForm] Size size);
        Task<IActionResult> EditSize(int id, [FromForm] Size size);
        Task<IActionResult> DeleteSize(int id);
    }

    public partial class BusinessLayer : IBusinessLayer
    {
        public async Task<List<Size>> GetSize()
        {
            var result = await _databaseLayer.GetSize();
            return result;
        }

        public async Task<IActionResult> AddSize([FromForm] Size size)
        {
            var result = await _databaseLayer.AddSize(size);
            return result;
        }

        public async Task<IActionResult> EditSize(int id, [FromForm] Size size)
        {
            var result = await _databaseLayer.EditSize(id, size);
            return result;
        }

        public async Task<IActionResult> DeleteSize(int id)
        {
            var result = await _databaseLayer.DeleteSize(id);
            return result;
        }
    }
}
