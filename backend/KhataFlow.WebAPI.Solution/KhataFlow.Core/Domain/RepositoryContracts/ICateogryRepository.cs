using KhataFlow.Core.Domain.Entities;

namespace KhataFlow.Core.Domain.RepositoryContracts;

public interface ICategoryRepository
{
    Task<Category> AddAsync(Category category, Guid businessId);

    Task<Category> UpdateAsync(Category category, Guid businessId);

    Task<bool> DeleteAsync(Guid id, Guid businessId);

    Task<Category?> GetByIdAsync(Guid id, Guid businessId);

    Task<List<Category>> GetByBusinessIdAsync(Guid businessId);
    Task<(List<Category> Items, int TotalCount)> GetPagedAsync(Guid businessId, int pageNumber, int pageSize);


    Task<bool> ExistsAsync(Guid businessId, string name);
}