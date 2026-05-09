using Microsoft.EntityFrameworkCore;
using warehouseManagement.Models;

namespace warehouseManagement.Repositories
{
    public class BinRepository : IBinRepository
    {
        private readonly WmsContext _context;

        public BinRepository(WmsContext context)
        {
            _context = context;
        }

        public async Task<List<Bin>> GetAllAsync(int? warehouseId)
        {
            var query = _context.Bins
                .Include(b => b.Warehouse)
                .AsQueryable();

            if (warehouseId.HasValue)
                query = query.Where(b => b.WarehouseId == warehouseId.Value);

            return await query.OrderBy(b => b.Code).ToListAsync();
        }

        public async Task<List<Bin>> GetAvailableAsync(int warehouseId)
        {
            return await _context.Bins
                .Where(b => b.WarehouseId == warehouseId && b.Status == "Available")
                .OrderBy(b => b.Code)
                .ToListAsync();
        }

        public async Task<Bin?> GetByIdAsync(int id)
            => await _context.Bins.FindAsync(id);

        public async Task<bool> ExistsAsync(string code, int warehouseId)
            => await _context.Bins.AnyAsync(b => b.Code == code && b.WarehouseId == warehouseId);

        public async Task<bool> HasInventoryAsync(int binId, int warehouseId)
            => await _context.Inventories.AnyAsync(i => i.BinId == binId && i.WarehouseId == warehouseId);

        public async Task AddAsync(Bin bin)
            => await _context.Bins.AddAsync(bin);

        public void Delete(Bin bin)
            => _context.Bins.Remove(bin);

        public async Task SaveChangesAsync()
            => await _context.SaveChangesAsync();
    }
}