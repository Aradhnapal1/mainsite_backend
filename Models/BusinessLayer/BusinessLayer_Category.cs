using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;

namespace firstproject.Models.BusinessLayer
{
    public partial interface IBusinessLayer
    {
        Task<List<categoryModel>> GetAllCategory();
        Task<categoryModel> Add(categoryModel model);
        Task<categoryModel> Edit(int id, categoryModel model);
        Task<bool> DeleteCategory(int id);
        Task<categoryModel?> GetCategoryById(int id);
    }

    public partial class BusinessLayer : IBusinessLayer
    {
        public async Task<List<categoryModel>> GetAllCategory()
        {
            return await _databaseLayer.GetAllCategory();
        }

        public async Task<categoryModel> Add(categoryModel model)
        {
            return await _databaseLayer.Add(model);
        }

        public async Task<categoryModel> Edit(int id, categoryModel model)
        {
            return await _databaseLayer.Edit(id, model);
        }

        public async Task<bool> DeleteCategory(int id)
        {
            return await _databaseLayer.DeleteCategory(id);
        }

        public async Task<categoryModel?> GetCategoryById(int id)
        {
            return await _databaseLayer.GetCategoryById(id);
        }
    }
}