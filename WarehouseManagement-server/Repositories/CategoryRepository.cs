using Microsoft.EntityFrameworkCore;
using warehouseManagement.Models;

namespace warehouseManagement.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {

        private readonly WmsContext _context;

        public CategoryRepository(WmsContext context)
        {
            _context = context;
        }
        public async Task AddAsync(Category category)
            => await _context.Categories.AddAsync(category);
        

        public void Delete(Category category)
           => _context.Categories.Remove(category);

        public async Task<bool> ExistsAsync(string name)
            => await _context.Categories.AnyAsync(c => c.Name == name);

        public async Task<List<Category>> GetAllAsync()
            => await _context.Categories.ToListAsync();

        public async Task<Category?> GetByIdAsync(int id)
            => await _context.Categories.FindAsync(id);

        public async Task SaveChangesAsync()
            => await _context.SaveChangesAsync();
        
    }
}
