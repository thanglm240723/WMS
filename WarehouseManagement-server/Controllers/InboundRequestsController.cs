using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using warehouseManagement.DTOs;
using warehouseManagement.Models;
using WarehouseManagement.DTOs.InboundRequests;

[Authorize]
[ApiController]
[Route("api/inbound-requests")]
public class InboundRequestsController : ControllerBase
{
    private readonly WmsContext _context;
    private readonly IMapper _mapper;

    public InboundRequestsController(WmsContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    // ──────────────────────────────────────────────────────────────
    // PURCHASE
    // ──────────────────────────────────────────────────────────────

    /// <summary>GET /api/inbound-requests/my</summary>
    [HttpGet("my")]
    [Authorize(Roles = "PURCHASE")]
    public async Task<ActionResult<IEnumerable<InboundRequestViewDto>>> GetMyRequests()
    {
        var userId = GetCurrentUserId();

        var requests = await _context.InboundRequests
            .AsNoTracking()
            .Where(r => r.CreatedBy == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(_mapper.Map<List<InboundRequestViewDto>>(requests));
    }

    /// <summary>GET /api/inbound-requests/{id}</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "PURCHASE,MANAGE,STAFF")]
    public async Task<IActionResult> GetById(int id)
    {
        var request = await _context.InboundRequests
            .AsNoTracking()
            .Include(r => r.InboundItems).ThenInclude(i => i.Unit)
            .Include(r => r.InboundItems).ThenInclude(i => i.Product).ThenInclude(p => p.BaseUnit)
            .Include(r => r.InboundItems).ThenInclude(i => i.Product).ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null) return NotFound();

        // PURCHASE may only view their own requests
        if (User.IsInRole("PURCHASE") && request.CreatedBy != GetCurrentUserId())
            return Forbid();

        var lastLog = await _context.ApprovalLogs
            .Where(l => l.Approval!.RefType == "InboundRequest" && l.Approval.RefId == id)
            .OrderByDescending(l => l.ActionAt)
            .Select(l => new { l.Action, l.Comment, l.ActionAt })
            .FirstOrDefaultAsync();

        return Ok(new
        {
            Data = _mapper.Map<InboundRequestViewDto>(request),
            History = lastLog
        });
    }

    /// <summary>POST /api/inbound-requests</summary>
    [HttpPost]  
    [Authorize(Roles = "PURCHASE")]
    public async Task<IActionResult> Create([FromBody] InboundRequestCreateDto dto)
    {
        var userId = GetCurrentUserId();

        using var tx = await _context.Database.BeginTransactionAsync();

        var request = _mapper.Map<InboundRequest>(dto);
        request.CreatedBy = userId;
        request.CreatedAt = DateTime.UtcNow;
        request.Status = "Pending";
        request.RequestNo = GenerateRequestNo();

        _context.InboundRequests.Add(request);
        await _context.SaveChangesAsync();

        if (dto.Items.Count > 0)
            _context.InboundItems.AddRange(MapItems(dto.Items, request.Id));

        _context.Approvals.Add(new Approval
        {
            RefType = "InboundRequest",
            RefId = request.Id,
            Status = "Pending",
            CurrentStep = 1
        });

        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new { request.Id, request.RequestNo });
    }

    /// <summary>PUT /api/inbound-requests/{id}</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "PURCHASE")]
    public async Task<IActionResult> Update(int id, [FromBody] InboundRequestCreateDto dto)
    {
        var userId = GetCurrentUserId();

        var request = await _context.InboundRequests
            .Include(r => r.InboundItems)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null) return NotFound();
        if (request.CreatedBy != userId) return Forbid();
        if (request.Status is "Approved" or "Completed")
            return BadRequest(new { message = "Không thể sửa đơn đã duyệt hoặc hoàn thành." });

        _context.InboundItems.RemoveRange(request.InboundItems);

        request.SupplierName = dto.SupplierName;
        request.Note = dto.Note;
        request.WarehouseId = dto.WarehouseId;
        request.Status = "Pending";

