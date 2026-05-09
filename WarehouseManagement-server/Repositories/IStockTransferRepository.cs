using Microsoft.EntityFrameworkCore.Storage;
using warehouseManagement.Models;

namespace warehouseManagement.Repositories
{
    public interface IStockTransferRepository
    {
        Task<List<StockTransferRequest>> GetAllCrossWarehouseAsync();
        Task<StockTransferRequest?> GetByIdAsync(int id);
        Task<StockTransferRequest?> GetByIdForUpdateAsync(int id);
        Task<string?> GetLastTransferNoAsync(string prefix);
        Task<Product?> FindProductAsync(int productId);
        Task AddAsync(StockTransferRequest request);
        void AddItem(StockTransferItem item);
        void AddApprovalLog(ApprovalLog log);
        Task SaveChangesAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}
