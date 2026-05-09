using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using warehouseManagement.Models;

namespace warehouseManagement.Repositories
{
    public class StockTransferRepository : IStockTransferRepository
    {
        private readonly WmsContext _context;

        public StockTransferRepository(WmsContext context)
        {
            _context = context;
        }

        public async Task<List<StockTransferRequest>> GetAllCrossWarehouseAsync()
            => await _context.StockTransferRequests
                .Include(r => r.StockTransferItems).ThenInclude(i => i.Product)
                .Include(r => r.StockTransferItems).ThenInclude(i => i.Unit)
                .Include(r => r.CreatedByNavigation)
                .Include(r => r.ApprovedByNavigation)
                .Include(r => r.FromWarehouse)
                .Include(r => r.ToWarehouse)
                .Where(r => r.FromWarehouseId != r.ToWarehouseId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

        public async Task<StockTransferRequest?> GetByIdAsync(int id)
            => await _context.StockTransferRequests
                .Include(r => r.StockTransferItems).ThenInclude(i => i.Product).ThenInclude(p => p.BaseUnit)
                .Include(r => r.StockTransferItems).ThenInclude(i => i.Product).ThenInclude(p => p.Category)
                .Include(r => r.StockTransferItems).ThenInclude(i => i.Unit)
                .Include(r => r.CreatedByNavigation)
                .Include(r => r.ApprovedByNavigation)
                .Include(r => r.FromWarehouse)
                .Include(r => r.ToWarehouse)
                .FirstOrDefaultAsync(r => r.Id == id);

        public async Task<StockTransferRequest?> GetByIdForUpdateAsync(int id)
            => await _context.StockTransferRequests.FirstOrDefaultAsync(r => r.Id == id);

        public async Task<string?> GetLastTransferNoAsync(string prefix)
            => await _context.StockTransferRequests
                .Where(t => t.TransferNo.StartsWith(prefix))
                .OrderByDescending(t => t.TransferNo)
                .Select(t => t.TransferNo)
                .FirstOrDefaultAsync();

        public async Task<Product?> FindProductAsync(int productId)
            => await _context.Products.FindAsync(productId);

        public async Task AddAsync(StockTransferRequest request)
            => await _context.StockTransferRequests.AddAsync(request);

        public void AddItem(StockTransferItem item)
            => _context.StockTransferItems.Add(item);

        public void AddApprovalLog(ApprovalLog log)
            => _context.ApprovalLogs.Add(log);

        public async Task SaveChangesAsync()
            => await _context.SaveChangesAsync();

        public async Task<IDbContextTransaction> BeginTransactionAsync()
            => await _context.Database.BeginTransactionAsync();
    }
}
