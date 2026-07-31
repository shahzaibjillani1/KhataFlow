using KhataFlow.Core.Domain.IdentityEntities;

namespace KhataFlow.Core.Domain.RepositoryContracts;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByEmailAsync(string email);
    Task<ApplicationUser?> GetByIdAsync(Guid id);
    Task<int> CountByBusinessAsync(Guid businessId);
    Task<List<ApplicationUser>> GetAllAsync();
    Task<List<ApplicationUser>> GetByBusinessIdAsync(Guid businessId);
    Task AddAsync(ApplicationUser user, string password);
}
