namespace KhataFlow.Core.DTO.Response;

public record ImpersonationTokenResponse(
    string Token,
    Guid BusinessId,
    string BusinessName,
    DateTime ExpiresAt  
);