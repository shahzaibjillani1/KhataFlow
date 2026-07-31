using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.ServiceContracts;

public interface IUserService
{
        Task<AuthResponse?> Register(RegisterRequest registerRequest);
        Task<AuthResponse?> Login(LoginRequest loginRequest);

        Task<List<UserResponse>> GetUsersAsync();
    Task<UserResponse> EditUserAsync(
    Guid targetUserId, Guid requestingUserId, UserUpdateRequest request);
    Task<UserResponse> GetUserByIdAsync(Guid id);
    Task<List<UserResponse>> GetBusinessUsersAsync(Guid businessId);
    Task<StaffInviteResponse> InviteStaffAsync(Guid requestingUserId, InviteStaffRequest request);
}
