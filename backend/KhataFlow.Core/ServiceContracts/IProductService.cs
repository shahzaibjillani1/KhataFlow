using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.ServiceContracts;

public interface IProductService
{

    Task<List<ProductResponse>?> GetProductByNameAsync(string productName, Guid businessId);

    Task<ProductResponse> AddProductAsync(ProductAddRequest productAddRequest, Guid businessId);

    Task<ProductResponse> UpdateProductAsync(ProductUpdateRequest productUpdateRequest, Guid id);

    Task<bool> DeleteProductAsync(Guid productId, Guid businessId);

    Task<int> GetProductCountAsync(Guid businessId);
    Task<PaginatedResponse<ProductResponse>> GetProductsPagedAsync(Guid businessId, int pageNumber, int pageSize);

    Task<int> GetLowStockProductsCountAsync(Guid businessId, int threshold = 5);

    Task<List<ProductResponse>> GetTopProductsBySalesAsync(Guid businessId, int topN = 5);
    
    Task<List<ProductResponse>> GetProductsByCategoryAsync(Guid businessId, Guid categoryId);

    Task<List<ProductResponse>> GetLowStockProductsAsync(Guid businessId);

    Task<List<ProductResponse>> GetInStockProductsAsync(Guid businessId);

    Task<List<ProductResponse>> GetOutOfStockProductsAsync(Guid businessId);

}