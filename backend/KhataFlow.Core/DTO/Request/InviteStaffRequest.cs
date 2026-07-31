using KhataFlow.Core.Enums;

namespace KhataFlow.Core.DTO.Request;

public record InviteStaffRequest(
    string FullName,
    string? Email,
    string PhoneNumber,
    UserRole Role   
);