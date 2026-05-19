using Microsoft.AspNetCore.Mvc;

namespace firstproject.Models.BusinessLayer
{
    public partial interface IBusinessLayer
    {
        Task<List<customermodel>> GetCustomerLogo();
        Task<customermodel> Add(customermodel model);
        Task<customermodel> Edit(int id, [FromForm] customermodel model);
        Task<customermodel> DeleteCustomerLogo(int id);
        Task<customermodel> GetCustomerLogoById(int id);
    }

    public partial class BusinessLayer : IBusinessLayer
    {
        public async Task<List<customermodel>> GetCustomerLogo()
        {
            var result = await _databaseLayer.GetCustomerLogo();
            return result;
        }

        public async Task<customermodel> Add(customermodel      model)
        {
            return await _databaseLayer.Add(model);
        }


        public async Task<customermodel> Edit(int id, [FromForm] customermodel model)
        {
            return await _databaseLayer.Edit(id, model);
        }   

        public async Task<customermodel> GetCustomerLogoById(int id)
        {
            return await _databaseLayer.GetCustomerLogoById(id);
        }

        public async Task<customermodel> DeleteCustomerLogo(int id)
        {
            // Step 1: Pehle image path fetch karo
            var customerLogos = await _databaseLayer.GetCustomerLogo();
            var customerLogo = customerLogos.FirstOrDefault(b => b.id == id);
            // Step 2: Database se delete karo
            var result = await _databaseLayer.DeleteCustomerLogo(id);

            // Step 3: Agar delete successful hua to image bhi delete karo
            if (result is OkResult && customerLogo?.customerimage != null)
            {
                var imagePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    customerLogo.customerimage.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)
                );

                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                }
            }

            return result;
        }
    }
}