using firstproject.Models;
using firstproject.Models.BusinessLayer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace firstproject.Controllers
{
    [ApiController]
   
    [Route("api/admin")]
    public class ColorController : ControllerBase
    {

        private readonly IBusinessLayer _businessLayer;

        public ColorController(IBusinessLayer businessLayer)
        {
            _businessLayer = businessLayer;
        }

        [HttpGet("getcolor")]
        public async Task<IActionResult> Get()
        {
            var result = await _businessLayer.GetColor();
            return Ok(result);
        }

        [HttpPost("addcolor")]
        [Authorize]
        public async Task<IActionResult> AddColor([FromForm] Colormodel color)
        {
            var result = await _businessLayer.AddColor(color);
            return result;
        }


        [HttpPut("editcolor/{id}")]
        [Authorize]
        public async Task<IActionResult> EditColor(int id, [FromForm] Colormodel color)
        {
            var result = await _businessLayer.EditColor(id, color);
            return result;
        }

        [HttpDelete("deletecolor/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteColor(int id)
        {
            var result = await _businessLayer.DeleteColor(id);
            return result;

        }
    }
}
