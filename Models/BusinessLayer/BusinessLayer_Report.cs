using Microsoft.AspNetCore.Mvc;

namespace firstproject.Models.BusinessLayer
{
    public partial interface IBusinessLayer
    {
        Task<object> GetReport();
    }

    public partial class BusinessLayer : IBusinessLayer
    {
        public async Task<object> GetReport()
        {
            return await _databaseLayer.GetReport();
        }
    }

}
