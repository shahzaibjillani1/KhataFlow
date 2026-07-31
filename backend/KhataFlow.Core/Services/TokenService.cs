using KhataFlow.Core.Domain.IdentityEntities;
using KhataFlow.Core.Domain.RepositoryContracts;
using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.ServiceContracts;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace KhataFlow.Core.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly ITokenRepository _tokenRepository;
    private readonly IUserRepository _userRepository; 

    public TokenService(
        IConfiguration config,
        ITokenRepository tokenRepository,
        IUserRepository userRepository)
    {
        _config = config;
        _tokenRepository = tokenRepository;
        _userRepository = userRepository;
    }

    public async Task<AuthResponse> GenerateAuthResponseAsync(ApplicationUser user)
    {
        var expiryMinutes = GetExpiryMinutes();
        var refreshExpiryDays = GetRefreshExpiryDays();
        var now = DateTime.UtcNow; 

        var (jwt, jwtId) = GenerateJwtTokenWithId(user);
        var refreshToken = GenerateRefreshToken();

        await _tokenRepository.AddRefreshTokenAsync(new UserRefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            JwtId = jwtId,
            ExpiresAt = now.AddDays(refreshExpiryDays),
            IsUsed = false,
            IsRevoked = false,
        });

        await _tokenRepository.SaveChangesAsync();

        return new AuthResponse(
            AccessToken: jwt,
            RefreshToken: refreshToken,
            AccessTokenExpiry: now.AddMinutes(expiryMinutes),
            RefreshTokenExpiry: now.AddDays(refreshExpiryDays)
        );
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var principal = GetPrincipalFromExpiredToken(request.AccessToken);

        var userId = Guid.Parse(
            principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new SecurityTokenException("Token missing sub."));

        var jwtId = principal.FindFirstValue(JwtRegisteredClaimNames.Jti)
            ?? throw new SecurityTokenException("Token missing Jti.");

        var storedToken = await _tokenRepository
            .GetValidRefreshTokenAsync(userId, request.RefreshToken, jwtId)
            ?? throw new SecurityTokenException("Invalid or expired refresh token.");

        await _tokenRepository.MarkTokenAsUsedAsync(storedToken);

        var user = await _userRepository.GetByIdAsync(userId)
            ?? throw new SecurityTokenException("User not found.");

        return await GenerateAuthResponseAsync(user);
    }

    public async Task RevokeRefreshTokenAsync(Guid userId)
    {
        await _tokenRepository.RevokeAllUserTokensAsync(userId);
    }

    private (string Token, string JwtId) GenerateJwtTokenWithId(ApplicationUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var jwtId = Guid.NewGuid().ToString();

        var claims = new[]
{
    new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
    new Claim(JwtRegisteredClaimNames.Name,  user.FullName ?? ""),
    new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
    new Claim(JwtRegisteredClaimNames.Jti,   jwtId),
    new Claim("role",                        user.Role.ToString()),
    new Claim("businessId",                  user.BusinessId.ToString()),
};

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(GetExpiryMinutes()),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), jwtId);
    }

    private string GenerateRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

        var validation = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = true,
            ValidIssuer = _config["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = _config["Jwt:Audience"],
            ValidateLifetime = false,
        };

        var handler = new JwtSecurityTokenHandler
        {
            MapInboundClaims = false 
        };

        return handler.ValidateToken(token, validation, out _);
    }

    private double GetExpiryMinutes()
        => double.Parse(_config["Jwt:ExpiryMinutes"] ?? "15");

    private double GetRefreshExpiryDays()
        => double.Parse(_config["Jwt:RefreshExpiryDays"] ?? "7");
}