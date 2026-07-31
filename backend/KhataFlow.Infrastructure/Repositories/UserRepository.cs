using KhataFlow.Core.Domain.IdentityEntities;
using KhataFlow.Core.Domain.RepositoryContracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace KhataFlow.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserRepository(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task AddAsync(ApplicationUser user, string password)
    {
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task<int> CountByBusinessAsync(Guid businessId)
    => await _userManager.Users
        .Where(u => u.BusinessId == businessId && !u.IsDeleted)
        .CountAsync();

    public async Task<List<ApplicationUser>> GetAllAsync()
    {
        return await _userManager.Users
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }
    public async Task<List<ApplicationUser>> GetByBusinessIdAsync(Guid businessId)
    {
        return await _userManager.Users
            .Where(u => u.BusinessId == businessId && !u.IsDeleted)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<ApplicationUser?> GetByIdAsync(Guid id)
    {
        return await _userManager.FindByIdAsync(id.ToString());
    }

}