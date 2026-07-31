namespace KhataFlow.Core.DTO.Response;

public record UserResponse(
    Guid Id,
    string? FullName,
    string? FullNameUr,
    string? DisplayName,
    string? DisplayNameUr,
    string? Email,
    string? PhoneNumber,
    string? ProfilePictureUrl,
    Guid BusinessId,
    string Gender,
    DateTime? DateOfBirth,
    string Role,
    string Status,
    string Plan,
    DateTime? PlanExpiryDate,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);