using Microsoft.AspNetCore.Mvc;

namespace firstproject.Models.BusinessLayer
{
    public partial interface IBusinessLayer {
        Task<List<SubCategoryModel>> GetAllSubCategory();
        Task<SubCategoryModel> Add(SubCategoryModel model);
        Task<SubCategoryModel> Edit(int id, SubCategoryModel model);
        Task<SubCategoryModel?> GetSubCategoryById(int id);
        Task<bool> DeleteSubCategory(int id);
    }

    public partial class BusinessLayer : IBusinessLayer
    {
        public async Task<List<SubCategoryModel>>GetAllSubCategory()
        {
            return await _databaseLayer.GetAllSubCategory();
        }

        public async Task<SubCategoryModel> Add(SubCategoryModel model)
        {
            return await _databaseLayer.Add(model);
        }

        public async Task<SubCategoryModel> Edit(int id, SubCategoryModel model)
        {
            return await _databaseLayer.Edit(id, model);
        }

        public async Task<SubCategoryModel?> GetSubCategoryById(int id)
        {
            return await _databaseLayer.GetSubCategoryById(id);
        }

        public async Task<bool> DeleteSubCategory(int id)
        {
            return await _databaseLayer.DeleteSubCategory(id);
        }
    }

}
