namespace KhataFlow.Core.DTO.Request;

public record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string? PhoneNumber,
    string BusinessName
);