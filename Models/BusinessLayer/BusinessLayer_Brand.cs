using firstproject.Models.DatabaseLayer;
using Microsoft.AspNetCore.Mvc;

namespace firstproject.Models.BusinessLayer
{
    public partial interface IBusinessLayer
    {
        Task<List<Brandmodel>> GetBrand();
        Task<Brandmodel> Add(Brandmodel model);
        Task<bool> Edit(int id, Brandmodel model);
        Task<bool> DeleteBrand(int id);
        Task<Brandmodel> GetBrandById(int id);
        Task UpdateBrandImage(int id, Brandmodel model);


    }

    public partial class BusinessLayer : IBusinessLayer
    {

        public async Task UpdateBrandImage(int id, Brandmodel model)
        {
            await _databaseLayer.UpdateBrandImage(id, model);
        }


        public async Task<List<Brandmodel>> GetBrand()
        {
            return await _databaseLayer.GetBrand();
        }

        public async Task<Brandmodel> Add(Brandmodel model)
        {
            return await _databaseLayer.Add(model);
        }

        public async Task<bool> Edit(int id, Brandmodel model)
        {
            return await _databaseLayer.Edit(id, model);
        }

        public async Task<Brandmodel> GetBrandById(int id)
        {
            return await _databaseLayer.GetBrandById(id);
        }

        public async Task<bool> DeleteBrand(int id)
        {
            return await _databaseLayer.DeleteBrand(id);
        }
    }
}