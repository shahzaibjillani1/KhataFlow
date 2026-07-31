using KhataFlow.Core.DTO;
using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.ServiceContracts;

public interface ICategoryService
{
    Task<List<CategoryResponse>> GetAllCategoriesAsync(Guid businessId);

    Task<CategoryResponse?> GetCategoryByIdAsync(Guid businessId, Guid id);
    Task<PaginatedResponse<CategoryResponse>> GetCategoriesPagedAsync(Guid businessId, int pageNumber, int pageSize);

    Task<CategoryResponse> AddCategoryAsync(Guid businessId, CategoryAddRequest categoryAddRequest);

    Task<CategoryResponse> UpdateCategoryAsync(Guid businessId, CategoryUpdateRequest categoryUpdateRequest);

    Task<bool> DeleteCategoryAsync(Guid businessId, Guid id);
}