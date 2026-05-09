using AutoMapper;
using warehouseManagement.DTOs;
using warehouseManagement.Models;
using warehouseManagement.Repositories;

namespace warehouseManagement.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;
        private readonly IMapper _mapper;

        public ProductService(IProductRepository repo, IMapper mapper)
        {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<List<ProductDTO>> GetAllAsync()
        {
            var products = await _repo.GetAllAsync();
            return _mapper.Map<List<ProductDTO>>(products);
        }

        public async Task<ProductDTO> GetByIdAsync(int id)
        {
            var product = await _repo.GetByIdAsync(id);
            if (product == null)
                throw new KeyNotFoundException("Product not found");
            return _mapper.Map<ProductDTO>(product);
        }

        public async Task<int> CreateAsync(CreateProductDTO dto)
        {
            if (await _repo.SkuExistsAsync(dto.Sku))
                throw new InvalidOperationException("SKU already exists");

            if (!await _repo.CategoryExistsAsync(dto.CategoryId))
                throw new KeyNotFoundException("Category not found");

            if (!await _repo.UnitExistsAsync(dto.BaseUnitId))
                throw new KeyNotFoundException("Base unit not found");

            var product = _mapper.Map<Product>(dto);
            await _repo.AddAsync(product);
            await _repo.SaveChangesAsync();
            return product.Id;
        }

        public async Task UpdateAsync(int id, UpdateProductDTO dto)
        {
            var product = await _repo.GetByIdAsync(id);
            if (product == null)
                throw new KeyNotFoundException("Product not found");

            if (dto.CategoryId.HasValue && !await _repo.CategoryExistsAsync(dto.CategoryId.Value))
                throw new KeyNotFoundException("Category not found");

            if (dto.BaseUnitId.HasValue && !await _repo.UnitExistsAsync(dto.BaseUnitId.Value))
                throw new KeyNotFoundException("Unit not found");

            if (dto.BaseUnitId.HasValue && dto.BaseUnitId != product.BaseUnitId)
            {
                if (await _repo.HasInventoryAsync(product.Id))
                    throw new InvalidOperationException("Không thể đổi Base Unit khi sản phẩm đã có tồn kho.");
            }

            _mapper.Map(dto, product);
            await _repo.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _repo.GetByIdAsync(id);
            if (product == null)
                throw new KeyNotFoundException("Product not found");

            product.Status = "INACTIVE";
            await _repo.SaveChangesAsync();
        }
    }
}
