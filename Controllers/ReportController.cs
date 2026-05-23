using firstproject.Models.BusinessLayer;
using Microsoft.AspNetCore.Mvc;

namespace firstproject.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class ReportController : ControllerBase
    {
        private readonly IBusinessLayer _businessLayer;

        public ReportController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
        }

        [HttpGet("getreport")]
        public async Task<IActionResult> GetReport()
        {
            var result = await _businessLayer.GetReport();

            var response = new
            {
                status = true,
                message = "Report fetched successfully",
                data = result
            };

            return Ok(response);
        }
    }
}
