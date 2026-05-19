using Microsoft.AspNetCore.Mvc;

namespace firstproject.Models.BusinessLayer
{
    public partial interface IBusinessLayer
    {
        Task<List<Variantmodel>> GetVariant();


        Task<IActionResult> AddVariant([FromForm] Variantmodel variant);
        Task<IActionResult> UpdateVariant(int id, [FromForm] Variantmodel variant);
        Task<Variantmodel> GetVariantById(int id);
        Task<IActionResult> DeleteVariant(int id);
    }

    public partial class BusinessLayer : IBusinessLayer
    {
        public async Task<List<Variantmodel>> GetVariant()
        {
            var result = await _databaseLayer.GetVariant();
            return result;
        }



        public async Task<IActionResult> AddVariant([FromForm] Variantmodel variant)
        {
            var result = await _databaseLayer.AddVariant(variant);
            return result;
        }

        public async Task<IActionResult> UpdateVariant(int id, [FromForm] Variantmodel variant)
        {
            var result = await _databaseLayer.UpdateVariant(id, variant);
            return result;
        }
        public async Task<Variantmodel> GetVariantById(int id)
        {
            var result = await _databaseLayer.GetVariantById(id);
            return result;
        }

        public async Task<IActionResult> DeleteVariant(int id)
        {
            var result = await _databaseLayer.DeleteVariant(id);
            return result;
        }
    }
}
