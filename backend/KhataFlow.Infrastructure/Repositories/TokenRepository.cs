using KhataFlow.Core.Domain.IdentityEntities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KhataFlow.Infrastructure.Repositories;

public class TokenRepository : ITokenRepository
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public TokenRepository(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task AddRefreshTokenAsync(UserRefreshToken refreshToken)
    {
        ArgumentNullException.ThrowIfNull(refreshToken);

        var userExists = await _userManager.Users.AnyAsync(u => u.Id == refreshToken.UserId);
        if (!userExists)
            throw new InvalidOperationException($"User {refreshToken.UserId} not found in database.");

        await _context.UserRefreshTokens.AddAsync(refreshToken);
    }

    public async Task<UserRefreshToken?> GetValidRefreshTokenAsync(
        Guid userId, string refreshToken, string jwtId)
    {
        return await _context.UserRefreshTokens
            .FirstOrDefaultAsync(t =>
                t.Token == refreshToken &&
                t.UserId == userId &&
                t.JwtId == jwtId &&
                !t.IsRevoked &&
                !t.IsUsed &&
                t.ExpiresAt > DateTime.UtcNow);
    }

    public Task MarkTokenAsUsedAsync(UserRefreshToken refreshToken)
    {
        refreshToken.IsUsed = true;
        return Task.CompletedTask;
    }

    public async Task RevokeAllUserTokensAsync(Guid userId)
    {
        await _context.UserRefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsRevoked, true));
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}