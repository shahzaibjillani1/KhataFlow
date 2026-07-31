using KhataFlow.Core.Domain.Entities;
using KhataFlow.Core.DTO;

namespace KhataFlow.Core.Domain.RepositoryContracts;

public interface IProductRepository
{
    Task<Product> AddAsync(Product product);
    Task<Product> UpdateAsync(Product product);
    Task<bool> DeleteAsync(Guid id);
    Task<int> GetProductCountAsync(Guid businessId);
    Task<int> GetLowStockCountAsync(Guid businessId);
    Task<int> CountByBusinessAsync(Guid businessId);
    Task<Product?> GetByIdAsync(Guid id);
    Task<Product?> GetByIdWithCategoryAsync(Guid id);
    Task<Product?> GetByIdForBusinessAsync(Guid id, Guid businessId);
    Task<List<Product>> GetAllWithCategoryAsync();
    Task<(List<Product> Items, int TotalCount)> GetPagedAsync(Guid businessId, int pageNumber, int pageSize);
    Task<List<Product>> GetByBusinessIdAsync(Guid businessId);
    Task<List<Product>> GetByNameAsync(Guid businessId, string name);
    Task<List<Product>> GetLowStockAsync(Guid businessId, int threshold);
    Task<List<Product>> GetTopProductsBySalesAsync(Guid businessId, int topN);

    Task<int> GetLowStockCountAsync(Guid businessId, int threshold);
    Task<bool> ExistsAsync(Guid businessId, string name);

    Task<List<Product>> GetProductsByCategoryAsync(Guid businessId, Guid categoryId);
    Task<List<Product>> GetLowStockProductsAsync(Guid businessId);

    Task<List<Product>> GetInStockProductsAsync(Guid businessId);

    Task<List<Product>> GetOutOfStockProductsAsync(Guid businessId);
}