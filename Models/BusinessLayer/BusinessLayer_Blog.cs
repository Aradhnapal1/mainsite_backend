using Microsoft.AspNetCore.Mvc;

namespace firstproject.Models.BusinessLayer
{
  public partial interface IBusinessLayer
    {
        Task<List<Blogmodel>> GetAllBlogs();
        Task<Blogmodel> AddBlog([FromForm] Blogmodel model);
        Task<Blogmodel> EditBlog(int id, [FromForm] Blogmodel model);
        Task<Blogmodel> DeleteBlog(int id);

        Task<Blogmodel> GetBlogById(int id);
    }
    public partial class BusinessLayer : IBusinessLayer
    {
        public async Task<List<Blogmodel>> GetAllBlogs()
        {
            return await _databaseLayer.GetAllBlogs();
        }
        public async Task<Blogmodel> AddBlog([FromForm] Blogmodel model)
        {
            var result = await _databaseLayer.AddBlog(model);
            return result;
        }
        public async Task<Blogmodel> EditBlog(int id, [FromForm] Blogmodel model)
        {
            var result = await _databaseLayer.EditBlog(id, model);
            return result;
        }
        public async Task<Blogmodel> DeleteBlog(int id)
        {
            var result = await _databaseLayer.DeleteBlog(id);
            return result;
        }
        public async Task<Blogmodel> GetBlogById(int id)
        {
            var result = await _databaseLayer.GetBlogById(id);
            return result;
        }
    }
}
