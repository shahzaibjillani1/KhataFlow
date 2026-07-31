namespace KhataFlow.Core.DTO.Request;

public record RefreshTokenRequest(
    string AccessToken,    
    string RefreshToken
);
