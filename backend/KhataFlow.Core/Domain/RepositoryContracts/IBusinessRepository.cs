using KhataFlow.Core.Domain.Entities;

namespace KhataFlow.Core.Domain.RepositoryContracts;

public interface IBusinessRepository
{
    Task<Business> AddAsync(Business business);
    Task<Business?> GetByIdAsync(Guid id);
    Task<Business?> GetByOwnerIdAsync(Guid ownerId);
    Task<List<Business>> GetAllAsync();
    Task<int> GetTotalCountAsync();
    Task<int> GetActiveSubscriptionsCountAsync();
    Task<(List<Business> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);
    Task<int> GetNewThisWeekCountAsync();       
    Task<bool> ExistsByOwnerIdAsync(Guid ownerId);
    Task<bool> ExistsByEmailAsync(string email);
    Task<Business> UpdateAsync(Business business);
    Task<bool> DeleteAsync(Guid id);
    
}