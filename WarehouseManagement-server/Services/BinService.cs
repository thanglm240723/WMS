using AutoMapper;
using warehouseManagement.DTOs;
using warehouseManagement.Models;
using warehouseManagement.Repositories;

namespace warehouseManagement.Services
{
    public class BinService : IBinService
    {
        private readonly IBinRepository _binRepo;
        private readonly IMapper _mapper;

        public BinService(IBinRepository binRepo, IMapper mapper)
        {
            _binRepo = binRepo;
            _mapper = mapper;
        }

        public async Task<List<BinViewDto>> GetAll(int userId, int? warehouseId)
        {
            var bins = await _binRepo.GetAllAsync(warehouseId);
            return _mapper.Map<List<BinViewDto>>(bins);
        }

        public async Task<List<BinViewDto>> GetAvailable(int userId, int warehouseId)
        {
            var bins = await _binRepo.GetAvailableAsync(warehouseId);
            return _mapper.Map<List<BinViewDto>>(bins);
        }

        public async Task<BinViewDto> Create(int userId, BinCreateDto dto)
        {
            var exists = await _binRepo.ExistsAsync(dto.Code, dto.WarehouseId);
            if (exists)
                throw new Exception("Bin code đã tồn tại trong warehouse này.");

            var bin = _mapper.Map<Bin>(dto);
            bin.Status = "Available";

            await _binRepo.AddAsync(bin);
            await _binRepo.SaveChangesAsync();

            return _mapper.Map<BinViewDto>(bin);
        }

        public async Task Update(int userId, int binId, BinUpdateDto dto)
        {
            var bin = await _binRepo.GetByIdAsync(binId);
            if (bin == null)
                throw new Exception("Bin không tồn tại.");

            _mapper.Map(dto, bin);
            await _binRepo.SaveChangesAsync();
        }

        public async Task Delete(int userId, int binId)
        {
            var bin = await _binRepo.GetByIdAsync(binId);
            if (bin == null)
                throw new Exception("Bin không tồn tại.");

            var hasInventory = await _binRepo.HasInventoryAsync(binId, bin.WarehouseId);
            if (hasInventory)
                throw new Exception("Bin đang chứa hàng, không thể xóa.");

            _binRepo.Delete(bin);
            await _binRepo.SaveChangesAsync();
        }
    }
}