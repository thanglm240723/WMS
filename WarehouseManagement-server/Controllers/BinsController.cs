using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using warehouseManagement.DTOs;
using warehouseManagement.Services;

namespace warehouseManagement.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BinsController : ControllerBase
    {
        private readonly IBinService _binService;

        public BinsController(IBinService binService)
        {
            _binService = binService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? warehouseId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _binService.GetAll(userId, warehouseId);
            return Ok(result);
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailable([FromQuery] int warehouseId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _binService.GetAvailable(userId, warehouseId);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BinCreateDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _binService.Create(userId, dto);
            return CreatedAtAction(nameof(GetAll), result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] BinUpdateDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _binService.Update(userId, id, dto);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _binService.Delete(userId, id);
            return NoContent();
        }
    }
}