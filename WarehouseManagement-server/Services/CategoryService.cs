using AutoMapper;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using warehouseManagement.DTOs;
using warehouseManagement.Models;
using warehouseManagement.Repositories;

namespace warehouseManagement.Services
{
    public class CategoryService : ICategoryService
    {

        private readonly ICategoryRepository _repo;
        private readonly IMapper _mapper;

        public CategoryService(ICategoryRepository categoryRepository, IMapper mapper)
        {
            _repo = categoryRepository;
            _mapper = mapper;
        }

        public async Task<CategoryDetailDTO> CreateAsync(CreateCategoryDTO categoryCreateDto)
        {
          var exit =  await _repo.ExistsAsync(categoryCreateDto.Name);
            if (exit)
                throw new Exception("Category đã tồn tại.");
            var cate = _mapper.Map<Category>(categoryCreateDto);
            await _repo.AddAsync(cate);
            await _repo.SaveChangesAsync();
            return _mapper.Map<CategoryDetailDTO>(cate);

        }

        public async Task Delete(int id)
        {
           var exit = await _repo.GetByIdAsync(id);
            if (exit == null)
                throw new Exception("Category không tồn tại.");


                  _repo.Delete(exit);
            await _repo.SaveChangesAsync();


        }

        public async Task<List<CategoryDetailDTO>> GetAllAsync()
        {
           
            var cate = await _repo.GetAllAsync();

            return _mapper.Map<List<CategoryDetailDTO>>(cate);
        }

        public async Task<CategoryDetailDTO> GetCategoryByIdAsync(int id)
        {
            var cate = await _repo.GetByIdAsync(id);

            if (cate == null)
                throw new Exception("Category không tồn tại.");

            return _mapper.Map<CategoryDetailDTO>(cate);
        }

        public async Task EditAsync(int id, UpdateCategoryDTO categoryUpdateDto)
        {
            var exit = await _repo.GetByIdAsync(id);

            if (exit == null)
                throw new Exception("Category không tồn tại.");

             var cate = _mapper.Map(categoryUpdateDto, exit);
             
             await _repo.SaveChangesAsync();
        }
    }
}
