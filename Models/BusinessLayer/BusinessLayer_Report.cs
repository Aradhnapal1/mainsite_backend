using Microsoft.AspNetCore.Mvc;

namespace firstproject.Models.BusinessLayer
{
    public partial interface IBusinessLayer
    {
        Task<int> GetReport();
    }

    public partial class BusinessLayer : IBusinessLayer
    {
        public async Task<int> GetReport()
        {
            return await _databaseLayer.GetReport();
        }
    }

}
