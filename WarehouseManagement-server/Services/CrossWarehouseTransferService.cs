using AutoMapper;
using warehouseManagement.DTOs.StockTransferRequests;
using warehouseManagement.Models;
using warehouseManagement.Repositories;

namespace warehouseManagement.Services
{
    public class CrossWarehouseTransferService : ICrossWarehouseTransferService
    {
        private readonly IStockTransferRepository _repo;
        private readonly IMapper _mapper;

        public CrossWarehouseTransferService(IStockTransferRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<List<StockTransferViewDto>> GetAllAsync()
        {
            var requests = await _repo.GetAllCrossWarehouseAsync();
            return _mapper.Map<List<StockTransferViewDto>>(requests);
        }

        public async Task<StockTransferViewDto> GetByIdAsync(int id)
        {
            var request = await _repo.GetByIdAsync(id);
            if (request == null)
                throw new KeyNotFoundException("Không tìm thấy yêu cầu chuyển kho");
            return _mapper.Map<StockTransferViewDto>(request);
        }

        public async Task<(string transferNo, int transferId)> CreateAsync(int userId, StockTransferCreateDto dto)
        {
            if (dto.FromWarehouseId == dto.ToWarehouseId)
                throw new InvalidOperationException("Kho nguồn và kho đích không được trùng nhau. Dùng /api/StockTransfer cho chuyển bin nội bộ.");

            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var prefix = $"CWT-{today}-";
            var lastNo = await _repo.GetLastTransferNoAsync(prefix);
            int seq = 1;
            if (lastNo != null)
            {
                var parts = lastNo.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int last)) seq = last + 1;
            }
            var transferNo = $"{prefix}{seq:D3}";

            var transfer = new StockTransferRequest
            {
                TransferNo = transferNo,
                FromWarehouseId = dto.FromWarehouseId,
                ToWarehouseId = dto.ToWarehouseId, 
                Status = "Pending",
                Note = dto.Note,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
            };
            await _repo.AddAsync(transfer);
            await _repo.SaveChangesAsync();

            foreach (var item in dto.Items)
            {
                var product = await _repo.FindProductAsync(item.ProductId);
                if (product == null)
                    throw new KeyNotFoundException($"Không tìm thấy sản phẩm Id={item.ProductId}");

                int resolvedUnitId = item.UnitId > 0 ? item.UnitId : product.BaseUnitId;

                _repo.AddItem(new StockTransferItem
                {
                    StockTransferRequestId = transfer.Id,
                    ProductId = item.ProductId,
                    UnitId = resolvedUnitId,
                    Quantity = item.Quantity,
                    LineNote = item.LineNote,
                });
            }
            await _repo.SaveChangesAsync();

            return (transferNo, transfer.Id);
        }

        public async Task<(string transferNo, string newStatus)> ApproveOrRejectAsync(int id, int userId, StockTransferApproveDto dto)
        {
            if (dto.Action != "Approve" && dto.Action != "Reject")
                throw new InvalidOperationException("Action phải là 'Approve' hoặc 'Reject'");

            var request = await _repo.GetByIdForUpdateAsync(id);
            if (request == null)
                throw new KeyNotFoundException("Không tìm thấy yêu cầu chuyển kho");

            if (request.Status != "Pending")
                throw new InvalidOperationException("Chỉ có thể duyệt/từ chối yêu cầu ở trạng thái Pending");

            await using var transaction = await _repo.BeginTransactionAsync();
            try
            {
                request.Status = dto.Action == "Approve" ? "Approved" : "Rejected";
                if (dto.Action == "Approve") request.ApprovedBy = userId;

                _repo.AddApprovalLog(new ApprovalLog
                {
                    Action = request.Status,
                    ActionBy = userId,
                    ActionAt = DateTime.UtcNow,
                    Comment = dto.Comment ?? dto.RejectReason
                });

                await _repo.SaveChangesAsync();
                await transaction.CommitAsync();

                return (request.TransferNo, request.Status);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task CancelAsync(int id, int userId)
        {
            var request = await _repo.GetByIdForUpdateAsync(id);
            if (request == null)
                throw new KeyNotFoundException("Không tìm thấy yêu cầu chuyển kho");

            if (request.Status != "Pending")
                throw new InvalidOperationException("Chỉ có thể hủy yêu cầu ở trạng thái Pending");

            await using var transaction = await _repo.BeginTransactionAsync();
            try
            {
                request.Status = "Cancelled";

                _repo.AddApprovalLog(new ApprovalLog
                {
                    Action = "Cancelled",
                    ActionBy = userId,
                    ActionAt = DateTime.UtcNow,
                });

                await _repo.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
