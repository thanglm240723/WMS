using warehouseManagement.DTOs.StockTransferRequests;

namespace warehouseManagement.Services
{
    public interface ICrossWarehouseTransferService
    {
        Task<List<StockTransferViewDto>> GetAllAsync();
        Task<StockTransferViewDto> GetByIdAsync(int id);
        Task<(string transferNo, int transferId)> CreateAsync(int userId, StockTransferCreateDto dto);
        Task<(string transferNo, string newStatus)> ApproveOrRejectAsync(int id, int userId, StockTransferApproveDto dto);
        Task CancelAsync(int id, int userId);
    }
}