        if (dto.Items.Count > 0)
            _context.InboundItems.AddRange(MapItems(dto.Items, request.Id));

        await _context.SaveChangesAsync();
        return Ok();
    }

    /// <summary>DELETE /api/inbound-requests/{id}</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "PURCHASE")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();

        var request = await _context.InboundRequests
            .Include(r => r.InboundItems)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null) return NotFound();
        if (request.CreatedBy != userId) return Forbid();
        if (request.Status is "Approved" or "Completed")
            return BadRequest(new { message = "Không thể xóa đơn đã duyệt hoặc hoàn thành." });

        using var tx = await _context.Database.BeginTransactionAsync();

        var approval = await _context.Approvals
            .Include(a => a.ApprovalLogs)
            .FirstOrDefaultAsync(a => a.RefType == "InboundRequest" && a.RefId == id);

        if (approval != null)
        {
            _context.ApprovalLogs.RemoveRange(approval.ApprovalLogs);
            _context.Approvals.Remove(approval);
        }

        _context.InboundItems.RemoveRange(request.InboundItems);
        _context.InboundRequests.Remove(request);

        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok();
    }

    // ──────────────────────────────────────────────────────────────
    // MANAGE / STAFF
    // ──────────────────────────────────────────────────────────────

    /// <summary>POST /api/inbound-requests/{id}/approval</summary>
    [HttpPost("{id:int}/approval")]
    [Authorize(Roles = "MANAGE,STAFF")]
    public async Task<IActionResult> ApproveOrReject(int id, [FromBody] ApproveInboundRequestDTO dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (dto.Action != "Approve" && dto.Action != "Reject")
            return BadRequest(new { message = "Action phải là 'Approve' hoặc 'Reject'." });

        var request = await _context.InboundRequests.FirstOrDefaultAsync(r => r.Id == id);
        if (request == null) return NotFound(new { message = "Không tìm thấy đơn nhập kho." });

        if (request.Status != "Pending")
            return BadRequest(new { message = "Chỉ có thể duyệt/từ chối đơn ở trạng thái Pending." });

        var userId = GetCurrentUserId();
        var approval = await FindOrCreateApprovalAsync(id);

        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            request.Status = dto.Action == "Approve" ? "Approved" : "Rejected";
            request.ApprovedBy = userId;
            request.ApprovedAt = DateTime.UtcNow;
            approval.Status = request.Status;

            _context.ApprovalLogs.Add(new ApprovalLog
            {
                ApprovalId = approval.Id,
                Action = request.Status,
                ActionBy = userId,
                ActionAt = DateTime.UtcNow,
                Comment = dto.Comment ?? dto.RejectReason
            });

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new
            {
                message = $"Đơn {request.RequestNo} đã {dto.Action.ToLower()} thành công.",
                newStatus = request.Status
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, new { message = "Lỗi khi xử lý duyệt đơn.", detail = ex.Message });
        }
    }

    /// <summary>POST /api/inbound-requests/{id}/receive</summary>
    [HttpPost("{id:int}/receive")]
    [Authorize(Roles = "MANAGE,STAFF")]
    public async Task<IActionResult> ReceiveGoods(int id, [FromBody] ReceiveInboundRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var request = await _context.InboundRequests
            .Include(r => r.InboundItems)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null) return NotFound(new { message = "Không tìm thấy đơn nhập kho." });
        if (request.Status != "Approved")
            return BadRequest(new { message = "Chỉ nhận hàng cho đơn đã được duyệt." });

        // Validate all submitted item IDs belong to this request
        var validItemIds = request.InboundItems.Select(i => i.Id).ToHashSet();
        var invalidIds = dto.Items
            .Where(i => !validItemIds.Contains(i.InboundItemId))
            .Select(i => i.InboundItemId)
            .ToList();

        if (invalidIds.Any())
            return BadRequest(new { message = $"InboundItemId không hợp lệ: {string.Join(", ", invalidIds)}" });

        // Validate bin total == received quantity for each item
        foreach (var ri in dto.Items)
        {
            if (ri.BinQuantities == null || ri.BinQuantities.Count == 0)
                return BadRequest(new { message = $"InboundItemId {ri.InboundItemId}: cần ít nhất 1 bin." });

            var totalBins = ri.BinQuantities.Sum(b => b.Quantity);
            if (totalBins != ri.TotalReceivedQuantity)
                return BadRequest(new
                {
                    message = $"InboundItemId {ri.InboundItemId}: " +
                              $"tổng SL bins ({totalBins}) phải bằng SL thực nhận ({ri.TotalReceivedQuantity})."
                });
        }

        // Batch-load products and unit conversions
        var productIds = request.InboundItems.Select(i => i.ProductId).Distinct().ToList();

        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var conversions = await _context.UnitConversions
            .Where(c => productIds.Contains(c.ProductId) && c.IsActive)
            .ToListAsync();

        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var ri in dto.Items)
            {
                var item = request.InboundItems.First(i => i.Id == ri.InboundItemId);
                var product = products[item.ProductId];

                var conversionFactor = ResolveConversionFactor(item, product, conversions, out var conversionError);
                if (conversionError != null)
                    return BadRequest(new { message = conversionError });

                item.ReceivedQuantity = ri.TotalReceivedQuantity;
                item.BinId = ri.BinQuantities.First().BinId;
                if (ri.LineNote != null) item.LineNote = ri.LineNote;

                foreach (var binQty in ri.BinQuantities)
                {
                    var baseQty = binQty.Quantity * conversionFactor;

                    var inventory = await _context.Inventories.FirstOrDefaultAsync(inv =>
                        inv.ProductId == item.ProductId &&
                        inv.WarehouseId == request.WarehouseId &&
                        inv.BinId == binQty.BinId);

                    if (inventory != null)
                    {
                        inventory.Quantity += baseQty;
                        inventory.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        _context.Inventories.Add(new Inventory
                        {
                            ProductId = item.ProductId,
                            WarehouseId = request.WarehouseId,
                            BinId = binQty.BinId,
                            Quantity = baseQty,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            request.Status = "Completed";
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new
            {
                message = $"Nhận hàng cho đơn {request.RequestNo} thành công.",
                newStatus = request.Status
            });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return StatusCode(500, new { message = "Lỗi khi xử lý nhận hàng.", detail = ex.Message });
        }
    }

    // ──────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────

    private int GetCurrentUserId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    private static string GenerateRequestNo() =>
        $"IN-{DateTime.UtcNow.Ticks.ToString()[^6..]}";

    private static IEnumerable<InboundItem> MapItems(
        IEnumerable<InboundRequestItemCreateDto> items, int requestId) =>
        items.Select(i => new InboundItem
        {
            InboundRequestId = requestId,
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitId = i.UnitId,
            LineNote = i.LineNote
        });

    private async Task<Approval> FindOrCreateApprovalAsync(int refId)
    {
        var approval = await _context.Approvals
            .FirstOrDefaultAsync(a => a.RefType == "InboundRequest" && a.RefId == refId);

        if (approval == null)
        {
            approval = new Approval
            {
                RefType = "InboundRequest",
                RefId = refId,
                Status = "Pending",
                CurrentStep = 1
            };
            _context.Approvals.Add(approval);
            await _context.SaveChangesAsync();
        }

        return approval;
    }

    /// <summary>
    /// Returns the conversion factor from item unit → base unit.
    /// Returns 1 if units match. Sets error message on missing conversion.
    /// </summary>
    private static decimal ResolveConversionFactor(
        InboundItem item,
        Product product,
        List<UnitConversion> conversions,
        out string? error)
    {
        error = null;

        if (item.UnitId == product.BaseUnitId)
            return 1m;

        var conversion = conversions.FirstOrDefault(c =>
            c.ProductId == item.ProductId && c.FromUnitId == item.UnitId);

        if (conversion == null)
        {
            error = $"Không tìm thấy quy đổi đơn vị cho Product {item.ProductId}.";
            return 0m;
        }

        return conversion.ConversionFactor;
    }
}
