using KhataFlow.Core.Domain.IdentityEntities;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.ServiceContracts;

public interface ITokenService
{
    Task<AuthResponse> GenerateAuthResponseAsync(ApplicationUser user);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task RevokeRefreshTokenAsync(Guid userId);
}