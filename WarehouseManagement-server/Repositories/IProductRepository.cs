using warehouseManagement.Models;

namespace warehouseManagement.Repositories
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(int id);
        Task<bool> SkuExistsAsync(string sku);
        Task<bool> CategoryExistsAsync(int categoryId);
        Task<bool> UnitExistsAsync(int unitId);
        Task<bool> HasInventoryAsync(int productId);
        Task AddAsync(Product product);
        Task SaveChangesAsync();
    }
}
