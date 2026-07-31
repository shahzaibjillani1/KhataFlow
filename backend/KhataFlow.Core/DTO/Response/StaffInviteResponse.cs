namespace KhataFlow.Core.DTO.Response;

public record StaffInviteResponse(
    UserResponse User,
    string WhatsAppShareUrl
);