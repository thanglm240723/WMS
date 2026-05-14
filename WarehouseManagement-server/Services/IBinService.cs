using warehouseManagement.DTOs;

namespace warehouseManagement.Services
{
    public interface IBinService
    {
        Task<List<BinViewDto>> GetAll(int userId, int? warehouseId);
        Task<List<BinViewDto>> GetAvailable(int userId, int warehouseId);
        Task<BinViewDto> Create(int userId, BinCreateDto dto);
        Task Update(int userId, int binId, BinUpdateDto dto);
        Task Delete(int userId, int binId);
    }
}






 