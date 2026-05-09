using warehouseManagement.DTOs;

namespace warehouseManagement.Services
{
    public interface IProductService
    {
        Task<List<ProductDTO>> GetAllAsync();
        Task<ProductDTO> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateProductDTO dto);
        Task UpdateAsync(int id, UpdateProductDTO dto);
        Task DeleteAsync(int id);
    }
}
