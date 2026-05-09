using warehouseManagement.Models;

namespace warehouseManagement.Repositories
{
    public interface ICategoryRepository
    {
        Task<List<Category>> GetAllAsync();
        Task<Category?> GetByIdAsync(int id);
        Task<bool> ExistsAsync(string name);
        Task AddAsync(Category category);
        
        void Delete(Category category);
        Task SaveChangesAsync();

    }
}
