using firstproject.Models;
using firstproject.Models.DatabaseLayer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace firstproject.Models.BusinessLayer
{
    public partial interface IBusinessLayer
    {
        //========================== MicroSite =================================== 
        Task<List<MicrositeModel>> GetMicrosite();
        Task<MicrositeModel> GetMicrositeById(long id);
        Task<MicrositeModel> CreateMicrosite(MicrositeModel model);
        Task<string> UpdateMicrosite(long id, MicrositeModel model);
        Task<string> DeleteMicrosite(long id);
        ////================================================================Assign Products MicroSites Task Start ===============================================================
        Task<bool> AssignProduct(long micrositeId, long productId);
        Task<List<object>> GetAssignedProducts();
        Task<bool> UpdateAssignedProduct(long id, long micrositeId, long productId, bool status);
        Task<bool> DeleteAssignedProduct(long id);
        Task<bool> UpdateMicrositeOrderStatus(long micrositeId, int orderId, string status);
        Task<bool> DeleteMicrositeOrder(long micrositeId, int orderId);
    }
    public partial class BusinessLayer
    {
        //========================== MicroSite =================================== 
        public async Task<List<MicrositeModel>> GetMicrosite()
        {
            return await _databaseLayer.GetMicrosite();
        }
        public async Task<MicrositeModel> GetMicrositeById(long id)
        {
            return await _databaseLayer.GetMicrositeById(id);
        }
        public async Task<MicrositeModel> CreateMicrosite(MicrositeModel model)
        {
            return await _databaseLayer.CreateMicrosite(model);
        }
        public async Task<string> UpdateMicrosite(long id, MicrositeModel model)
        {
            return await _databaseLayer.UpdateMicrosite(id, model);
        }
        public async Task<string> DeleteMicrosite(long id)
        {
            return await _databaseLayer.DeleteMicrosite(id);
        }

        ////================================================================ ASSIGN PRODUCT ===============================================================
        public async Task<bool> AssignProduct(long micrositeId, long productId)
        {
            return await _databaseLayer.AssignProduct(micrositeId, productId);
        }
        public async Task<List<object>> GetAssignedProducts()
        {
            return await _databaseLayer.GetAssignedProducts();
        }
        public async Task<bool> UpdateAssignedProduct(long id, long micrositeId, long productId, bool status)
        {
            return await _databaseLayer.UpdateAssignedProduct(id, micrositeId, productId, status);
        }
        public async Task<bool> DeleteAssignedProduct(long id)
        {
            return await _databaseLayer.DeleteAssignedProduct(id);
        }
        public async Task<bool> UpdateMicrositeOrderStatus(long micrositeId, int orderId, string status)
        {
            return await _databaseLayer.UpdateMicrositeOrderStatus(micrositeId, orderId, status);
        }
        public async Task<bool> DeleteMicrositeOrder(long micrositeId, int orderId)
        {
            return await _databaseLayer.DeleteMicrositeOrder(micrositeId, orderId);
        }
    }
}