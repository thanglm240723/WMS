using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Formats.Asn1;
using warehouseManagement.DTOs;
using warehouseManagement.Models;
using warehouseManagement.Services;

namespace warehouseManagement.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _service;
        public CategoryController(ICategoryService service)
        {
            _service = service;
        }

        [HttpGet]

        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _service.GetAllAsync();
            return Ok(categories);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
           var category = await _service.GetCategoryByIdAsync(id);
            return Ok(category);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryDTO dto)
        {
           
                var createdCategory = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = createdCategory.Id }, createdCategory);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDTO dto)
        {
            await _service.EditAsync(id, dto);
            return NoContent();
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.Delete(id);
            return NoContent();
        }
    }
}
