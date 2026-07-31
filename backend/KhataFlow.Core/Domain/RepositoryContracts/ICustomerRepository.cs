using KhataFlow.Core.Domain.Entities;

namespace KhataFlow.Core.Domain.RepositoryContracts;

public interface ICustomerRepository
{
    Task<Customer> AddAsync(Customer customer);
    Task<Customer> UpdateAsync(Customer customer);
    Task<bool> DeleteAsync(Guid id);

    Task<Customer?> GetByIdAsync(Guid id);
    Task<List<Customer>> GetByBusinessIdAsync(Guid businessId);
    Task<int> CountByBusinessAsync(Guid businessId);
    Task<Customer?> GetByPhoneAsync(Guid businessId, string phone);
    Task<(List<Customer> Items, int TotalCount)> GetPagedAsync(Guid businessId, int pageNumber, int pageSize);
    Task<List<Customer>> GetCustomersWithOutstandingAsync(Guid businessId); 
    Task<int> GetCustomerCountAsync(Guid businessId);          
    Task<bool> ExistsAsync(Guid businessId, string phone);
    Task<Customer?> GetByPublicTokenAsync(string token, CancellationToken ct = default);
}