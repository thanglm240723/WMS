using warehouseManagement.Models;

namespace warehouseManagement.Repositories
{
    public interface IBinRepository
    {
        Task<List<Bin>> GetAllAsync(int? warehouseId);
        Task<List<Bin>> GetAvailableAsync(int warehouseId);
        Task<Bin?> GetByIdAsync(int id);
        Task<bool> ExistsAsync(string code, int warehouseId);
        Task<bool> HasInventoryAsync(int binId, int warehouseId);
        Task AddAsync(Bin bin);
        void Delete(Bin bin);
        Task SaveChangesAsync();
    }
}