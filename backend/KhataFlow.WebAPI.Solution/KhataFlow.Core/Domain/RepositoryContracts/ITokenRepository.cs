using KhataFlow.Core.Domain.IdentityEntities;

namespace KhataFlow.Core.Domain.RepositoryContracts;

public interface ITokenRepository
{
    Task<UserRefreshToken?> GetValidRefreshTokenAsync(Guid userId, string refreshToken, string jwtId);


    Task AddRefreshTokenAsync(UserRefreshToken refreshToken);

    Task MarkTokenAsUsedAsync(UserRefreshToken refreshToken);


    Task RevokeAllUserTokensAsync(Guid userId);
    Task SaveChangesAsync();
}
