namespace KhataFlow.Core.DTO;

public record BusinessUpdateRequest(
    Guid Id,
    string? Name,
    string? Email,
    string? PhoneNumber,
    string? Address
);