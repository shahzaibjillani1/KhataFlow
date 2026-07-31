using KhataFlow.Core.Enums;

namespace KhataFlow.Core.DTO.Request;

public record UserUpdateRequest(
    string? FullName,
    string? DisplayName,
    string? Email,
    string? PhoneNumber,
    Gender? Gender,
    DateTime? DateOfBirth
);
