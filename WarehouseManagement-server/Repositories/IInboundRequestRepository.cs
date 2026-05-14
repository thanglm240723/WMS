using Microsoft.EntityFrameworkCore.Storage;
using warehouseManagement.Models;
using WarehouseManagement.DTOs.InboundRequests;

namespace warehouseManagement.Repositories
{
    public interface IInboundRequestRepository
    {
        Task<List<InboundRequest>> GetByUserAsync(int userId);
        Task<InboundRequest?> GetByIdWithDetailsAsync(int id);
        Task<InboundRequest?> GetByIdWithItemsAsync(int id);
        Task<ApprovalLogSummaryDto?> GetLastApprovalLogAsync(int requestId);
        Task<Approval?> GetApprovalWithLogsAsync(int requestId);
        Task<Approval?> GetApprovalAsync(int requestId);
        Task<Dictionary<int, Product>> GetProductsByIdsAsync(List<int> productIds);
        Task<List<UnitConversion>> GetActiveConversionsAsync(List<int> productIds);
        Task<Inventory?> GetInventoryAsync(int productId, int warehouseId, int? binId);

        Task AddRequestAsync(InboundRequest request);
        void AddItemsRange(IEnumerable<InboundItem> items);
        void RemoveItemsRange(ICollection<InboundItem> items);
        void AddApproval(Approval approval);
        void AddApprovalLog(ApprovalLog log);
        void AddInventory(Inventory inventory);
        void RemoveApproval(Approval approval);
        void Remove(InboundRequest request);

        Task SaveChangesAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}
