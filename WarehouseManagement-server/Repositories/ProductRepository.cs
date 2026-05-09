using Microsoft.EntityFrameworkCore;
using warehouseManagement.Models;

namespace warehouseManagement.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly WmsContext _context;

        public ProductRepository(WmsContext context)
        {
            _context = context;
        }

        public async Task<List<Product>> GetAllAsync()
            => await _context.Products
                .Include(p => p.Category)
                .Include(p => p.BaseUnit)
                .ToListAsync();

        public async Task<Product?> GetByIdAsync(int id)
            => await _context.Products
                .Include(p => p.Category)
                .Include(p => p.BaseUnit)
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<bool> SkuExistsAsync(string sku)
            => await _context.Products.AnyAsync(p => p.Sku == sku);

        public async Task<bool> CategoryExistsAsync(int categoryId)
            => await _context.Categories.AnyAsync(c => c.Id == categoryId);

        public async Task<bool> UnitExistsAsync(int unitId)
            => await _context.Units.AnyAsync(u => u.Id == unitId);

        public async Task<bool> HasInventoryAsync(int productId)
            => await _context.Inventories.AnyAsync(i => i.ProductId == productId && i.Quantity > 0);

        public async Task AddAsync(Product product)
            => await _context.Products.AddAsync(product);

        public async Task SaveChangesAsync()
            => await _context.SaveChangesAsync();
    }
}
