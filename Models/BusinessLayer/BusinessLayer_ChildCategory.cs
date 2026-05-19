namespace firstproject.Models.BusinessLayer
{
    public partial interface IBusinessLayer
    {
        Task<List<childCategoryModel>> GetAllChildCategory();
        Task<childCategoryModel> Add(childCategoryModel model);
        Task<childCategoryModel> Edit(int id, childCategoryModel model);
        Task<childCategoryModel?> GetChildCategoryById(int id);
        Task<bool> DeleteChildCategory(int id);

    }

    public partial class  BusinessLayer : IBusinessLayer
    {
        public async Task<List<childCategoryModel>> GetAllChildCategory()
        {
            return await _databaseLayer.GetAllChildCategory();    
        }

        public async Task<childCategoryModel> Add(childCategoryModel model)
        {
            return await _databaseLayer.Add(model);
        }

        public async Task<childCategoryModel> Edit(int id, childCategoryModel model)
        {
            return await _databaseLayer.Edit(id, model);
        }

        public async Task<childCategoryModel?> GetChildCategoryById(int id)
        {
            return await _databaseLayer.GetChildCategoryById(id);
        }   

        public async Task<bool> DeleteChildCategory(int id)
        {
            return await _databaseLayer.DeleteChildCategory(id);
        }
    }
}
