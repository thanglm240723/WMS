using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using warehouseManagement.DTOs.StockTransferRequests;
using warehouseManagement.Services;

namespace warehouseManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CrossWarehouseTransferController : ControllerBase
    {
        private readonly ICrossWarehouseTransferService _service;

        public CrossWarehouseTransferController(ICrossWarehouseTransferService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost]
        [Authorize(Roles = "MANAGE")]
        public async Task<IActionResult> Create([FromBody] StockTransferCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value;
            if (!int.TryParse(userIdStr, out int currentUserId) || currentUserId == 0)
                return Unauthorized("Không xác định được người dùng.");

            try
            {
                var (transferNo, transferId) = await _service.CreateAsync(currentUserId, dto);
                return Ok(new { Message = "Tạo yêu cầu chuyển kho thành công", TransferNo = transferNo, TransferId = transferId });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id}/approval")]
        [Authorize(Roles = "MANAGE")]
        public async Task<IActionResult> ApproveOrReject(int id, [FromBody] StockTransferApproveDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int currentUserId))
                return Unauthorized("Không xác định được người dùng.");

            try
            {
                var (transferNo, newStatus) = await _service.ApproveOrRejectAsync(id, currentUserId, dto);
                return Ok(new { Message = $"Yêu cầu {transferNo} đã {newStatus}.", NewStatus = newStatus });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Lỗi: " + ex.Message);
            }
        }

        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "MANAGE")]
        public async Task<IActionResult> Cancel(int id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int currentUserId))
                return Unauthorized("Không xác định được người dùng.");

            try
            {
                await _service.CancelAsync(id, currentUserId);
                return Ok(new { Message = "Yêu cầu đã được hủy.", NewStatus = "Cancelled" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id}/ship")]
        [Authorize(Roles = "STAFF")]
        public async Task<IActionResult> ShipGoods(int id, [FromBody] StockTransferShipDto dto)
        {
            return StatusCode(501, "Chức năng chưa được triển khai.");
        }

        [HttpPost("{id}/receive")]
        [Authorize(Roles = "STAFF")]
        public async Task<IActionResult> ReceiveGoods(int id, [FromBody] StockTransferReceiveDto dto)
        {
            return StatusCode(501, "Chức năng chưa được triển khai.");
        }
    }
}
