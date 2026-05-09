using warehouseManagement.DTOs;
using warehouseManagement.Models;

namespace warehouseManagement.Services
{
    public interface ICategoryService
    {
        Task<List<CategoryDetailDTO>> GetAllAsync();

        Task<CategoryDetailDTO> GetCategoryByIdAsync(int id);

        Task<CategoryDetailDTO> CreateAsync(CreateCategoryDTO categoryCreateDto);

        Task EditAsync(int id , UpdateCategoryDTO categoryUpdateDto);

        Task Delete (int id);
    }
}
